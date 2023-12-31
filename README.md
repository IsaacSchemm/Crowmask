﻿# Crowmask 🐦‍⬛🎭

**Crowmask** is a single-user ActivityPub bridge for Weasyl, implemented
using Azure Functions and Azure Cosmos DB. It is intended for users who:

* post artwork to Weasyl
* have an ActivityPub account elsewhere (e.g. Mastodon) but want to keep
  artwork on a separate account
* have an Azure account, are familiar with deploying .NET web apps, and are
  comfortable setting things up in the portal

Crowmask is written mostly in C# with some parts in F#.

The Crowmask server is configured with a Weasyl API key and generates a single
automated user account, using the name, info, avatar, and submissions of the
Weasyl user who created the API key. This user account can be followed by
users on Mastodon, Pixelfed, and microblog.pub, among others.

Submissions (retrieved from the Weasyl API) are mapped to `Note` objects, and
journals (scraped from the website using the Weasyl API key as authentication)
are mapped to `Article` objects.

When a user likes, replies to, or shares/boosts one of this account's posts,
Crowmask will send a private message to the "admin actor" defined in its
configuration variables and shown on its profile page.

Outgoing calls (like "accept follow" or "create new post") are processed every
five minutes. Weasyl profile data and recent submissions are updated hourly;
older submissions are updated daily.

## Browsing

