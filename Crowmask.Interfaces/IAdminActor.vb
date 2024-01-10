''' <summary>
''' Provides the ActivityPub ID of the admin actor, who will recieve private
''' notifications of likes, boosts, and replies.
''' </summary>
Public Interface IAdminActor
    ''' <summary>
    ''' The admin actor's ActivityPub ID.
    ''' </summary>
    ''' <returns></returns>
    ReadOnly Property Id As String
End Interface
