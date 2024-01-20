using Crowmask.Data;
using Crowmask.Interfaces;

namespace Crowmask
{
    /// <summary>
    /// Provides a way to look up Crowmask posts, given the ID of a relevant
    /// ActivityPub object (a Like, an Announce, or a reply).
    /// This implementation pulls all posts in the database and looks through
    /// each of them.
    /// </summary>
    public class SlowInteractionLookup(CrowmaskDbContext context) : IInteractionLookup
    {
        public async Task<IEnumerable<int>> GetRelevantSubmitIdsAsync(string external_activity_or_object_id)
        {
            HashSet<int> set = [];

            await foreach (var submission in context.Submissions)
            {
                bool matching =
                    submission.Boosts.Any(x => x.ActivityId == external_activity_or_object_id)
                    || submission.Likes.Any(x => x.ActivityId == external_activity_or_object_id)
                    || submission.Replies.Any(x => x.ObjectId == external_activity_or_object_id);
                if (matching)
                    set.Add(submission.SubmitId);
            }

            return set;
        }
    }
}
