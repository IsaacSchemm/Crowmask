using Crowmask.Data;
using Crowmask.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Crowmask
{
    /// <summary>
    /// Provides a way to quickly look up a Crowmask post, given the ID of a
    /// relevant ActivityPub object (a Like, an Announce, or a reply). This
    /// implementation is specific to the Cosmos DB backend and would need to
    /// be modified (or replaced with a much less efficient implementation
    /// that loops through all posts) if an SQL backend were used.
    /// </summary>
    /// <param name="context"></param>
    public class FastInteractionLookup(CrowmaskDbContext context) : IInteractionLookup
    {
        public async Task<int?> GetRelevantSubmitIdAsync(string external_activity_or_object_id)
        {
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
                    return item.SubmitId;

            return null;
        }
    }
}
