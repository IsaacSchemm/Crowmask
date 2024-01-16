''' <summary>
''' Provides a way to quickly look up a Crowmask post, given the ID of a
''' relevant ActivityPub object (a Like, an Announce, or a reply).
''' </summary>
''' <param name="activity_or_reply_id"></param>
''' <returns></returns>
Public Interface IInteractionLookup
    ''' <summary>
    ''' Given the ID of an ActivityPub boost, like, or reply, returns the
    ''' corresponding submission ID, or returns null if the activity or
    ''' object is not present on any submissions in Crowmask's cache.
    ''' </summary>
    ''' <param name="activity_or_reply_id">The ID of an Announce or Like activity, or the ID of a reply to an artwork submission</param>
    Function GetRelevantSubmitIdAsync(activity_or_reply_id As String) As Task(Of Integer?)
End Interface
