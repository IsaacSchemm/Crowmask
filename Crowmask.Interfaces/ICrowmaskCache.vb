Imports Crowmask.DomainModeling

Public Interface ICrowmaskCache
    Function GetSubmissionAsync(submitid As Integer) As Task(Of CacheResult)

    Function UpdateSubmissionAsync(submitid As Integer) As Task(Of CacheResult)

    Function GetJournalAsync(journalid As Integer) As Task(Of CacheResult)

    Function UpdateJournalAsync(journalid As Integer) As Task(Of CacheResult)

    Function GetCachedPostAsync(identifier As JointIdentifier) As Task(Of CacheResult)

    Function GetCachedPostCountAsync() As Task(Of Integer)

    Function GetAllCachedPostsAsync() As IAsyncEnumerable(Of Post)

    Function GetRelevantCachedPostsAsync(activity_or_reply_id As String) As IAsyncEnumerable(Of Post)

    Function GetUserAsync() As Task(Of Person)

    Function UpdateUserAsync() As Task(Of Person)
End Interface
