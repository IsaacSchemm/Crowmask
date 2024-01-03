using System.Net.Http.Headers;

namespace Crowmask.Signatures;

public record SignedRequestToVerify(
    HttpMethod Method,
    Uri RequestUri,
    HttpHeaders Headers);
