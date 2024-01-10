using System.Net.Http.Headers;

namespace Crowmask.Library.Signatures;

public record IncomingRequest(
    HttpMethod Method,
    Uri RequestUri,
    HttpHeaders Headers);
