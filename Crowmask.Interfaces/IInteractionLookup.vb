''' <summary>
''' Provides a way to quickly look up a Crowmask post, given the ID of a
''' relevant ActivityPub object (a Like, an Announce, or a reply).
''' </summary>
''' <param name="activity_or_reply_id"></param>
''' <returns></returns>
Public Interface IInteractionLookup
    ''' <summary>
    ''' Given the ID of an ActivityPub boost, like, or reply, returns the
    ''' corresponding submission IDs, or returns null if the activity or
    ''' object is not present on any submissions in Crowmask's cache. An
    ''' interaction will generally be tied to just one post.
    ''' </summary>
    ''' <param name="activity_or_reply_id">The ID of an Announce or Like activity, or the ID of a reply to an artwork submission</param>
    Function GetRelevantSubmitIdsAsync(activity_or_reply_id As String) As Task(Of IEnumerable(Of Integer))
End Interface
