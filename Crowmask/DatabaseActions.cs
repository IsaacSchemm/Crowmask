using Crowmask.Data;
using Crowmask.DomainModeling;
using Crowmask.Formats.ActivityPub;
using Crowmask.Library.Remote;
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
    public class DatabaseActions(CrowmaskDbContext context)
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

        /// <summary>
        /// Adds a new outbound activity to the Crowmask database, addressed
        /// to a single actor, to be sent by the RefreshUpstream function.
        /// </summary>
        /// <param name="obj">The object (from the Translator module) to serialize as JSON-LD</param>
        /// <param name="remoteActor">The actor to send the object to</param>
        /// <returns></returns>
        public async Task AddOutboundActivityAsync(IDictionary<string, object> obj, RemoteActor remoteActor)
        {
            context.OutboundActivities.Add(new OutboundActivity
            {
                Id = Guid.NewGuid(),
                Inbox = remoteActor.Inbox,
                JsonBody = AP.SerializeWithContext(obj),
                StoredAt = DateTimeOffset.UtcNow
            });

            await context.SaveChangesAsync();
        }

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
        /// <param name="identifier">The submission or journal ID</param>
        /// <param name="activityId">The Like activity ID, so Undo requests can be honored</param>
        /// <param name="actor">The actor who liked the post</param>
        /// <returns></returns>
        public async Task AddLikeAsync(JointIdentifier identifier, string activityId, RemoteActor actor)
        {
            if (identifier.IsSubmissionIdentifier)
            {
                var submission = await context.Submissions.FindAsync(identifier.submitid);
                submission.Likes.Add(new SubmissionLike
                {
                    Id = Guid.NewGuid(),
                    AddedAt = DateTimeOffset.UtcNow,
                    ActivityId = activityId,
                    ActorId = actor.Id,
                });
            }

            if (identifier.IsJournalIdentifier)
            {
                var journal = await context.Journals.FindAsync(identifier.journalid);
                journal.Likes.Add(new JournalLike
                {
                    Id = Guid.NewGuid(),
                    AddedAt = DateTimeOffset.UtcNow,
                    ActivityId = activityId,
                    ActorId = actor.Id,
                });
            }

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Adds a boost to a Crowmask post.
        /// </summary>
        /// <param name="identifier">The submission or journal ID</param>
        /// <param name="activityId">The Announce activity ID, so Undo requests can be honored</param>
        /// <param name="actor">The actor who boosted the post</param>
        public async Task AddBoostAsync(JointIdentifier identifier, string activityId, RemoteActor actor)
        {
            if (identifier.IsSubmissionIdentifier)
            {
                var submission = await context.Submissions.FindAsync(identifier.submitid);
                submission.Boosts.Add(new SubmissionBoost
                {
                    Id = Guid.NewGuid(),
                    AddedAt = DateTimeOffset.UtcNow,
                    ActivityId = activityId,
                    ActorId = actor.Id,
                });
            }

            if (identifier.IsJournalIdentifier)
            {
                var journal = await context.Journals.FindAsync(identifier.journalid);
                journal.Boosts.Add(new JournalBoost
                {
                    Id = Guid.NewGuid(),
                    AddedAt = DateTimeOffset.UtcNow,
                    ActivityId = activityId,
                    ActorId = actor.Id,
                });
            }

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Adds a reply to a Crowmask post.
        /// </summary>
        /// <param name="identifier">The submission or journal ID</param>
        /// <param name="activityId">The ID of the reply object (Note, etc.), so Delete requests can be honored</param>
        /// <param name="actor">The actor who replied to the post</param>
        public async Task AddReplyAsync(JointIdentifier identifier, string replyObjectId, RemoteActor actor)
        {
            if (identifier.IsSubmissionIdentifier)
            {
                var submission = await context.Submissions.FindAsync(identifier.submitid);
                submission.Replies.Add(new SubmissionReply
                {
                    Id = Guid.NewGuid(),
                    AddedAt = DateTimeOffset.UtcNow,
                    ObjectId = replyObjectId,
                    ActorId = actor.Id,
                });
            }

            if (identifier.IsJournalIdentifier)
            {
                var journal = await context.Journals.FindAsync(identifier.journalid);
                journal.Replies.Add(new JournalReply
                {
                    Id = Guid.NewGuid(),
                    AddedAt = DateTimeOffset.UtcNow,
                    ObjectId = replyObjectId,
                    ActorId = actor.Id,
                });
            }

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Removes a like, boost, or reply from a Crowmask post.
        /// </summary>
        /// <param name="identifier">The submission or journal ID</param>
        /// <param name="id">The GUID associated with the interaction in Crowmask's database</param>
        /// <returns></returns>
        public async Task RemoveInteractionAsync(JointIdentifier identifier, Guid id)
        {
            if (identifier.IsSubmissionIdentifier)
            {
                var submission = await context.Submissions.FindAsync(identifier.submitid);
                foreach (var boost in submission.Boosts.ToList())
                    if (boost.Id == id)
                        submission.Boosts.Remove(boost);
                foreach (var like in submission.Likes.ToList())
                    if (like.Id == id)
                        submission.Likes.Remove(like);
                foreach (var reply in submission.Replies.ToList())
                    if (reply.Id == id)
                        submission.Replies.Remove(reply);
            }
            else if (identifier.IsJournalIdentifier)
            {
                var journal = await context.Journals.FindAsync(identifier.journalid);
                foreach (var boost in journal.Boosts.ToList())
                    if (boost.Id == id)
                        journal.Boosts.Remove(boost);
                foreach (var like in journal.Likes.ToList())
                    if (like.Id == id)
                        journal.Likes.Remove(like);
                foreach (var reply in journal.Replies.ToList())
                    if (reply.Id == id)
                        journal.Replies.Remove(reply);
            }

            await context.SaveChangesAsync();
        }
    }
}
