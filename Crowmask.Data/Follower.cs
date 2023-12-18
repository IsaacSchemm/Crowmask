using System.ComponentModel.DataAnnotations;

#nullable disable

namespace Crowmask.Data
{
    public class Follower
    {
        public long Id { get; set; }

        public string FollowActivityId { get; set; }

        [Required]
        public string Inbox { get; set; }

        public string SharedInbox { get; set; }
    }
}
