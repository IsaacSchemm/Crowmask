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
users on Mastodon, Pixelfed, and microblog.pub, among others. (Submissions are
exposed as `Note` objects - this was used instead of `Article` for a better
experience in Mastodon and to keep compatibility with Pixelfed.)

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
* The Crowmask outbox only contains the 20 most recent posts. (Note that
  Mastodon does not use the outbox.)

## Implementation details

Internal objects:

- [x] `User`: a cached user from Weasyl, along with information about when Crowmask last attempted to refresh it and when it was last refreshed
- [x] `UserLink`: an entry from the Contact and Social Media section of the Weasyl profile
- [x] `Submission`: a cached post from Weasyl, along with information about when Crowmask last attempted to refresh it and when it was last refreshed
- [x] `SubmissionMedia`: an associated image
- [x] `SubmissionTag`: an associated tag
- [x] `Follower`: an ActivityPub actor who follows this actor
- [x] `OutboundActivity`: a list of unsent `Accept`, `Announce`, `Undo`, `Create`, `Update`, and `Delete` activities sent to particular actors or instances

ActivityPub HTTP endpoints:

- [x] `/api/actor`: attempts cache refresh for the user, then returns the resulting object
- [x] `/api/actor/inbox`: processes the following activities:
    - [x] `Follow`: adds a new follower (or updates the `Follow` ID of an existing follower)
    - [x] `Undo` with `Follow`: removes the follower with the given `Follow` ID (if any)
    - [x] `Like`: if it matches a `Submission`, sends a transient message to the admin actor, with a link to the actor who sent the `Like`
    - [x] `Announce`: if it matches a `Submission`, sends a transient message to the admin actor, with a link to the actor who sent the `Announce`
    - [x] `Create`: if it's for a reply to a `Submission`, sends a transient message to the admin, with a link to the reply
- [x] `/api/actor/outbox`: contains links to the first and last gallery page
- [x] `/api/actor/outbox/page`: contains `Create` activities for known Weasyl posts (20 per page)
- [ ] `/api/actor/followers`: contains the IDs of all followers (20 per page)
- [ ] `/api/actor/following`: an empty list
- [x] `/api/submissions/{submitid}`: attempts cache refresh for the post, then returns the resulting `Note` object

Timed functions:

- [x] `ShortUpdate`: Attempt cache refresh for all posts (cached or on Weasyl) within the past 60 days, then send outbound activities (every five minutes)
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

* the last attempted cache refresh was within the past 5 minutes
* the last successful cache refresh was within the past hour, and the post is more than an hour old
* the last successful cache refresh was within the past 7 days, and the post is more than 7 days old
* the last successful cache refresh was within the past 28 days, and the post is more than 28 days old

A cached user (name, icon, etc.) will not be refreshed if:

* the last attempted cache refresh was within the past 5 minutes
* the last successful cache refresh was within the past hour

Other tasks:

- [x] Sign HTTP requets using Azure Key Vault
- [x] Create an application that can send Create, Update, and Delete activities and host Actor and Note URLs,
      using [ActivityPub Starter Kit](https://github.com/jakelazaroff/activitypub-starter-kit) as a guide,
      consumable by microblog.pub (posts should be visible in the Inbox tab)
- [x] Figure out local and Azure configuration storage for the database connection, the Weasyl API key, and the "admin actor URL"
- [x] Implement shared inbox support
- [x] Only insert JSON-LD @context at top level
- [x] Use an actual JSON-LD implementation for parsing
- [x] Allow the hostname to be configurable
- [x] Webfinger implementation
- [x] Make sure that a submission belongs to the logged-in user before adding and returning it
- [x] Dedupe follow requests by actor (only honor most recent Follow)
- [x] Forward unknown webfinger requests to the admin actor's server, if any
- [x] Make the domain in the handle configurable
- [x] Create a private post to the admin actor (if any) describing each incoming like, boost, or reply
- [x] Linkify the external links from Weasyl in the same way Weasyl does
- [x] Add handlers to GET endpoints for `text/html`
- [x] Experiment with other post types besides `Note`

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