Crowmask is primarily an [ActivityPub](https://www.w3.org/TR/activitypub/)
server, but it does include profile, submission list, and submission pages,
which you can access by pointing a web browser (or another non-ActivityPub
user agent) at the actor, outbox, and object URIs, respectively. For example,
the URL `/api/actor` will send ActivityPub info to a program that asks for it
(in the `Accept` header), but it will send an HTML profile page to a web
browser, and a Markdown version of that page to something like `curl` that
doesn't indicate any particular media type.

## Implementation details

Layers:

* **Crowmask.Merging**: code to merge the results of multiple asynchronous
  sequences into a single sequence by taking the newest items first. Used to
  combine submissions and journals into a single feed.
* **Crowmask.Data**: contains the data storage types and and data context,
  which map to documents in Cosmos DB using the Cosmos DB backend of EF Core.
* **Crowmask.Weasyl**: used to connect to the Weasyl API and retrieve user and
  submission information.
* **Crowmask.DomainModeling**: converts data objects like `Submission` (which
  are specific to Weasyl and the database schema), to more general F# records
  with only the properties needed to expose the information via ActivityPub or
  Markdown.
* **Crowmask.IdMapping**: contains the `ActivityStreamsIdMapper`, from which
  other code can derive the public IDs / URIs for objects in the application.
* **Crowmask.InteractionSummaries**: provides Markdown and HTML summaries of
  interactions with posts (boosts, likes, and replies) which are shown on the
  post page and sent in private messages to the admin actor.
* **Crowmask.ActivityPub**: converts domain model objects to ActivityPub
  objects (represented as `IDictionary<string, object>`); serializes these
  objects to LD-JSON (by manually adding the `@context`), and creates
  private messages to the admin actor.
* **Crowmask.Cache**: Retrieves new user or submission information from Weasyl
  when necessary, and creates ActivityPub `Update` activities when the
  ActivityPub representation of a user or submission changes.
* **Crowmask.Signatures**: HTTP signature validation, adapted from
  [Letterbook](https://github.com/Letterbook/Letterbook).
* **Crowmask.Remote**: Talks to other ActivityPub servers.
* **Crowmask.Feed**: Implements RSS and Atom feeds.
* **Crowmask.Markdown**: Implements the web UI by creating Markdown and HTML
  representations of data available through ActivityPub `GET` endpoints, and
  helps perform content negotiation with the `Accept` header.
* **Crowmask**: The main Azure Functions project, responsible for handling
  HTTP requests and running timed functions.

Configuration values are passed using custom singleton dependencies:

* `DomainModeling.IAdminActor`: provides the admin actor's ActivityPub ID. Implemented in `Program.cs` from the configuration value `AdminActor`.
* `DomainModeling.ICrowmaskHost`: provides the domain name that Crowmask is expected to run on. Implemented in `Program.cs` from the configuration value `CrowmaskHost`.
* `DomainModeling.IHandleHost`: provides the domain name used in the Crowmask actor's handle. (Can be the same as the Crowmask domain.) Implemented in `Program.cs` from the configuration value `HandleHost`.
* `Remote.ISigner`: signs HTTP requests. Implemented by `KeyProvider`, which uses the key from Azure Key Vault specified in `KeyVaultHost`.
* `Cache.IPublicKeyProvider`: provides the actor's public key in PEM format. Implemented by `KeyProvider`, which uses the key from Azure Key Vault specified in `KeyVaultHost`.
* `Weasyl.IWeasylApiKeyProvider`: provides the Weasyl API key. Implemented in `Program.cs` from the configuration value `WeasylApiKey`.

Internal objects include:

* `User`: a cached user from Weasyl, along with information about when Crowmask last attempted to refresh it and when it was last refreshed
* `Submission`: a cached artwork post from Weasyl, along with information about when Crowmask last attempted to refresh it and when it was last refreshed
* `Journal`: a cached journal entry from Weasyl, along with information about when Crowmask last attempted to refresh it and when it was last refreshed
* `Follower`: an ActivityPub actor who follows this actor
* `OutboundActivity`: a list of unsent `Accept`, `Announce`, `Undo`, `Create`, `Update`, and `Delete` activities sent to particular actors or instances

Other objects in Crowmask.Data, like `SubmissionTag` and `UserAvatar`, are
stored as arrays within the parent document in Cosmos DB.

HTTP endpoints:

* `/.well-known/nodeinfo`: returns the location of the NodeInfo endpoint
* `/.well-known/webfinger`: returns information about the actor, if given the actor's URL or a handle representing the actor on either `CrowmaskHost` or `HandleHost`; otherwise, redirects to the same path on the admin actor's domain
* `/api/actor`: returns the `Person` object
* `/api/actor/followers`: contains the IDs of all followers (not paginated)
* `/api/actor/following`: an empty list
* `/api/actor/inbox`: processes the following activities:
    * `Follow`: adds a new follower (or updates the `Follow` ID of an existing follower)
    * `Undo`: undoes a `Follow`, `Announce`, or `Like`
    * `Like`: if it matches a `Submission`, records the like, then sends a transient message to the admin actor
    * `Announce`: if it matches a `Submission`, records the boost, then sends a transient message to the admin actor
    * `Create`: if it's for a reply to a `Submission`, records the new message, then sends a transient message to the admin
    * `Delete`: if it's for a reply to a `Submission` that was recorded earlier, deletes that reply
* `/api/actor/nodeinfo`: returns a NodeInfo 2.2 response
* `/api/actor/outbox`: provides the number of submissions and a link to the first outbox page
* `/api/actor/outbox/page`: contains `Create` activities for known cached Weasyl posts, newest first; also handles Atom and RSS (20 per page)
* `/api/submissions/{submitid}`: returns the resulting `Note` object
    * With `?view=comments`: returns a `Collection` with object IDs for all replies (not paginated)
    * With `?view=likes`: returns a `Collection` with activity IDs for all likes (not paginated)
    * With `?view=shares`: returns a `Collection` with activity IDs for all boosts (not paginated)
* `/api/journals/{journalid}`: returns the resulting `Article` object
    * With `?view=comments`: returns a `Collection` with object IDs for all replies (not paginated)
    * With `?view=likes`: returns a `Collection` with activity IDs for all likes (not paginated)
    * With `?view=shares`: returns a `Collection` with activity IDs for all boosts (not paginated)

Timed functions:

* `RefreshUpstream`: Checks Weasyl for recent posts (stopping when a post is more than a day old), updating cache as needed, then sends outbound activities (every ten minutes)
* `RefreshCached`: Attempts cache refresh for all stale cached posts and the actor's name/avatar/etc (every day at 23:56)
* `OutboundActivityCleanup`: Remove any unsent `OutboundActivity` objects more than 7 days old (every hour)

Note that a cached submission will not be refreshed if:

* the last attempted cache refresh was within the past 4 minutes
* the last successful cache refresh was within the past hour, and the post is more than an hour old
* the last successful cache refresh was within the past 7 days, and the post is more than 7 days old
* the last successful cache refresh was within the past 28 days, and the post is more than 28 days old

A cached user (name, icon, etc.) will not be refreshed if:

* the last attempted cache refresh was within the past 4 minutes
* the last successful cache refresh was within the past hour

Cache refresh occurs as part of `RefreshUpstream` (for recent items), `RefreshCached`,
or when a specific item is requested that is not yet in the cache.

Crowmask stands for "Content Read Off Weasyl: Modified ActivityPub Starter Kit". It began as an attempt
to port [ActivityPub Starter Kit](https://github.com/jakelazaroff/activitypub-starter-kit) to .NET, but
was quickly modified to support the strongly typed nature of .NET and the specificity of this app.
Still, having a simple, working ActivityPub implementation in a language I was able to understand (if
not compile) was incredibly helpful.

See also [bird.makeup](https://sr.ht/~cloutier/bird.makeup/), which is another
ActivityPub bridge written in C# / .NET.

Example `local.settings.json`:

    {
      "IsEncrypted": false,
      "Values": {
        "AdminActor": "https://pixelfed.example.com/users/...",
        "CosmosDBAccountEndpoint": "https://example.documents.azure.com:443/",
        "CosmosDBAccountKey": "...",
        "CrowmaskHost": "crowmask.example.com",
        "HandleHost": "crowmask.example.com",
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
