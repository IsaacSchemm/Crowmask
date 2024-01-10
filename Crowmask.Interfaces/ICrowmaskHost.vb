''' <summary>
''' Provides the hostname used by Crowmask for rendering absolute links and
''' ActivityPub URLs. This should be the domain that Crowmask is running on.
''' </summary>
Public Interface ICrowmaskHost
    ''' <summary>
    ''' The host / domain name used by Crowmask.
    ''' May or may not be the same as the domain in the actor's handle.
    ''' </summary>
    ''' <returns></returns>
    ReadOnly Property Hostname As String
End Interface