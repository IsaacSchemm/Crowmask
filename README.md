# Crowmask 🐦‍⬛🎭

**Crowmask** is a single-user ActivityPub bridge for Weasyl, implemented using Azure Functions and Azure Cosmos DB.

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
- [x] `/api/actor/outbox`: contains `Create` activities for the 20 most recent cached Weasyl posts
- [x] `/api/submissions/{submitid}`: attempts cache refresh for the post, then returns the resulting `Note` object
- [x] `/api/creations/{submitid}`: returns the `Create` activity included in the outbox (thus ensuring that these `Create` IDs map to valid URLs)

Timed functions:

- [x] `ShortUpdate`: Attempt cache refresh for all posts (cached or on Weasyl) within the past 60 days, then send outbound activities (every five minutes)
- [x] `LongUpdate`: Attempt cache refresh for all posts (cached or on Weasyl) and the actor's name/avatar/etc (every day)
- [x] `OutboundActivityCleanup`: Remove any unsent `OutboundActivity` more than 24 hours old (every hour)

Note that a cached submission will not be refreshed if:

* the last attempted cache refresh was within the past 5 minutes
* the last successful cache refresh was within the past hour, and the post is more than an hour old
* the last successful cache refresh was within the past 7 days, and the post is more than 7 days old
* the last successful cache refresh was within the past 28 days, and the post is more than 28 days old

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
    * If the post is cached:
        * If the post is at least 24 hours old and the cache is less than an hour old, keep it
        * If a cache refresh was performed on this post within the last 5 minutes, keep it
    * Pull the post from Weasyl
    * Update or delete our copy
    * Add outbound activities
- [x] User Cache Refresh
    * If the post is cached:
        * If the post is at least 24 hours old and the cache is less than an hour old, keep it
        * If a cache refresh was performed on this post within the last 5 minutes, keep it
    * Pull the user from Weasyl
    * Update or delete our copy
    * Add outbound activities

Submissions/posts that are less than 24 hours old will show a creation date of
when they were cached by Crowmask, not when they were created.

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
- [ ] Throw an error if the user ID changes
- [ ] Make sure that a submission belongs to the logged-in user before adding and returning it
- [x] Dedupe follow requests by actor (only honor most recent Follow)
- [ ] Forward unknown webfinger requests to the admin actor's server, if any
- [ ] Make the domain in the handle configurable
- [x] Create a private post to the admin actor (if any) describing each incoming like, boost, or reply
- [ ] Linkify the external links from Weasyl in the same way Weasyl does
- [ ] Add HTML redirects
- [ ] Experiment with other post types besides `Note`

Potential future improvements:

- [ ] Add HTML endpoints
- [ ] Implement HTTP signature validation for incoming requests
- [ ] Entra / RBAC auth for Cosmos DB (requires EF Core 7+)
- [ ] Outbox paging

Crowmask stands for "Content Read Off Weasyl: Modified ActivityPub Starter Kit". It began as an attempt
to port [ActivityPub Starter Kit](https://github.com/jakelazaroff/activitypub-starter-kit) to .NET, but
was quickly modified to support the strongly typed nature of .NET and the specificity of this app.
Still, having a simple, working ActivityPub implementation in a language I was able to understand (if
not compile) was incredibly helpful.

See also [bird.makeup](https://sr.ht/~cloutier/bird.makeup/), which is another
ActivityPub bridge written in C# / .NET.

Example `local.settings.json` (default settings omitted):

    {
      "Values": {
        "AdminActor": "https://pixelfed.example.com/users/...",
        "CosmosDBConnectionString": "AccountEndpoint=...;AccountKey=...;",
        "KeyVaultHost": "crowmask.vault.azure.net",
        "CrowmaskHost": "crowmask.example.com",
        "WeasylApiKey": "..."
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

The app should work with an SQL Server backend instead if you want to go that route.
In either case, it would be nice if this app used Managed Identity for that too, but
I think that would need .NET 7+ and I'm not sure if Azure Functions is quite set up
for that yet.
