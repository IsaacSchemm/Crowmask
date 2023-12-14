using System;
using System.ComponentModel.DataAnnotations;

namespace Crowmask.Models
{
    public class Follower
    {
        [Required]
        public string Id { get; set; }

        [Required]
        public string Actor { get; set; }

        [Required]
        public string Uri { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }
}
