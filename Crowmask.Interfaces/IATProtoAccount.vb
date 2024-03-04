Public Interface IATProtoAccount
    ''' <summary>
    ''' The hostname of the PDS (e.g. bluesky.social).
    ''' </summary>
    ''' <returns></returns>
    ReadOnly Property Hostname As String

    ''' <summary>
    ''' The user's handle (e.g. example.bsky.social).
    ''' </summary>
    ''' <returns></returns>
    ReadOnly Property Handle As String

    ''' <summary>
    ''' The login username, if available.
    ''' </summary>
    ''' <returns></returns>
    ReadOnly Property Identifier As String

    ''' <summary>
    ''' The login password, if available.
    ''' </summary>
    ''' <returns></returns>
    ReadOnly Property Password As String
End Interface
