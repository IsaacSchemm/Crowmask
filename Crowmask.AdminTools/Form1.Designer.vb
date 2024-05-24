<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class Form1
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        GroupBox1 = New GroupBox()
        BtnSubmissionSync = New Button()
        TxtSubmissionAltText = New TextBox()
        Label3 = New Label()
        TxtSubmissionUrl = New TextBox()
        Label2 = New Label()
        GroupBox2 = New GroupBox()
        BtnJournalSync = New Button()
        TxtJournalUrl = New TextBox()
        Label4 = New Label()
        GroupBox3 = New GroupBox()
        DataGridView1 = New DataGridView()
        Category = New DataGridViewTextBoxColumn()
        Action = New DataGridViewTextBoxColumn()
        User = New DataGridViewTextBoxColumn()
        Context = New DataGridViewTextBoxColumn()
        Timestamp = New DataGridViewTextBoxColumn()
        BtnNotificationsRefresh = New Button()
        NumericUpDown1 = New NumericUpDown()
        Label5 = New Label()
        GroupBox4 = New GroupBox()
        TxtApiKey = New TextBox()
        Label7 = New Label()
        TxtCrowmaskUrl = New TextBox()
        Label6 = New Label()
        GroupBox1.SuspendLayout()
        GroupBox2.SuspendLayout()
        GroupBox3.SuspendLayout()
        CType(DataGridView1, ComponentModel.ISupportInitialize).BeginInit()
        CType(NumericUpDown1, ComponentModel.ISupportInitialize).BeginInit()
        GroupBox4.SuspendLayout()
        SuspendLayout()
        ' 
        ' GroupBox1
        ' 
        GroupBox1.AutoSize = True
        GroupBox1.AutoSizeMode = AutoSizeMode.GrowAndShrink
        GroupBox1.Controls.Add(BtnSubmissionSync)
        GroupBox1.Controls.Add(TxtSubmissionAltText)
        GroupBox1.Controls.Add(Label3)
        GroupBox1.Controls.Add(TxtSubmissionUrl)
        GroupBox1.Controls.Add(Label2)
        GroupBox1.Location = New Point(12, 114)
        GroupBox1.Name = "GroupBox1"
        GroupBox1.Size = New Size(300, 125)
        GroupBox1.TabIndex = 1
        GroupBox1.TabStop = False
        GroupBox1.Text = "Sync Submission / Set Alt Text"
        ' 
        ' BtnSubmissionSync
        ' 
        BtnSubmissionSync.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        BtnSubmissionSync.Location = New Point(219, 80)
        BtnSubmissionSync.Name = "BtnSubmissionSync"
        BtnSubmissionSync.Size = New Size(75, 23)
        BtnSubmissionSync.TabIndex = 5
        BtnSubmissionSync.Text = "Sync"
        BtnSubmissionSync.UseVisualStyleBackColor = True
        ' 
        ' TxtSubmissionAltText
        ' 
        TxtSubmissionAltText.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        TxtSubmissionAltText.Location = New Point(80, 51)
        TxtSubmissionAltText.Name = "TxtSubmissionAltText"
        TxtSubmissionAltText.Size = New Size(214, 23)
        TxtSubmissionAltText.TabIndex = 3
        ' 
        ' Label3
        ' 
        Label3.AutoSize = True
        Label3.Location = New Point(6, 54)
        Label3.Name = "Label3"
        Label3.Size = New Size(45, 15)
        Label3.TabIndex = 2
        Label3.Text = "Alt text"
        ' 
        ' TxtSubmissionUrl
        ' 
        TxtSubmissionUrl.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        TxtSubmissionUrl.Location = New Point(80, 22)
        TxtSubmissionUrl.Name = "TxtSubmissionUrl"
        TxtSubmissionUrl.Size = New Size(214, 23)
        TxtSubmissionUrl.TabIndex = 1
        ' 
        ' Label2
        ' 
        Label2.AutoSize = True
        Label2.Location = New Point(6, 25)
        Label2.Name = "Label2"
        Label2.Size = New Size(68, 15)
        Label2.TabIndex = 0
        Label2.Text = "Weasyl URL"
        ' 
        ' GroupBox2
        ' 
        GroupBox2.AutoSize = True
        GroupBox2.AutoSizeMode = AutoSizeMode.GrowAndShrink
        GroupBox2.Controls.Add(BtnJournalSync)
        GroupBox2.Controls.Add(TxtJournalUrl)
        GroupBox2.Controls.Add(Label4)
        GroupBox2.Location = New Point(12, 247)
        GroupBox2.Name = "GroupBox2"
        GroupBox2.Size = New Size(300, 97)
        GroupBox2.TabIndex = 2
        GroupBox2.TabStop = False
        GroupBox2.Text = "Sync Journal Entry"
        ' 
        ' BtnJournalSync
        ' 
        BtnJournalSync.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        BtnJournalSync.Location = New Point(219, 52)
        BtnJournalSync.Name = "BtnJournalSync"
        BtnJournalSync.Size = New Size(75, 23)
        BtnJournalSync.TabIndex = 8
        BtnJournalSync.Text = "Sync"
        BtnJournalSync.UseVisualStyleBackColor = True
        ' 
        ' TxtJournalUrl
        ' 
        TxtJournalUrl.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        TxtJournalUrl.Location = New Point(80, 23)
        TxtJournalUrl.Name = "TxtJournalUrl"
        TxtJournalUrl.Size = New Size(214, 23)
        TxtJournalUrl.TabIndex = 7
        ' 
        ' Label4
        ' 
        Label4.AutoSize = True
        Label4.Location = New Point(6, 26)
        Label4.Name = "Label4"
        Label4.Size = New Size(68, 15)
        Label4.TabIndex = 6
        Label4.Text = "Weasyl URL"
        ' 
        ' GroupBox3
        ' 
        GroupBox3.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        GroupBox3.Controls.Add(DataGridView1)
        GroupBox3.Controls.Add(BtnNotificationsRefresh)
        GroupBox3.Controls.Add(NumericUpDown1)
        GroupBox3.Controls.Add(Label5)
        GroupBox3.Location = New Point(318, 114)
        GroupBox3.Name = "GroupBox3"
        GroupBox3.Size = New Size(554, 332)
        GroupBox3.TabIndex = 3
        GroupBox3.TabStop = False
        GroupBox3.Text = "Notifications"
        ' 
        ' DataGridView1
        ' 
        DataGridView1.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        DataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
        DataGridView1.Columns.AddRange(New DataGridViewColumn() {Category, Action, User, Context, Timestamp})
        DataGridView1.Location = New Point(6, 51)
        DataGridView1.Name = "DataGridView1"
        DataGridView1.Size = New Size(542, 275)
        DataGridView1.TabIndex = 3
        ' 
        ' Category
        ' 
        Category.HeaderText = "Category"
        Category.Name = "Category"
        ' 
        ' Action
        ' 
        Action.HeaderText = "Action"
        Action.Name = "Action"
        ' 
        ' User
        ' 
        User.HeaderText = "User"
        User.Name = "User"
        ' 
        ' Context
        ' 
        Context.HeaderText = "Context"
        Context.Name = "Context"
        ' 
        ' Timestamp
        ' 
        Timestamp.HeaderText = "Timestamp"
        Timestamp.Name = "Timestamp"
        ' 
        ' BtnNotificationsRefresh
        ' 
        BtnNotificationsRefresh.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        BtnNotificationsRefresh.Location = New Point(473, 22)
        BtnNotificationsRefresh.Name = "BtnNotificationsRefresh"
        BtnNotificationsRefresh.Size = New Size(75, 23)
        BtnNotificationsRefresh.TabIndex = 2
        BtnNotificationsRefresh.Text = "Refresh"
        BtnNotificationsRefresh.UseVisualStyleBackColor = True
        ' 
        ' NumericUpDown1
        ' 
        NumericUpDown1.Location = New Point(52, 22)
        NumericUpDown1.Maximum = New Decimal(New Integer() {999, 0, 0, 0})
        NumericUpDown1.Minimum = New Decimal(New Integer() {1, 0, 0, 0})
        NumericUpDown1.Name = "NumericUpDown1"
        NumericUpDown1.Size = New Size(60, 23)
        NumericUpDown1.TabIndex = 1
        NumericUpDown1.Value = New Decimal(New Integer() {20, 0, 0, 0})
        ' 
        ' Label5
        ' 
        Label5.AutoSize = True
        Label5.Location = New Point(6, 25)
        Label5.Name = "Label5"
        Label5.Size = New Size(40, 15)
        Label5.TabIndex = 0
        Label5.Text = "Count"
        ' 
        ' GroupBox4
        ' 
        GroupBox4.AutoSizeMode = AutoSizeMode.GrowAndShrink
        GroupBox4.Controls.Add(TxtApiKey)
        GroupBox4.Controls.Add(Label7)
        GroupBox4.Controls.Add(TxtCrowmaskUrl)
        GroupBox4.Controls.Add(Label6)
        GroupBox4.Location = New Point(12, 12)
        GroupBox4.Name = "GroupBox4"
        GroupBox4.Size = New Size(776, 96)
        GroupBox4.TabIndex = 0
        GroupBox4.TabStop = False
        GroupBox4.Text = "Authorization"
        ' 
        ' TxtApiKey
        ' 
        TxtApiKey.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        TxtApiKey.Location = New Point(99, 51)
        TxtApiKey.Name = "TxtApiKey"
        TxtApiKey.Size = New Size(671, 23)
        TxtApiKey.TabIndex = 5
        ' 
        ' Label7
        ' 
        Label7.AutoSize = True
        Label7.Location = New Point(6, 54)
        Label7.Name = "Label7"
        Label7.Size = New Size(86, 15)
        Label7.TabIndex = 4
        Label7.Text = "Weasyl API key"
        ' 
        ' TxtCrowmaskUrl
        ' 
        TxtCrowmaskUrl.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        TxtCrowmaskUrl.Location = New Point(99, 22)
        TxtCrowmaskUrl.Name = "TxtCrowmaskUrl"
        TxtCrowmaskUrl.Size = New Size(671, 23)
        TxtCrowmaskUrl.TabIndex = 3
        TxtCrowmaskUrl.Text = "https://crowmask.example.com"
        ' 
        ' Label6
        ' 
        Label6.AutoSize = True
        Label6.Location = New Point(6, 25)
        Label6.Name = "Label6"
        Label6.Size = New Size(87, 15)
        Label6.TabIndex = 2
        Label6.Text = "Crowmask URL"
        ' 
        ' Form1
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(884, 461)
        Controls.Add(GroupBox4)
        Controls.Add(GroupBox3)
        Controls.Add(GroupBox2)
        Controls.Add(GroupBox1)
        Name = "Form1"
        Text = "Crowmask Admin Tools"
        GroupBox1.ResumeLayout(False)
        GroupBox1.PerformLayout()
        GroupBox2.ResumeLayout(False)
        GroupBox2.PerformLayout()
        GroupBox3.ResumeLayout(False)
        GroupBox3.PerformLayout()
        CType(DataGridView1, ComponentModel.ISupportInitialize).EndInit()
        CType(NumericUpDown1, ComponentModel.ISupportInitialize).EndInit()
        GroupBox4.ResumeLayout(False)
        GroupBox4.PerformLayout()
        ResumeLayout(False)
        PerformLayout()
    End Sub
    Friend WithEvents GroupBox1 As GroupBox
    Friend WithEvents GroupBox2 As GroupBox
    Friend WithEvents TxtSubmissionUrl As TextBox
    Friend WithEvents Label2 As Label
    Friend WithEvents BtnSubmissionSync As Button
    Friend WithEvents TxtSubmissionAltText As TextBox
    Friend WithEvents Label3 As Label
    Friend WithEvents BtnJournalSync As Button
    Friend WithEvents TxtJournalUrl As TextBox
    Friend WithEvents Label4 As Label
    Friend WithEvents GroupBox3 As GroupBox
    Friend WithEvents NumericUpDown1 As NumericUpDown
    Friend WithEvents Label5 As Label
    Friend WithEvents BtnNotificationsRefresh As Button
    Friend WithEvents DataGridView1 As DataGridView
    Friend WithEvents Category As DataGridViewTextBoxColumn
    Friend WithEvents Action As DataGridViewTextBoxColumn
    Friend WithEvents User As DataGridViewTextBoxColumn
    Friend WithEvents Context As DataGridViewTextBoxColumn
    Friend WithEvents Timestamp As DataGridViewTextBoxColumn
    Friend WithEvents GroupBox4 As GroupBox
    Friend WithEvents TxtApiKey As TextBox
    Friend WithEvents Label7 As Label
    Friend WithEvents TxtCrowmaskUrl As TextBox
    Friend WithEvents Label6 As Label

End Class
