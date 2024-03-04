''' <summary>
''' An account on Bluesky or a compatible atproto app.
''' </summary>
Public Interface IBlueskyAccount
    ''' <summary>
    ''' The hostname of the PDS (e.g. bluesky.social).
    ''' </summary>
    ''' <returns></returns>
    ReadOnly Property PDS As String

    ''' <summary>
    ''' The user's DID (e.g. did:plc:xxxxxxxxxxxxxxxxxxxxxxxx).
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
