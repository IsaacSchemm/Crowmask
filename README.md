# Crowmask

**Crowmask** stands for "Content Read Off Weasyl: Modified ActivityPub Starter Kit".

Internal objects:

- [x] `Submission`: a cached post from Weasyl, along with information about when Crowmask last attempted to refresh it and when it was last refreshed
- [x] `SubmissionMedia`: an associated image
- [x] `SubmissionTag`: an associated tag
- [ ] `Follower`: an ActivityPub actor who follows this actor
- [ ] `InstanceOutbox`: an internal outbox for another ActivityPub instance (to deal with instances that go down)
- [ ] `Reply`: a reply to this actor's post, with a private announcement sent to all Admin Actors

ActivityPub HTTP endpoints:

- [ ] `/api/actor`: a Person object with the name, avatar, and URL of the associated Weasyl account
- [ ] `/api/actor/inbox`: accepts `Follow`, `Undo` `Follow`, `Create`, and `Delete`
- [ ] `/api/actor/outbox`: contains a `Create` activity for each cached Weasyl post
- [ ] `/api/actor/followers`: contains a list of followers
- [ ] `/api/actor/following`: an empty list
- [ ] `/api/creates/{id}`: returns the matching `Create` activity (`Update` and `Delete` activites are transient)
- [ ] `/api/submissions/{submitid}`: Attempts cache refresh for the post, processes instance outboxes, then returns the resulting object

Accepted inbox activities:

- [ ] `Follow`: adds the actor to the list of followers
- [ ] `Undo` `Follow`: removes the actor from the list of followers
- [ ] `Create`: if the post is in reply to this actor's post, store a `Reply` and send an `Announce` activity to all Admin Actors
- [ ] `Delete`: if the post has an associated `Reply`, remove it and send an `Undo`

Timed functions:

- [ ] `GalleryUpdate`: Check the associated Weasyl account for new posts since the last `GalleryUpdate` and attempt cache refresh for each, then process instance outboxes (every hour)

Other functions:

- [ ] Process Instance Outboxes
    * For each instance outbox:
        * Send each pending `Create`, `Update`, or `Delete` (in order) to the instance
        * Mark as sent (no longer pending)
    * If a send fails, skip that instance and move to the next
- [ ] Cache Refresh
    * If the post is cached:
        * If the post is at least 24 hours old and the cache is less than an hour old, keep it
        * If a cache refresh was performed on this post within the last 5 minutes, keep it
    * Pull the post from Weasyl
    * If the fields we care about have changed, or if the post is deleted:
        * Update or delete our copy
        * Add a `Create`, `Update`, or `Delete` to instance outboxes for all instances with at least one follower

Other tasks:

- [x] Sign HTTP requets using Azure Key Vault
- [x] Create an application that can send Create, Update, and Delete activities and host Actor and Note URLs,
      using [ActivityPub Starter Kit](https://github.com/jakelazaroff/activitypub-starter-kit) as a guide,
      consumable by microblog.pub (posts should be visible in the Inbox tab)
- [ ] Figure out local and Azure configuration storage for the SQL database connection, the Weasyl API key, and the "admin actor URLs"
- [ ] Verify HTTP signatures in inbox
