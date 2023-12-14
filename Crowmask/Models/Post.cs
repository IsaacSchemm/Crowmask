using System;
using System.ComponentModel.DataAnnotations;

namespace Crowmask.Models
{
    public class Post
    {
        [Required]
        public string Id { get; set; }

        [Required]
        public string ContentsJson { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
    }
}
