using Crowmask.Data;
using Crowmask.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Crowmask
{
    /// <summary>
    /// Provides a way to quickly look up Crowmask posts, given the ID of a
    /// relevant ActivityPub object (a Like, an Announce, or a reply). This
    /// implementation is specific to the Cosmos DB backend.
    /// </summary>
    /// <param name="context"></param>
    public class FastInteractionLookup(CrowmaskDbContext context) : IInteractionLookup
    {
        public async Task<IEnumerable<int>> GetRelevantSubmitIdsAsync(string external_activity_or_object_id)
        {
            HashSet<int> set = [];

            IReadOnlyList<IQueryable<Submission>> queries = [
                context.Submissions.FromSqlRaw(
                    $"SELECT VALUE c FROM c JOIN t IN c.{nameof(Submission.Boosts)} WHERE t.{nameof(Submission.SubmissionBoost.ActivityId)} = {{0}} AND c.id LIKE '{nameof(Submission)}|%'",
                    external_activity_or_object_id),

                context.Submissions.FromSqlRaw(
                    $"SELECT VALUE c FROM c JOIN t IN c.{nameof(Submission.Likes)} WHERE t.{nameof(Submission.SubmissionLike.ActivityId)} = {{0}} AND c.id LIKE '{nameof(Submission)}|%'",
                    external_activity_or_object_id),

                context.Submissions.FromSqlRaw(
                    $"SELECT VALUE c FROM c JOIN t IN c.{nameof(Submission.Replies)} WHERE t.{nameof(Submission.SubmissionReply.ObjectId)} = {{0}} AND c.id LIKE '{nameof(Submission)}|%'",
                    external_activity_or_object_id),
            ];

            foreach (var query in queries)
                await foreach (var item in query.AsAsyncEnumerable())
                    set.Add(item.SubmitId);

            return set;
        }
    }
}
