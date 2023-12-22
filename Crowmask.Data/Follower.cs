using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace Crowmask.Data
{
    public class Follower
    {
        public Guid Id { get; set; }

        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; }

        public string ActorId { get; set; }

        [Required]
        public string MostRecentFollowId { get; set; }

        [Required]
        public string Inbox { get; set; }

        public string SharedInbox { get; set; }
    }
}
