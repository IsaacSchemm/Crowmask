# Crowmask 🐦‍⬛🎭

**Crowmask** is a single-user ActivityPub bridge for Weasyl, implemented using Azure Functions.

Internal objects:

- [x] `User`: a cached user from Weasyl, along with information about when Crowmask last attempted to refresh it and when it was last refreshed
- [x] `UserLink`: an entry from the Contact and Social Media section of the Weasyl profile
- [x] `Submission`: a cached post from Weasyl, along with information about when Crowmask last attempted to refresh it and when it was last refreshed
- [x] `SubmissionMedia`: an associated image
- [x] `SubmissionTag`: an associated tag
- [x] `Follower`: an ActivityPub actor who follows this actor
- [x] `PrivateAnnouncement`: a list of `Announce` activities sent to particular actors or instances
- [x] `OutboundActivity`: a list of `Accept`, `Create`, `Update`, and `Delete` activities sent to particular actors or instances

ActivityPub HTTP endpoints:

- [x] `/api/actor`: attempts cache refresh for the user, then returns the resulting object
- [x] `/api/actor/inbox`: accepts `Follow`, `Undo` `Follow`, and `Create`
- [ ] `/api/actor/outbox`: contains a `Create` activity for each cached Weasyl post
- [ ] `/api/actor/followers`: contains a list of followers
- [ ] `/api/actor/following`: an empty list
- [ ] `/api/creates/{submitid}`: returns a `Create` activity for the post from the public outbox
- [ ] `/api/activities/{guid}`: returns the matching `OutboundActivity`
- [x] `/api/submissions/{submitid}`: Attempts cache refresh for the post, then returns the resulting object

Accepted inbox activities:

- [x] `Follow`: adds the actor to the list of followers and adds an `Accept` to `OutboundActivity`
- [x] `Undo` `Follow`: removes the actor from the list of followers
- [ ] `Create`: if the post is in reply to this actor's post, add a `PrivateAnnouncement` for the Admin Actor

Timed functions:

- [ ] `ActorUpdate`: Update the name, avatar, etc of the actor and add an `Update` to `OutboundActivity` if needed (every day)
- [ ] `GalleryUpdate`: Check the associated Weasyl account for new posts since the last `GalleryUpdate` and attempt cache refresh for each (every hour)
- [x] `OutboundActivitySend`: Try to send outbound activities (every five minutes)
- [ ] `OutboundActivityCleanup`: Remove any `OutboundActivity` more than a week old, regardless of whether it was sent or not (every day)

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
- [ ] Verify incoming replies by ensuring they are real and public
- [x] Only insert JSON-LD @context at top level
- [x] Use an actual JSON-LD implementation for parsing
- [ ] Webfinger implementation
- [ ] Make sure that a submission belongs to the logged-in user before adding and returning it
- [ ] Allow certain endpoints to accept HTML instead of just redirecting to Weasyl? (I'll think about it)
- [x] Dedupe follow requests?
- [ ] Periodically check actors to make sure they are still following?

Potential future improvements:

- [ ] Implement HTTP signature validation for incoming requests
- [ ] Entra / RBAC auth for Cosmos DB (requires EF Core 7+)

Crowmask stands for "Content Read Off Weasyl: Modified ActivityPub Starter Kit". It began as an attempt
to port [ActivityPub Starter Kit](https://github.com/jakelazaroff/activitypub-starter-kit) to .NET, but
was quickly modified to support the strongly typed nature of .NET and the specificity of this app.
Still, having a simple, working ActivityPub implementation in a language I was able to understand (if
not compile) was incredibly helpful.

See also [bird.makeup](https://sr.ht/~cloutier/bird.makeup/), which is another
ActivityPub bridge written in C# / .NET.
