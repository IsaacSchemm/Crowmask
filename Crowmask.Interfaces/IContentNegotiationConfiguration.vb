''' <summary>
''' Settings for Crowmask's content negotiation.
''' </summary>
Public Interface IContentNegotiationConfiguration
    ''' <summary>
    ''' Whether the HTML (text/html) user interface of Crowmask is enabled.
    ''' </summary>
    ''' <returns></returns>
    ReadOnly Property ReturnHTML As Boolean

    ''' <summary>
    ''' Whether the Markdown (text/plain) user interface of Crowmask is enabled.
    ''' </summary>
    ''' <returns></returns>
    ReadOnly Property ReturnMarkdown As Boolean

    ''' <summary>
    ''' Whether requests from web browsers to individual posts or to the actor
    ''' URL should redirect to Weasyl.
    ''' </summary>
    ''' <returns></returns>
    ReadOnly Property UpstreamRedirect As Boolean
End Interface
