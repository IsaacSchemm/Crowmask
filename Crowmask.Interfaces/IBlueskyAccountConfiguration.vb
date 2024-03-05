''' <summary>
''' An account on Bluesky or a compatible atproto app, with optional username
''' and password credentials.
''' </summary>
Public Interface IBlueskyAccountConfiguration
    ''' <summary>
    ''' The PDS to connect to (e.g. bsky.social).
    ''' </summary>
    ''' <returns></returns>
    ReadOnly Property PDS As String

    ''' <summary>
    ''' The user's DID.
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
