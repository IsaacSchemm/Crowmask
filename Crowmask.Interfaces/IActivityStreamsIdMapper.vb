Imports Crowmask.DomainModeling

Public Interface IActivityStreamsIdMapper
    ''' <summary>
    ''' The ID / URL of the single ActivityPub actor exposed by Crowmask.
    ''' </summary>
    ''' <returns>An ActivityPub ID / URL</returns>
    ReadOnly Property ActorId As String

    ''' <summary>
    ''' Generates a random ActivityPub ID that is not intended to be looked up.
    ''' Used for Create, Update, and Delete activities.
    ''' </summary>
    ''' <returns>An ActivityPub ID / URL</returns>
    Function GetTransientId() As String

    ''' <summary>
    ''' Determines the appropriate ActivityPub type for a Crowmask post.
    ''' </summary>
    ''' <param name="identifier">The submission or journal ID</param>
    ''' <returns>Note or Article</returns>
    Function GetObjectType(identifier As JointIdentifier) As String

    ''' <summary>
    ''' Determines the ID / URL of the ActivityPub object that Crowmask will
    ''' generate for a post.
    ''' </summary>
    ''' <param name="identifier">The submission or journal ID</param>
    ''' <returns>An ActivityPub ID / URL</returns>
    Function GetObjectId(identifier As JointIdentifier) As String

    ''' <summary>
    ''' Extracts the submission or journal ID from the ActivityPub object ID,
    ''' if it matches the format used by this Crowmask server.
    ''' </summary>
    ''' <param name="objectId">An ActivityPub ID / URL</param>
    ''' <returns>A submission or journal ID, or null</returns>
    Function GetJointIdentifier(objectId As String) As JointIdentifier?

    ''' <summary>
    ''' Determines the ActivityPub ID / URL of the notification sent to the
    ''' admin actor for the given interaction.
    ''' </summary>
    ''' <param name="identifier">The submission or journal ID of the post that was interacted with</param>
    ''' <param name="interaction">Information about the interaction (like, boost, or reply)</param>
    ''' <returns>An ActivityPub ID / URL</returns>
    Function GetNotificationObjectId(identifier As JointIdentifier, interaction As Interaction) As String
End Interface
