﻿# Crowmask 🐦‍⬛🎭

**Crowmask** is a combination ActivityPub server and Bluesky bot, written in
C# and F#, that runs on Azure Functions and Cosmos DB and mirrors artwork (and
optionally journal entries) from a [Weasyl](https://www.weasyl.com/) account.

Crowmask implements ActivityPub (server-to-server), exposing a user account
with the name, profile, and avatar of the attached Weasyl user (the user
associated with the Weasyl API key configured in the app's settings). Mastodon
and Pixelfed users can follow this account.

Crowmask can also connect to a Bluesky account, creating and deleting artwork
posts as needed (all posts are backdated, and posts for edited submissions are
deleted and re-created).

## Browsing

Crowmask is primarily an [ActivityPub](https://www.w3.org/TR/activitypub/)
server and bot, but it does include profile and submission pages. As long as
`ReturnHTML` is set to `true` in Program.cs, you can access these pages by
pointing a web browser at any of the ActivityPub URLs.

Certain parameters set in Program.cs affect content negotiation. By default,
Crowmask will return Markdown renditions of its pages to any user agent that
does not specify ActivityPub JSON or HTML in its `Accept` header, and it will
redirect web browsers that try to access the URLs for individual posts to the
corresponding Weasyl pages (but it will not do the same with the actor URL).

Crowmask implements ActivityPub, HTML, and Markdown responses through content
negotiation. The RSS and Atom feeds are implemented on the endpoint for page 1
of the outbox, but must be explicitly requested with `format=rss` or
`format=atom`.

## Notifications

Crowmask notifications come in three categories:

* ActivityPub activities (e.g. likes, announces)
* ActivityPub mentions and replies
* Bluesky notifications

ActivityPub data is stored in Crowmask's database, but the list of Bluesky
notifications it uses comes directly from Bluesky. There is no UI for viewing
these notifications, but Crowmask does provide its own API to let you fetch
them (see below).

## Implementation details

### Layers

* **Crowmask.ATProto**: a small Bluesky API client. Only implements functionality needed for Crowmask.
* **Crowmask.Data**: contains the data types and and data context, which map to documents in the Cosmos DB backend of EF Core.
* **Crowmask.LowLevel**:
  * determines when cached posts are considered stale;
  * converts data objects like `Submission` (which are specific to the database schema) to more general F# records, then to ActivityPub objects or Markdown / HTML pages;
  * maps Crowmask internal IDs to ActivityPub IDs;
  * talks to the Weasyl API;
  * and performs content negotiation.
* **Crowmask.HighLevel**:
  * **ATProto**: Creates, updates, and deletes Bluesky posts.
  * **Signatures**: HTTP signature validation.
  * **Remote**: Talks to other ActivityPub servers.
  * **FeedBuilder**: Implements RSS and Atom feeds.
  * **RemoteInboxLocator**: Collects inbox URLs for known users and servers.
  * **SubmissionCache**: Retrieves and updates submissions in Crowmask's database.
  * **UserCache**: Retrieves and updates the user profile in Crowmask's database.
* **Crowmask**: The main Azure Functions project, responsible for handling HTTP requests and running timed functions.

### Public HTTP endpoints

* `/.well-known/nodeinfo`: returns the location of the NodeInfo endpoint
* `/.well-known/webfinger`: returns information about the actor, if given the actor's URL or a handle representing the actor on either `CrowmaskHost` or `HandleHost`; otherwise, performs the same request on the domain indicated by `WebFingerDomain` (if any) and returns the result
* `/api/actor`: returns the `Person` object
* `/api/actor/followers`: contains the IDs of all followers (not paginated)
* `/api/actor/following`: an empty list
* `/api/actor/inbox`: processes incoming activities
* `/api/actor/nodeinfo`: returns a NodeInfo 2.2 response
* `/api/actor/outbox`: provides the number of submissions and a link to the first outbox page
* `/api/actor/outbox/page`: contains `Create` activities for known cached Weasyl posts, newest first; also handles Atom and RSS (20 per page)
* `/api/journals/{journalid}`: returns the resulting ActivityPub object
* `/api/submissions/{submitid}`: returns the resulting ActivityPub object

### Private HTTP endpoints

These functions must be authenticated using the `X-Weasyl-API-Key` header, and
the value of the header must be the same as the Weasyl API key that Crowmask
is configured to use.

* `POST /api/journals/{journalid}/refresh`: triggers a refresh of the journal entry
* `GET /api/notification-list`: returns a list of JSON objects representing interactions with content hosted by Crowmask
* `POST /api/submissions/{submitid}/refresh`: triggers a refresh of the submission
  * Optional query string parameter `alt=`: sets the alt text of the image

The project **Crowmask.AdminTools** is a VB.NET WinForms app that provides a
user interface for accessing these endpoints.

### Timed refresh functions

* `RefreshRecent` (every five minutes)
  * Cached posts with an upstream post date within the last hour (posts this recent are always considered stale)
* `RefreshStale` (every day at 12:00)
  * All stale cached posts
* `RefreshUpstreamFull` (every month on the 5th at 17:00)
  * All submissions in the user's Weasyl gallery
* `RefreshUpstreamNew` (every day at 16:15)
  * All submissions in the user's Weasyl gallery that are newer than the most recent cached post

### Other timed funtions

* `CheckBlueskyNotifications` (every six hours at :00)
* `RefreshProfile` (every day at 16:00)
* `SendOutbound` (every hour at :02)

### Additional information

Crowmask stands for "Content Read Off Weasyl: Modified ActivityPub Starter Kit". It began as an attempt
to port [ActivityPub Starter Kit](https://github.com/jakelazaroff/activitypub-starter-kit) to .NET, but
was quickly modified to support the strongly typed nature of .NET and the specificity of this app.
Still, having a simple, working ActivityPub implementation in a language I was able to understand (if
not compile) was incredibly helpful, as was running a [microblog.pub](`https://docs.microblog.pub/`)
instance and being able to see the logs.

HTTP signature validation is adapted from [Letterbook](https://github.com/Letterbook/Letterbook).

Example `local.settings.json`:

    {
      "IsEncrypted": false,
      "Values": {
        "CosmosDBAccountEndpoint": "https://example.documents.azure.com:443/",
        "CosmosDBAccountKey": "...",
        "CrowmaskHost": "crowmask.example.com",
        "HandleHost": "crowmask.example.com",
        "HandleName": "activitypub-username-here",
        "KeyVaultHost": "crowmask.vault.azure.net",
        "WeasylApiKey": "...",
        "ATProtoPDS": "conocybe.us-west.host.bsky.network",
        "ATProtoHandle": "example.bsky.social",
        "ATProtoIdentifier": "username@example.com",
        "ATProtoPassword": "xxxxxxxxxxxxxx",
        "AzureWebJobsStorage": "UseDevelopmentStorage=true",
        "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"
      }
    }

Settings prefixed with `ATProto` can be omitted if you don't need the Bluesky
bot. Crowmask stores Bluesky tokens in its database, so `ATProtoIdentifier`
and `ATProtoPassword` should be removed once tokens are established.

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
