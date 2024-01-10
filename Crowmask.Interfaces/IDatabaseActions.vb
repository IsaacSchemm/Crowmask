Imports Crowmask.DomainModeling

Public Interface IDatabaseActions
    Function AddKnownInboxAsync(actor As IRemoteActor) As Task

    Function AddOutboundActivityAsync(obj As IDictionary(Of String, Object), actor As IRemoteActor) As Task

    Function AddFollowAsync(objectId As String, actor As IRemoteActor) As Task

    Function RemoveFollowAsync(objectId As String) As Task

    Function AddLikeAsync(identifier As JointIdentifier, activityId As String, actor As IRemoteActor) As Task

    Function AddBoostAsync(identifier As JointIdentifier, activityId As String, actor As IRemoteActor) As Task

    Function AddReplyAsync(identifier As JointIdentifier, replyId As String, actor As IRemoteActor) As Task

    Function RemoveInteractionAsync(identifier As JointIdentifier, id As Guid) As Task
End Interface
