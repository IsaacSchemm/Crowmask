Imports Crowmask.DomainModeling

Public Interface IInteractionSummarizer
    Function ToMarkdown(post As Post, interaction As Interaction) As String
    Function ToHtml(post As Post, interaction As Interaction) As String
End Interface
