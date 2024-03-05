''' <summary>
''' An account on Bluesky or a compatible atproto app, with optional username
''' and password credentials.
''' </summary>
Public Interface IBlueskyAccountConfiguration
    Inherits ATProto.IAccount

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
