using Crowmask.ActivityPub;
using Crowmask.Data;
using Crowmask.DomainModeling;
using Crowmask.Remote;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Crowmask
{
    public class DatabaseActions(CrowmaskDbContext context)
    {
        public async Task AddOutboundActivityAsync(IDictionary<string, object> obj, Requester.RemoteActor remoteActor)
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

        public async Task AddFollowAsync(string objectId, Requester.RemoteActor actor)
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

        public async Task RemoveFollowAsync(string objectId)
        {
            var followers = context.Followers
                .Where(i => i.MostRecentFollowId == objectId)
                .AsAsyncEnumerable();
            await foreach (var i in followers)
                context.Followers.Remove(i);

            await context.SaveChangesAsync();
        }

        public async Task AddLikeAsync(JointIdentifier identifier, string activityId, Requester.RemoteActor actor)
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

        public async Task AddBoostAsync(JointIdentifier identifier, string activityId, Requester.RemoteActor actor)
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

        public async Task AddReplyAsync(JointIdentifier identifier, string replyObjectId, Requester.RemoteActor actor)
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
