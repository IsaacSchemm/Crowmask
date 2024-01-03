using System.Net.Http.Headers;

namespace Crowmask.Signatures;

public record IncomingRequest(
    HttpMethod Method,
    Uri RequestUri,
    HttpHeaders Headers);
