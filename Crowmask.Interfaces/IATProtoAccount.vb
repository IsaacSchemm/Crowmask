Public Interface IATProtoAccount
    ''' <summary>
    ''' The hostname of the PDS (e.g. bluesky.social).
    ''' </summary>
    ''' <returns></returns>
    ReadOnly Property Hostname As String

    ''' <summary>
    ''' The DID (e.g. did:plc:xxxxxx).
    ''' </summary>
    ''' <returns></returns>
    ReadOnly Property DID As String

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
