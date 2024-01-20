﻿using Crowmask.Data;
using Crowmask.Formats;
using Crowmask.Library;
using Crowmask.Library.Remote;
using Crowmask.LowLevel;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Crowmask
{
    /// <summary>
    /// Provides a way for the inbox handler to make changes in Crowmask's
    /// database, outside of what CrowmaskCache can do.
    /// </summary>
    public class InboxHandler(ActivityPubTranslator translator, CrowmaskDbContext context, RemoteInboxLocator locator)
    {
        /// <summary>
        /// Adds an actor's shared inbox (or personal inbox, if there is none
        /// to Crowmask's list of known inboxes, unless it is already present.
        /// </summary>
        /// <remarks>
        /// Inboxes that no longer exist will be removed by OutboundActivityCleanup.
        /// </remarks>
        /// <param name="actor">The ActivityPub actor to add</param>
        public async Task AddKnownInboxAsync(RemoteActor actor)
        {
            string personalInbox = actor.Inbox;
            string primaryInbox = actor.SharedInbox ?? actor.Inbox;

            var known = await context.KnownInboxes
                .Where(i => i.Inbox == personalInbox || i.Inbox == primaryInbox)
                .Take(1)
                .ToListAsync();

            if (known.Count == 0)
            {
                context.KnownInboxes.Add(new KnownInbox
                {
                    Id = Guid.NewGuid(),
                    Inbox = primaryInbox
                });
                await context.SaveChangesAsync();
            }
        }

        ///// <summary>
        ///// Adds a new outbound activity to the Crowmask database, addressed
        ///// to a single actor, to be sent by the RefreshUpstream function.
        ///// </summary>
        ///// <param name="obj">The object (from the Translator module) to serialize as JSON-LD</param>
        ///// <param name="remoteActor">The actor to send the object to</param>
        ///// <returns></returns>
        //public async Task AddOutboundActivityAsync(IDictionary<string, object> obj, RemoteActor remoteActor)
        //{
        //    context.OutboundActivities.Add(new OutboundActivity
        //    {
        //        Id = Guid.NewGuid(),
        //        Inbox = remoteActor.Inbox,
        //        JsonBody = ActivityPubSerializer.SerializeWithContext(obj),
        //        StoredAt = DateTimeOffset.UtcNow
        //    });

        //    await context.SaveChangesAsync();
        //}

        /// <summary>
        /// Adds a follower to the database. If the follower already exists,
        /// the ID of the Follow activity will be updated.
        /// </summary>
        /// <param name="objectId">The ID of the Follow activity, so Undo requests can be honored</param>
        /// <param name="actor">The follower to add</param>
        /// <returns></returns>
        public async Task AddFollowAsync(string objectId, RemoteActor actor)
        {
            var existing = await context.Followers
                .Where(f => f.ActorId == actor.Id)
                .SingleOrDefaultAsync();

            if (existing != null)
            {
                existing.MostRecentFollowId = objectId;
            }
            else
            {
                context.Followers.Add(new Follower
                {
                    Id = Guid.NewGuid(),
                    ActorId = actor.Id,
                    MostRecentFollowId = objectId,
                    Inbox = actor.Inbox,
                    SharedInbox = actor.SharedInbox
                });

                context.OutboundActivities.Add(new OutboundActivity
                {
                    Id = Guid.NewGuid(),
                    Inbox = actor.Inbox,
                    JsonBody = ActivityPubSerializer.SerializeWithContext(
                        translator.AcceptFollow(objectId)),
                    StoredAt = DateTimeOffset.UtcNow
                });
            }

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Remove a follower.
        /// </summary>
        /// <param name="objectId">The ID of the Follow activity</param>
        /// <returns></returns>
        public async Task RemoveFollowAsync(string objectId)
        {
            var followers = context.Followers
                .Where(i => i.MostRecentFollowId == objectId)
                .AsAsyncEnumerable();
            await foreach (var i in followers)
                context.Followers.Remove(i);

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Adds a like to a Crowmask post.
        /// </summary>
        /// <param name="submitid">The submission ID</param>
        /// <param name="activityId">The Like activity ID, so Undo requests can be honored</param>
        /// <param name="actor">The actor who liked the post</param>
        /// <returns></returns>
        public async Task AddLikeAsync(int submitid, string activityId, RemoteActor actor)
        {
            Guid newGuid = Guid.NewGuid();

            var submission = await context.Submissions.FindAsync(submitid);
            submission.Likes.Add(new Submission.SubmissionLike
            {
                Id = newGuid,
                AddedAt = DateTimeOffset.UtcNow,
                ActivityId = activityId,
                ActorId = actor.Id,
            });

            var newPost = Domain.AsPost(submission);

            foreach (var interaction in newPost.Interactions)
            {
                if (interaction.Id != newGuid)
                    continue;

                context.OutboundActivities.Add(new OutboundActivity
                {
                    Id = Guid.NewGuid(),
                    Inbox = await locator.GetAdminActorInboxAsync(),
                    JsonBody = ActivityPubSerializer.SerializeWithContext(
                        translator.PrivateNoteToCreate(
                            newPost,
                            interaction)),
                    StoredAt = DateTimeOffset.UtcNow
                });
            }

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Adds a boost to a Crowmask post.
        /// </summary>
        /// <param name="submitid">The submission ID</param>
        /// <param name="activityId">The Announce activity ID, so Undo requests can be honored</param>
        /// <param name="actor">The actor who boosted the post</param>
        public async Task AddBoostAsync(int submitid, string activityId, RemoteActor actor)
        {
            Guid newGuid = Guid.NewGuid();

            var submission = await context.Submissions.FindAsync(submitid);
            submission.Boosts.Add(new Submission.SubmissionBoost
            {
                Id = newGuid,
                AddedAt = DateTimeOffset.UtcNow,
                ActivityId = activityId,
                ActorId = actor.Id,
            });

            var newPost = Domain.AsPost(submission);

            foreach (var interaction in newPost.Interactions)
            {
                if (interaction.Id != newGuid)
                    continue;

                context.OutboundActivities.Add(new OutboundActivity
                {
                    Id = Guid.NewGuid(),
                    Inbox = await locator.GetAdminActorInboxAsync(),
                    JsonBody = ActivityPubSerializer.SerializeWithContext(
                        translator.PrivateNoteToCreate(
                            newPost,
                            interaction)),
                    StoredAt = DateTimeOffset.UtcNow
                });
            }

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Adds a reply to a Crowmask post.
        /// </summary>
        /// <param name="submitid">The submission ID</param>
        /// <param name="activityId">The ID of the reply object (Note, etc.), so Delete requests can be honored</param>
        /// <param name="actor">The actor who replied to the post</param>
        public async Task AddReplyAsync(int submitid, string replyObjectId, RemoteActor actor)
        {
            Guid newGuid = Guid.NewGuid();

            var submission = await context.Submissions.FindAsync(submitid);
            submission.Replies.Add(new Submission.SubmissionReply
            {
                Id = newGuid,
                AddedAt = DateTimeOffset.UtcNow,
                ObjectId = replyObjectId,
                ActorId = actor.Id,
            });

            var newPost = Domain.AsPost(submission);

            foreach (var interaction in newPost.Interactions)
            {
                if (interaction.Id != newGuid)
                    continue;

                context.OutboundActivities.Add(new OutboundActivity
                {
                    Id = Guid.NewGuid(),
                    Inbox = await locator.GetAdminActorInboxAsync(),
                    JsonBody = ActivityPubSerializer.SerializeWithContext(
                        translator.PrivateNoteToCreate(
                            newPost,
                            interaction)),
                    StoredAt = DateTimeOffset.UtcNow
                });
            }

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Removes a like, boost, or reply from a Crowmask post.
        /// </summary>
        /// <param name="submitid">The submission ID</param>
        /// <param name="id">The GUID associated with the interaction in Crowmask's database</param>
        /// <returns></returns>
        public async Task RemoveInteractionAsync(int submitid, Guid id)
        {
            var submission = await context.Submissions.FindAsync(submitid);
            var oldPost = Domain.AsPost(submission);

            foreach (var boost in submission.Boosts.ToList())
                if (boost.Id == id)
                    submission.Boosts.Remove(boost);
            foreach (var like in submission.Likes.ToList())
                if (like.Id == id)
                    submission.Likes.Remove(like);
            foreach (var reply in submission.Replies.ToList())
                if (reply.Id == id)
                    submission.Replies.Remove(reply);

            foreach (var interaction in oldPost.Interactions)
            {
                if (interaction.Id != id)
                    continue;

                context.OutboundActivities.Add(new OutboundActivity
                {
                    Id = Guid.NewGuid(),
                    Inbox = await locator.GetAdminActorInboxAsync(),
                    JsonBody = ActivityPubSerializer.SerializeWithContext(
                        translator.PrivateNoteToDelete(
                            oldPost,
                            interaction)),
                    StoredAt = DateTimeOffset.UtcNow
                });
            }

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Adds a remote mention to Crowmask, if it does not already exist.
        /// </summary>
        /// <param name="objectIds">The ID of the object (Note, etc.), so Delete requests can be honored</param>
        /// <param name="actor">The actor who created the mention</param>
        public async Task AddMentionAsync(string objectId, RemoteActor actor)
        {
            var existingMention = await context.Mentions
                .Where(m => m.ObjectId == objectId)
                .Where(m => m.ActorId == actor.Id)
                .FirstOrDefaultAsync();
            if (existingMention != null)
                return;

            var newMention = new Mention
            {
                Id = Guid.NewGuid(),
                AddedAt = DateTimeOffset.UtcNow,
                ObjectId = objectId,
                ActorId = actor.Id,
            };

            context.Mentions.Add(newMention);

            var remotePost = Domain.AsRemotePost(newMention);

            context.OutboundActivities.Add(new OutboundActivity
            {
                Id = Guid.NewGuid(),
                Inbox = await locator.GetAdminActorInboxAsync(),
                JsonBody = ActivityPubSerializer.SerializeWithContext(
                    translator.PrivateNoteToCreate(remotePost)),
                StoredAt = DateTimeOffset.UtcNow
            });

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Removes remote mentions from Crowmask.
        /// </summary>
        /// <param name="objectIds">The ID(s) of the object(s) (Note, etc.)</param>
        /// <param name="actor">The actor who created the mention</param>
        public async Task RemoveMentionsAsync(IReadOnlyList<string> objectIds, RemoteActor actor)
        {
            var existingMentions = await context.Mentions
                .Where(m => objectIds.Contains(m.ObjectId))
                .Where(m => m.ActorId == actor.Id)
                .ToListAsync();

            foreach (var existingMention in existingMentions)
            {
                var remotePost = Domain.AsRemotePost(existingMention);

                context.OutboundActivities.Add(new OutboundActivity
                {
                    Id = Guid.NewGuid(),
                    Inbox = await locator.GetAdminActorInboxAsync(),
                    JsonBody = ActivityPubSerializer.SerializeWithContext(
                        translator.PrivateNoteToDelete(remotePost)),
                    StoredAt = DateTimeOffset.UtcNow
                });

                context.Mentions.Remove(existingMention);
            }

            await context.SaveChangesAsync();
        }
    }
}
