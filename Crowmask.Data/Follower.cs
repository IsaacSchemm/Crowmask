using System.ComponentModel.DataAnnotations;

#nullable disable

namespace Crowmask.Data
{
    public class Follower
    {
        public Guid Id { get; set; }

        public string ActorId { get; set; }

        public string FollowId { get; set; }

        [Required]
        public string Inbox { get; set; }

        public string SharedInbox { get; set; }
    }
}
