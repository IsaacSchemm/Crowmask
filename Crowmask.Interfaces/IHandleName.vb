''' <summary>
''' Provides the username of the ActivityPub actor that Crowmask creates.
''' </summary>
Public Interface IHandleName
    ''' <summary>
    ''' The username of the Crowmask actor (used in the @ handle).
    ''' </summary>
    ReadOnly Property PreferredUsername As String
End Interface