using System;
using System.ComponentModel.DataAnnotations;

namespace Crowmask.Models
{
    public class Following
    {
        [Required]
        public string Id { get; set; }

        [Required]
        public string Actor { get; set; }

        [Required]
        public string Uri { get; set; }

        public bool Confirmed { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }
}
