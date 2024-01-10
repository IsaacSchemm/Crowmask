''' <summary>
''' Provides the hostname used in the Crowmask actor's preferred handle.
''' </summary>
Public Interface IHandleHost
    ''' <summary>
    ''' The host / domain name used in the Crowmask actor's preferred handle.
    ''' May or may not be the same as Crowmask's domain.
    ''' </summary>
    ReadOnly Property Hostname As String
End Interface
