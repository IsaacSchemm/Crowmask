# Crowmask 🐦‍⬛🎭

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
oesn't indicate any particular media type.

## Missing functionality

* Crowmask does not verify HTTP signatures. (This turns out to be a difficult
  thing to do, and I wasn't able to find a .NET library that could handle this
  easily in an ActivityPub context.) This means it's possible to forge
  "follow", "unfollow", "like", "share", and "create reply" activites
  (although the last three simply send a message to the admin actor and are
  then discarded.)
* Crowmask does not keep track of interaction with posts (beyond notifying the
  admin actor).
* Weasyl tags are ignored in Crowmask, instead of being translated into
  Mastodon-style hashtags.

## Implementation details

Layers:

* **Crowmask.Merging**: used to merge the results of multiple asynchronous
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
* **Crowmask.ActivityPub**: converts domain model objects to ActivityPub
  objects (represented as `IDictionary<string, object>`); serializes these
  objects to LD-JSON (by manually adding the `@context`), and creates
  private messages to the admin actor.
* **Crowmask.Cache**: Retrieves new user or submission information from Weasyl
  when necessary, and creates ActivityPub `Update` activities when the
  ActivityPub representation of a user or submission changes.
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

Internal objects:

- [x] `User`: a cached user from Weasyl, along with information about when Crowmask last attempted to refresh it and when it was last refreshed
- [x] `UserLink`: an entry from the Contact and Social Media section of the Weasyl profile
- [x] `Submission`: a cached post from Weasyl, along with information about when Crowmask last attempted to refresh it and when it was last refreshed
- [x] `SubmissionMedia`: an associated image
- [x] `SubmissionTag`: an associated tag
- [x] `Follower`: an ActivityPub actor who follows this actor
- [x] `OutboundActivity`: a list of unsent `Accept`, `Announce`, `Undo`, `Create`, `Update`, and `Delete` activities sent to particular actors or instances

ActivityPub HTTP endpoints:

- [x] `/.well-known/webfinger`: returns information about the actor, if given the actor's URL or a handle representing the actor on either `CrowmaskHost` or `HandleHost`; otherwise, redirects to the same path on the admin actor's domain
- [x] `/api/actor`: attempts cache refresh for the user, then returns the resulting object
- [x] `/api/actor/inbox`: processes the following activities:
    - [x] `Follow`: adds a new follower (or updates the `Follow` ID of an existing follower)
    - [x] `Undo` with `Follow`: removes the follower with the given `Follow` ID (if any)
    - [x] `Like`: if it matches a `Submission`, sends a transient message to the admin actor, with a link to the actor who sent the `Like`
    - [x] `Announce`: if it matches a `Submission`, sends a transient message to the admin actor, with a link to the actor who sent the `Announce`
    - [x] `Create`: if it's for a reply to a `Submission`, sends a transient message to the admin, with a link to the reply
- [x] `/api/actor/outbox`: contains links to the first and last gallery page
- [x] `/api/actor/outbox/page`: contains `Create` activities for known Weasyl posts (20 per page)
- [x] `/api/actor/followers`: contains the IDs of all followers (20 per page)
- [x] `/api/actor/following`: an empty list
- [x] `/api/submissions/{submitid}`: attempts cache refresh for the post, then returns the resulting `Note` object
- [x] `/api/journals/{journalid}`: attempts cache refresh for the journal, then returns the resulting `Article` object

Timed functions:

- [x] `ShortUpdate`: Attempt cache refresh for all posts (cached or on Weasyl) within the past 30 days, then send outbound activities (every five minutes)
- [x] `LongUpdate`: Attempt cache refresh for all posts (cached or on Weasyl) and the actor's name/avatar/etc (every day)
- [x] `OutboundActivityCleanup`: Remove any unsent `OutboundActivity` objects more than 7 days old (every hour)

Other functions:

- [x] Add Outbound Activities
    * Group followers by inbox
    * For each inbox, add the appropriate `OutboundActivity`
- [x] Process Outbound Activities
    * For each `OutboundActivity`:
        * Send the activity to the inbox
        * Mark as sent (no longer pending)
    * For each `PrivateAnnouncement`:
        * Send the activity to the inbox
        * Mark as sent (no longer pending)
    * If a send fails, don't try sending it again for 4 hours
- [x] Cache Refresh
    * If the post is stale:
        * Pull the post from Weasyl
        * Update or delete our copy
        * Add outbound activities
- [x] User Cache Refresh
    * If the user info is stale:
        * Pull the user from Weasyl
        * Update or delete our copy
        * Add outbound activities

Note that a cached submission will not be refreshed if:

* the last attempted cache refresh was within the past 4 minutes
* the last successful cache refresh was within the past hour, and the post is more than an hour old
* the last successful cache refresh was within the past 7 days, and the post is more than 7 days old
* the last successful cache refresh was within the past 28 days, and the post is more than 28 days old

A cached user (name, icon, etc.) will not be refreshed if:

* the last attempted cache refresh was within the past 4 minutes
* the last successful cache refresh was within the past hour

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
