# Crowmask

**Crowmask** stands for "Content Read Off Weasyl: Modified ActivityPub Starter Kit".

The plan:

- [ ] Create a version of [ActivityPub Starter Kit](https://github.com/jakelazaroff/activitypub-starter-kit) that's written in C# or F# and runs on ASP.NET Core or Azure Functions with an Entity Framework Core backend
- [ ] Pull recent posts from the Weasyl API periodically
- [ ] Handle incoming replies by private-boosting them just to certain actors (in practice, to my own separate microblog)
- [ ] Add public endpoints for a "home page" and for individual Weasyl posts
- [ ] Periodically check whether stored posts still exist on Weasyl, and hide or delete them if they do not

Only posts by a single user (the user who issued the Weasyl API key) will be read from, and that user's name and avatar will be used for the ActivityPub actor.
