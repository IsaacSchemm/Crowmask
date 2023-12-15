# Crowmask

**Crowmask** stands for "Content Read Off Weasyl: Modified ActivityPub Starter Kit".

The plan:

- [x] Sign HTTP requets using Azure Key Vault
- [x] Create an application that can send Create, Update, and Delete activities and host Actor and Note URLs,
      using [ActivityPub Starter Kit](https://github.com/jakelazaroff/activitypub-starter-kit) as a guide,
      consumable by microblog.pub (posts should be visible in the Inbox tab)
- [ ] Figure out local and Azure configuration storage for the SQL database connection, the Weasyl API key, and the "admin actor URLs"
- [ ] Accept follow (ID or object)
- [ ] Accept undo follow (ID or object)
- [ ] Implement inbox, outbox, followers, following
- [ ] Implement an Entity Framework Core backend to store Activities and Notes
- [ ] Send new public posts (from the user who issued the API key) to all followers
- [ ] When a reply is recieved, boost it, but make that boost only visible to the "admin actor URLs"
- [ ] Pull new posts from the Weasyl API periodically
- [ ] Periodically check whether stored posts still exist on Weasyl, and update or delete them as appropriate
- [ ] Add public endpoints for a "home page" and for individual Weasyl posts
