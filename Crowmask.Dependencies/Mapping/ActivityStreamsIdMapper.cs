using Crowmask.DomainModeling;

namespace Crowmask.Dependencies.Mapping
{
    public class ActivityStreamsIdMapper(ICrowmaskHost crowmaskHost)
    {
        public string ActorId =>
            $"https://{crowmaskHost.Hostname}/api/actor";

        public string GetTransientId() =>
            $"{ActorId}#transient-{Guid.NewGuid()}";

        //public string GetObjectType(JointIdentifier identifier) =>
        //    identifier.IsSubmissionIdentifier ? "Note"
        //    : identifier.IsJournalIdentifier ? "Article"
        //    : throw new NotImplementedException();

        public string GetObjectId(JointIdentifier identifier) =>
            identifier.IsSubmissionIdentifier
                ? $"https://{crowmaskHost.Hostname}/api/submissions/{identifier.submitid}"
            : identifier.IsJournalIdentifier
                ? $"https://{crowmaskHost.Hostname}/api/journals/{identifier.journalid}"
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
