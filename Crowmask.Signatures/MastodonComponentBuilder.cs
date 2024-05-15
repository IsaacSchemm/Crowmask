﻿// Adapted from Letterbook
// https://github.com/Letterbook/Letterbook/blob/b1616beaf49ddefea22de58f41783521e088ea10/Letterbook.Adapter.ActivityPub/Signatures/MastodonComponentBuilder.cs
// GNU Affero General Public License v3.0

using static NSign.Constants;
using NSign.Signatures;

namespace Crowmask.Signatures;

public class MastodonComponentBuilder(SignedRequestToVerify _message) : ISignatureComponentVisitor
{
    private readonly List<string> _derivedParams = [];
    private readonly List<string> _derivedParamsValues = [];
    private readonly List<string> _headerParams = [];
    private readonly List<string> _headerParamsValues = [];

    public string SigningDocument => BuildSigningDocument();
    public string? SigningDocumentSpec => BuildDocumentSpec();

    void ISignatureComponentVisitor.Visit(SignatureComponent component) { }

    void ISignatureComponentVisitor.Visit(HttpHeaderComponent httpHeader)
    {
        string fieldName = httpHeader.ComponentName;

        if (_message.Headers.TryGetValues(fieldName, out var values))
        {
            AddHeader(
                fieldName.Equals("content-digest", StringComparison.InvariantCultureIgnoreCase)
                    ? "digest"
                    : fieldName,
                string.Join(", ", values));
        }
        else
        {
            if (fieldName == "host")
            {
                AddHeader(
                    fieldName,
                    _message.GetDerivedComponentValue(SignatureComponent.Authority));
            }
        }
    }

    void ISignatureComponentVisitor.Visit(HttpHeaderDictionaryStructuredComponent httpHeaderDictionary) { }

    void ISignatureComponentVisitor.Visit(HttpHeaderStructuredFieldComponent httpHeaderStructuredField) { }

    void ISignatureComponentVisitor.Visit(DerivedComponent derived)
    {
        var method = new DerivedComponent(DerivedComponents.Method);
        switch (derived.ComponentName)
        {
            case DerivedComponents.RequestTarget:
                AddRequestTarget(
                    $"{_message.GetDerivedComponentValue(method).ToLowerInvariant()} {_message.GetDerivedComponentValue(derived)}");
                break;
            case DerivedComponents.Authority:
                AddHeader("host", _message.GetDerivedComponentValue(derived));
                break;
        }
    }

    public void Visit(SignatureParamsComponent signatureParamsComponent)
    {
        var hasTarget = false;
        foreach (SignatureComponent component in signatureParamsComponent.Components)
        {
            component.Accept(this);
            if (component is DerivedComponent { ComponentName: DerivedComponents.RequestTarget })
            {
                hasTarget = true;
            }
        }

        if (!hasTarget) new DerivedComponent(DerivedComponents.RequestTarget).Accept(this);
    }

    void ISignatureComponentVisitor.Visit(QueryParamComponent queryParam) { }

    private void AddHeader(string header, string value)
    {
        _headerParams.Add(header);
        _headerParamsValues.Add($"{header}: {value}");
    }

    private void AddRequestTarget(string value)
    {
        _derivedParams.Add("(request-target)");
        _derivedParamsValues.Add($"(request-target): {value}");
    }

    private string BuildSigningDocument()
    {
        return string.Join('\n', _derivedParamsValues.Concat(_headerParamsValues));
    }

    private string BuildDocumentSpec()
    {
        return string.Join(' ', _derivedParams.Concat(_headerParams));
    }
}