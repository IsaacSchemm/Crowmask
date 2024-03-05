''' <summary>
''' Provides the name and version number of the application.
''' </summary>
Public Interface IApplicationInformation
    ''' <summary>
    ''' The application name (e.g. "Crowmask").
    ''' </summary>
    ''' <returns></returns>
    ReadOnly Property ApplicationName As String

    ''' <summary>
    ''' The Crowmask version number.
    ''' </summary>
    ReadOnly Property VersionNumber As String

    ''' <summary>
    ''' The host / domain name used by Crowmask.
    ''' May or may not be the same as the domain in the actor's handle.
    ''' </summary>
    ReadOnly Property ApplicationHostname As String

    ''' <summary>
    ''' A URL to a website with more information about the application.
    ''' </summary>
    ''' <returns></returns>
    ReadOnly Property WebsiteUrl As String

    ''' <summary>
    ''' The user agent string for outgoing requests.
    ''' </summary>
    ''' <returns></returns>
    ReadOnly Property UserAgent As String

    ''' <summary>
    ''' The username of the Crowmask actor (used in the @ handle).
    ''' </summary>
    ReadOnly Property Username As String

    ''' <summary>
    ''' The host / domain name used in the Crowmask actor's preferred handle.
    ''' May or may not be the same as Crowmask's domain.
    ''' </summary>
    ReadOnly Property HandleHostname As String

    ''' <summary>
    ''' The ActivityPub ID of the admin actors, ActivityPub users who should
    ''' be notified when Crowmask recieves a boost, like, reply, or mention.
    ''' </summary>
    ReadOnly Property AdminActorIds As IEnumerable(Of String)

    ''' <summary>
    ''' Whether the HTML (text/html) user interface of Crowmask is enabled.
    ''' </summary>
    ''' <returns></returns>
    ReadOnly Property ReturnHTML As Boolean

    ''' <summary>
    ''' Whether the Markdown (text/plain) user interface of Crowmask is enabled.
    ''' </summary>
    ''' <returns></returns>
    ReadOnly Property ReturnMarkdown As Boolean

    ''' <summary>
    ''' Whether requests from web browsers to individual posts or to the actor
    ''' URL should redirect to Weasyl.
    ''' </summary>
    ''' <returns></returns>
    ReadOnly Property UpstreamRedirect As Boolean

    ''' <summary>
    ''' A list of Bluesky accounts that Crowmask should create and delete
    ''' posts on (mirroring its ActivityPub posts).
    ''' </summary>
    ''' <returns></returns>
    ReadOnly Property BlueskyBotAccounts As IEnumerable(Of IBlueskyAccountConfiguration)
End Interface
