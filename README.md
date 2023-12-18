# Crowmask

**Crowmask** is a single-user ActivityPub bridge for Weasyl, implemented using Azure Functions.

Crowmask stands for "Content Read Off Weasyl: Modified ActivityPub Starter Kit". It began as an attempt
to port [ActivityPub Starter Kit](https://github.com/jakelazaroff/activitypub-starter-kit) to .NET, but
was quickly modified to support the strongly typed nature of .NET and the specificity of this app.
Still, having a simple,working ActivityPub implementation in a language I was able to understand (if
not compile) was incredibly helpful.

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

- [x] `/api/actor`: attempts cache refresh for the user, processes outbound activities, then returns the resulting object
- [ ] `/api/actor/inbox`: accepts `Follow`, `Undo` `Follow`, `Create`, and `Delete`
- [ ] `/api/actor/outbox`: contains a `Create` activity for each cached Weasyl post
- [ ] `/api/actor/followers`: contains a list of followers
- [ ] `/api/actor/following`: an empty list
- [ ] `/api/creates/{submitid}`: returns a `Create` activity for the post from the public outbox
- [ ] `/api/activities/{guid}`: returns the matching `OutboundActivity`
- [x] `/api/submissions/{submitid}`: Attempts cache refresh for the post, processes outbound activities, then returns the resulting object

Accepted inbox activities:

- [ ] `Follow`: adds the actor to the list of followers, adds an `Accept` to `OutboundActivity`, then processes outbound activities for this actor only
- [ ] `Undo` `Follow`: removes the actor from the list of followers
- [ ] `Create`: if the post is in reply to this actor's post, add a `PrivateAnnouncement` for the Admin Actor

Timed functions:

- [ ] `ActorUpdate`: Update the name, avatar, etc of the actor and add an `Update` to `OutboundActivity` if needed
- [ ] `GalleryUpdate`: Check the associated Weasyl account for new posts since the last `GalleryUpdate` and attempt cache refresh for each, then process outbound activities (every four hours)
- [ ] `OutboundActivityCleanup`: Remove each `OutboundActivity` that was successfully sent more than a week ago (every hour)

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
    * If a send fails, record the failure, and prioritize outgoing messages with fewer failures
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
- [x] Figure out local and Azure configuration storage for the SQL database connection, the Weasyl API key, and the "admin actor URL"
- [ ] Try out Entra auth for DB
- [ ] Verify HTTP signatures in inbox
- [x] Only insert JSON-LD @context at top level
- [x] Use an actual JSON-LD implementation for parsing
- [ ] Webfinger implementation
