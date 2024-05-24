Imports System.Net.Http
Imports System.Net.Http.Json
Imports System.Text.RegularExpressions

Public Class Form1
    Private _client As New HttpClient()

    Private Async Function SyncSubmissionAsync() As Task
        Dim regex As New Regex("^https?://(www\.)?weasyl.com/~[^/]+/submissions/([0-9]+)")
        Dim match = regex.Match(TxtSubmissionUrl.Text)
        If Not match.Success Then
            MsgBox("The URL was not in a recognized format.", vbInformation)
            Exit Function
        End If

        Dim id As Integer
        If Not Integer.TryParse(match.Groups(2).Value, id) Then
            MsgBox("The submission ID must be numeric.", vbInformation)
            Exit Function
        End If

        Dim crowmaskUri As New Uri(TxtCrowmaskUrl.Text)
        Using req As New HttpRequestMessage(HttpMethod.Post, New Uri(crowmaskUri, $"/api/submissions/{id}/refresh?altText={Uri.EscapeDataString(TxtSubmissionAltText.Text)}"))
            req.Headers.Add("X-Weasyl-API-Key", TxtApiKey.Text)
            Using resp = Await _client.SendAsync(req)
                resp.EnsureSuccessStatusCode()
            End Using
        End Using
    End Function

    Private Async Sub BtnSubmissionSync_Click(sender As Object, e As EventArgs) Handles BtnSubmissionSync.Click
        sender.Enabled = False

        Try
            Await SyncSubmissionAsync()
        Finally
            sender.Enabled = True
        End Try
    End Sub

    Private Async Function SyncJournalAsync() As Task
        Dim regex As New Regex("^https?://(www\.)?weasyl.com/journal/([0-9]+)")
        Dim match = regex.Match(TxtJournalUrl.Text)
        If Not match.Success Then
            MsgBox("The URL was not in a recognized format.", vbInformation)
            Exit Function
        End If

        Dim id As Integer
        If Not Integer.TryParse(match.Groups(2).Value, id) Then
            MsgBox("The journal entry ID must be numeric.", vbInformation)
            Exit Function
        End If

        Dim crowmaskUri As New Uri(TxtCrowmaskUrl.Text)
        Using req As New HttpRequestMessage(HttpMethod.Post, New Uri(crowmaskUri, $"/api/journals/{id}/refresh"))
            req.Headers.Add("X-Weasyl-API-Key", TxtApiKey.Text)
            Using resp = Await _client.SendAsync(req)
                resp.EnsureSuccessStatusCode()
            End Using
        End Using
    End Function

    Private Async Sub BtnJournalSync_Click(sender As Object, e As EventArgs) Handles BtnJournalSync.Click
        sender.Enabled = False

        Try
            Await SyncJournalAsync()
        Finally
            sender.Enabled = True
        End Try
    End Sub

    Private Class Notification
        Public Property Category As String
        Public Property Action As String
        Public Property User As String
        Public Property Context As String
        Public Property Timestamp As DateTimeOffset
    End Class

    Private Async Function RefreshNotificationsAsync() As Task
        Dim crowmaskUri As New Uri(TxtCrowmaskUrl.Text)
        Using req As New HttpRequestMessage(HttpMethod.Get, New Uri(crowmaskUri, $"/api/notification-list"))
            req.Headers.Add("X-Weasyl-API-Key", TxtApiKey.Text)
            Using resp = Await _client.SendAsync(req)
                resp.EnsureSuccessStatusCode()

                Dim list = Await resp.Content.ReadFromJsonAsync(Of IEnumerable(Of Notification))
                DataGridView1.Rows.Clear()
                For Each notification In list
                    DataGridView1.Rows.Add(
                        notification.Category,
                        notification.Action,
                        notification.User,
                        notification.Context,
                        notification.Timestamp.ToLocalTime())
                Next
            End Using
        End Using
    End Function

    Private Async Sub BtnNotificationsRefresh_Click(sender As Object, e As EventArgs) Handles BtnNotificationsRefresh.Click
        sender.Enabled = False

        Try
            Await RefreshNotificationsAsync()
        Finally
            sender.Enabled = True
        End Try
    End Sub
End Class
