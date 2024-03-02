# Crowmask 🐦‍⬛🎭

**Crowmask** is a single-user ActivityPub bridge for Weasyl, implemented
using Azure Functions and Azure Cosmos DB. It is intended for users who:

* post artwork to Weasyl
* have an ActivityPub account elsewhere (e.g. Mastodon) but want to keep
  artwork on a separate account
* have an Azure account, are familiar with deploying .NET web apps from Visual
 Studio, and are comfortable setting things up in the Azure portal

Crowmask is written mostly in C# with some parts in F#.

The Crowmask server is configured with a Weasyl API key and generates a single
automated user account, using the name, info, avatar, and submissions of the
Weasyl user who created the API key. This user account can be followed by
users on Mastodon, Pixelfed, and microblog.pub, among others.

Submissions (retrieved from the Weasyl API) are mapped to `Note` objects that
contain the title (as a link to Weasyl), description, and tags. Images will be
included for visual submissions.

When a user likes, replies to, or shares/boosts one of this account's posts,
or tags the Crowmask actor in a post, Crowmask will send a private message to
the "admin actors" defined in its configuration variables and shown on its
profile page.

Outgoing activities (like "accept follow" or "create new post") are processed
every minute. Submissions are updated periodically, ranging from every ten
minutes (for submissions less than an hour old) to every 28 days (for those
over 28 days old). User profile data is updated hourly.

## Browsing

Crowmask is primarily an [ActivityPub](https://www.w3.org/TR/activitypub/)
server, but it does include profile and submission pages, among others. If
`ReturnHTML` is set to `true` in Program.cs, you can access these pages by
pointing a web browser at any of the ActivityPub URLs.

If `ReturnMarkdown` is set to `true` in Program.cs, Crowmask will return
Markdown renditions of these pages to user agents that do not request a more
specific content type.

If `UpstreamRedirect` is set to `true` in Program.cs, Crowmask will redirect
actor and post URLs to the equivalent Weasyl pages for any web browsers that
request them.

Crowmask implements ActivityPub, HTML, and Markdown responses through content
negotiation. The RSS and Atom feeds are implemented on the endpoint for page 1
of the outbox, but must be explicitly requested with `format=rss` or
`format=atom`, as (historically) some browsers have sent Accept headers that
explicitly prefer `application/xml` over `text/html`.

## Implementation details

Layers:

* **Crowmask.Interfaces** (VB.NET): contains interfaces used to pass config
  values between layers or to allow inner layers to call outer-layer code.
* **Crowmask.Data** (C#): contains the data types and and data context, which
  map to documents in the Cosmos DB backend of EF Core.
* **Crowmask.LowLevel** (F#): converts data objects like `Submission` (which
  are specific to the database schema) to more general F# records, then to
  ActivityPub objects or Markdown / HTML pages; maps Crowmask internal IDs to
  ActivityPub IDs; and talks to the Weasyl API.
* **Crowmask.HighLevel** (C#):
    * **Signatures**: HTTP signature validation, adapted from
      [Letterbook](https://github.com/Letterbook/Letterbook).
    * **Remote**: Talks to other ActivityPub servers.
    * **FeedBuilder**: Implements RSS and Atom feeds.
    * **RemoteInboxLocator**: Collects inbox URLs for the admin actors, followers, and other known servers.
    * **SubmissionCache**: Retrieves and updates submissions in Crowmask's database.
    * **UserCache**: Retrieves and updates the user profile in Crowmask's database.
* **Crowmask** (C#): The main Azure Functions project, responsible for
  handling HTTP requests and running timed functions.

HTTP endpoints:

* `/.well-known/nodeinfo`: returns the location of the NodeInfo endpoint
* `/.well-known/webfinger`: returns information about the actor, if given the actor's URL or a handle representing the actor on either `CrowmaskHost` or `HandleHost`; otherwise, redirects to the same path on the domain of the first admin actor
* `/api/actor`: returns the `Person` object
* `/api/actor/followers`: contains the IDs of all followers (not paginated)
* `/api/actor/following`: an empty list
* `/api/actor/inbox`: processes incoming activities
* `/api/actor/nodeinfo`: returns a NodeInfo 2.2 response
* `/api/actor/outbox`: provides the number of submissions and a link to the first outbox page
* `/api/actor/outbox/page`: contains `Create` activities for known cached Weasyl posts, newest first; also handles Atom and RSS (20 per page)
* `/api/submissions/{submitid}`: returns the resulting `Note` object
* `/api/interactions/{guid}/notification`: shows the (unlisted) notification sent to the admin actors
* `/api/mentions/{guid}/notification`: shows the (unlisted) notification sent to the admin actors

Timed functions:

* `RefreshCache` (every day at 12:00)
* `RefreshProfile` (every hour at :05)
* `RefreshRecent` (every ten minutes)
* `RefreshUpstream` (every month on the 1st at 17:00)
* `SendOutbound` (every hour at :02)

Crowmask stands for "Content Read Off Weasyl: Modified ActivityPub Starter Kit". It began as an attempt
to port [ActivityPub Starter Kit](https://github.com/jakelazaroff/activitypub-starter-kit) to .NET, but
was quickly modified to support the strongly typed nature of .NET and the specificity of this app.
Still, having a simple, working ActivityPub implementation in a language I was able to understand (if
not compile) was incredibly helpful.

Example `local.settings.json`:

    {
      "IsEncrypted": false,
      "Values": {
        "AdminActor": "https://pixelfed.example.com/users/...",
        "CosmosDBAccountEndpoint": "https://example.documents.azure.com:443/",
        "CosmosDBAccountKey": "...",
        "CrowmaskHost": "crowmask.example.com",
        "HandleHost": "crowmask.example.com",
        "HandleName": "activitypub-username-here",
        "KeyVaultHost": "crowmask.vault.azure.net",
        "WeasylApiKey": "...",
        "AzureWebJobsStorage": "UseDevelopmentStorage=true",
        "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"
      }
    }

For **Key Vault**, the app is set up to use Managed Identity - turn this on in
the Function App (Settings > Identity) then go to the key vault's access
control (IAM) tab to give a role assignment of Key Vault Crypto User to that
new managed identity.

For **Cosmos DB**, you will need to create the container in Data Explorer:

* Database ID: `Crowmask`
* Container ID: `CrowmaskDbContext`
* Partition key: `__partitionKey`

If you don't want to include `CosmosDBAccountKey`, then Crowmask will try to
use role-based access control (RBAC) via `DefaultAzureCredential`. In that
case, run something like this in Azure CLI (PowerShell) to give the
appropriate permissions to the function app's managed identity:

    az login
    az cosmosdb sql role assignment create --account-name {...} --resource-group {...} --scope "/" --principal-id {...} --role-definition-id 00000000-0000-0000-0000-000000000002

The `{...}` for the principal ID should be the ID shown in the Identity tab of
the function app settings.
