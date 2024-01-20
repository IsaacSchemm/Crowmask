''' <summary>
''' Provides the hostname used in the Crowmask actor's preferred handle.
''' </summary>
Public Interface IHandle
    ''' <summary>
    ''' The username of the Crowmask actor (used in the @ handle).
    ''' </summary>
    ReadOnly Property PreferredUsername As String

    ''' <summary>
    ''' The host / domain name used in the Crowmask actor's preferred handle.
    ''' May or may not be the same as Crowmask's domain.
    ''' </summary>
    ReadOnly Property Hostname As String
End Interface
