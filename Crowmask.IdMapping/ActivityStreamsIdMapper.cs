﻿using Crowmask.DomainModeling;

namespace Crowmask.IdMapping
{
    public class ActivityStreamsIdMapper(ICrowmaskHost crowmaskHost)
    {
        public string ActorId =>
            $"https://{crowmaskHost.Hostname}/api/actor";

        public string GetTransientId() =>
            $"{ActorId}#transient-{Guid.NewGuid()}";

        public string GetObjectType(JointIdentifier identifier) =>
            identifier is JointIdentifier.SubmissionIdentifier ? "Note"
            : identifier is JointIdentifier.JournalIdentifier ? "Article"
            : throw new NotImplementedException();

        public string GetObjectId(JointIdentifier identifier) =>
            identifier is JointIdentifier.SubmissionIdentifier s
                ? $"https://{crowmaskHost.Hostname}/api/submissions/{s.submitid}"
            : identifier is JointIdentifier.JournalIdentifier j
                ? $"https://{crowmaskHost.Hostname}/api/journals/{j.journalid}"
            : throw new NotImplementedException();

        public JointIdentifier? GetJointIdentifier(string objectId)
        {
            if (!Uri.TryCreate(objectId, UriKind.Absolute, out Uri? uri))
                return null;

            if (!int.TryParse(uri.AbsolutePath.Split('/').Last(), out int id))
                return null;

            foreach (var candidate in new[]
            {
                JointIdentifier.NewSubmissionIdentifier(id),
                JointIdentifier.NewJournalIdentifier(id)
            })
            {
                if (GetObjectId(candidate) == objectId)
                    return candidate;
            }

            return null;
        }

        public string GetObjectId(JointIdentifier identifier, Interaction interaction) =>
            $"{GetObjectId(identifier)}/interactions/{interaction.Id}/notification";
    }
}
