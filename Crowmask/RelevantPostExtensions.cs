﻿using Crowmask.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace Crowmask
{
    public static class RelevantPostExtensions
    {
        public static async IAsyncEnumerable<Submission> GetRelevantSubmissionsAsync(this CrowmaskDbContext context, string activity_or_reply_id)
        {
            IReadOnlyList<IQueryable<Submission>> queries = [
                context.Submissions.FromSqlRaw(
                    $"SELECT VALUE c FROM c JOIN t IN c.{nameof(Submission.Boosts)} WHERE t.{nameof(SubmissionBoost.ActivityId)} = {{0}} AND c.id LIKE '{nameof(Submission)}|%'",
                    activity_or_reply_id),

                context.Submissions.FromSqlRaw(
                    $"SELECT VALUE c FROM c JOIN t IN c.{nameof(Submission.Likes)} WHERE t.{nameof(SubmissionLike.ActivityId)} = {{0}} AND c.id LIKE '{nameof(Submission)}|%'",
                    activity_or_reply_id),

                context.Submissions.FromSqlRaw(
                    $"SELECT VALUE c FROM c JOIN t IN c.{nameof(Submission.Replies)} WHERE t.{nameof(SubmissionReply.ObjectId)} = {{0}} AND c.id LIKE '{nameof(Submission)}|%'",
                    activity_or_reply_id),
            ];

            foreach (var query in queries)
                await foreach (var item in query.AsAsyncEnumerable())
                    yield return item;
        }

        public static async IAsyncEnumerable<Journal> GetRelevantJournalsAsync(this CrowmaskDbContext context, string activity_or_reply_id)
        {
            IReadOnlyList<IQueryable<Journal>> queries = [
                context.Journals.FromSqlRaw(
                    $"SELECT VALUE c FROM c JOIN t IN c.{nameof(Journal.Boosts)} WHERE t.{nameof(JournalBoost.ActivityId)} = {{0}} AND c.id LIKE '{nameof(Journal)}|%'",
                    activity_or_reply_id),

                context.Journals.FromSqlRaw(
                    $"SELECT VALUE c FROM c JOIN t IN c.{nameof(Journal.Likes)} WHERE t.{nameof(JournalLike.ActivityId)} = {{0}} AND c.id LIKE '{nameof(Journal)}|%'",
                    activity_or_reply_id),

                context.Journals.FromSqlRaw(
                    $"SELECT VALUE c FROM c JOIN t IN c.{nameof(Journal.Replies)} WHERE t.{nameof(JournalReply.ObjectId)} = {{0}} AND c.id LIKE '{nameof(Journal)}|%'",
                    activity_or_reply_id),
            ];

            foreach (var query in queries)
                await foreach (var item in query.AsAsyncEnumerable())
                    yield return item;
        }
    }
}
