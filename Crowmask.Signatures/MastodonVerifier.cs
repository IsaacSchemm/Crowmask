// Adapted from Letterbook
// https://github.com/Letterbook/Letterbook/blob/b1616beaf49ddefea22de58f41783521e088ea10/Letterbook.Adapter.ActivityPub/Signatures/MastodonVerifier.cs
// GNU Affero General Public License v3.0

using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using NSign;
using NSign.Signatures;

namespace Crowmask.Signatures;

public partial class MastodonVerifier(ILogger<MastodonVerifier> _logger)
{
    private readonly HashSet<string> DerivedComponents = [
        Constants.DerivedComponents.Authority,
        Constants.DerivedComponents.Status,
        Constants.DerivedComponents.RequestTarget,
        Constants.DerivedComponents.TargetUri,
        Constants.DerivedComponents.Path,
        Constants.DerivedComponents.Method,
        Constants.DerivedComponents.Query,
        Constants.DerivedComponents.Scheme,
        Constants.DerivedComponents.QueryParam,
        Constants.DerivedComponents.SignatureParams
    ];

    [GeneratedRegex(@"\(.*\)")]
    private static partial Regex DerivedComponentsRegex();

    private static readonly char[] SpacesAndQuotes = [' ', '"'];
    private static readonly char[] SpacesAndParentheses = [' ', '(', ')'];

    public VerificationResult VerifyRequestSignature(SignedRequestToVerify message, ISigningKey verificationKey)
    {
        var builder = new MastodonComponentBuilder(message);
        var components = ParseMastodonSignatureComponents(message);
        var result = VerificationResult.NoMatchingVerifierFound;

        foreach (var parsed in components)
        {
            if (!Uri.TryCreate(parsed.keyId, UriKind.Absolute, out Uri? keyId))
                continue;
            if (keyId != verificationKey.Id)
                continue;

            if (VerifySignature(parsed, verificationKey, builder))
                return VerificationResult.SuccessfullyVerified;

            result = VerificationResult.SignatureMismatch;
        }

        return result;
    }

    private IEnumerable<MastodonSignatureComponents> ParseMastodonSignatureComponents(SignedRequestToVerify message)
    {
        if (!message.Headers.TryGetValues(Constants.Headers.Signature, out var values))
            return []; 

        var mastodonSignatures = values
            .Select(header => header.Split(',', StringSplitOptions.RemoveEmptyEntries))
            .Where(parts => parts.Length > 1);

        if (!mastodonSignatures.Any())
            return [];

        return mastodonSignatures.Select(ParseSignatureValue);
    }

    private MastodonSignatureComponents ParseSignatureValue(IEnumerable<string> parts)
    {
        var pairs = parts.Select(part =>
        {
            var innerParts = part.Split('=', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            return (innerParts[0], innerParts[1]);
        });

        var components = new MastodonSignatureComponents();

        foreach (var (key, value) in pairs)
        {
            switch (key)
            {
                case "keyId":
                    components.keyId = value.Trim('"');
                    break;
                case "signature":
                    components.signature = value.Trim('"');
                    break;
                case "headers":
                    string headersString = value;

                    _logger.LogDebug("Parsing Mastodon signature headers '{Headers}'", headersString);

                    var spec = new SignatureInputSpec("spec");
                    var match = DerivedComponentsRegex().Match(headersString);
                    if (match.Success)
                    {
                        foreach (var token in match.Value.Split(SpacesAndParentheses, StringSplitOptions.RemoveEmptyEntries))
                        {
                            spec.SignatureParameters.AddComponent(new DerivedComponent("@" + token));
                        }
                    }

                    var comps = headersString[(match.Length + 1)..]
                        .Split(SpacesAndQuotes, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select<string, SignatureComponent>(s =>
                            DerivedComponents.Contains(s)
                                ? new DerivedComponent(s)
                            : DerivedComponents.Contains("@" + s)
                                ? new DerivedComponent("@" + s)
                            : new HttpHeaderComponent(s));

                    foreach (var component in comps)
                    {
                        spec.SignatureParameters.AddComponent(component);
                    }

                    _logger.LogDebug("Parsed Mastodon signature headers as {@Spec}", spec);

                    components.spec = spec;
                    break;
                default:
                    _logger.LogWarning(
                        "Unknown component {Component} in apparently Mastodon-compatible HTTP signature {Parts}",
                        key, parts);
                    break;
            }
        }

        return components;
    }

    private bool VerifySignature(MastodonSignatureComponents components, ISigningKey verificationKey,
        MastodonComponentBuilder builder)
    {
        var algorithm = verificationKey.GetRsa();
        builder.Visit(components.spec.SignatureParameters);
        return algorithm.VerifyData(
            Encoding.ASCII.GetBytes(builder.SigningDocument),
            Convert.FromBase64String(components.signature),
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
    }

    private struct MastodonSignatureComponents
    {
        internal SignatureInputSpec spec;
        internal string keyId;
        internal string signature;
    }
}
