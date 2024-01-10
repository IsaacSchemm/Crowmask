Imports Crowmask.DomainModeling

Public Interface IFeedBuilder
    Function ToRssFeed(person As Person, posts As IEnumerable(Of Post)) As String

    Function ToAtomFeed(person As Person, posts As IEnumerable(Of Post)) As String
End Interface
