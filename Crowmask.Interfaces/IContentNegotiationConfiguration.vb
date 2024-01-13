''' <summary>
''' Settings for Crowmask's content negotiation.
''' </summary>
Public Interface IContentNegotiationConfiguration
    ''' <summary>
    ''' Whether the HTML and Markdown user interfaces of Crowmask are enabled.
    ''' </summary>
    ''' <returns></returns>
    ReadOnly Property UserInterface As Boolean

    ''' <summary>
    ''' Whether requests from web browsers should redirect to Weasyl.
    ''' Applies only to individual posts and the user profile.
    ''' </summary>
    ''' <returns></returns>
    ReadOnly Property UpstreamRedirect As Boolean
End Interface
