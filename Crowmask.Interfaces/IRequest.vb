Imports System.Net.Http
Imports System.Net.Http.Headers

''' <summary>
''' An abstraction of request data used to validate incoming HTTP signatures.
''' </summary>
Public Interface IRequest
    ''' <summary>
    ''' The HTTP method (e.g. GET, POST).
    ''' </summary>
    ''' <returns></returns>
    ReadOnly Property Method As HttpMethod

    ''' <summary>
    ''' The request URI (e.g. https://crowmask.example.com/api/actor/inbox).
    ''' </summary>
    ''' <returns></returns>
    ReadOnly Property RequestUri As Uri

    ''' <summary>
    ''' A collection of request headers.
    ''' </summary>
    ''' <returns></returns>
    ReadOnly Property Headers As HttpHeaders
End Interface
