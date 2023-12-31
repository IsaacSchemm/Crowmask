﻿using System.ComponentModel.DataAnnotations;

#nullable disable

namespace Crowmask.Data
{
    public class OutboundActivity
    {
        public Guid Id { get; set; }

        [Required]
        public string Inbox { get; set; }

        [Required]
        public string JsonBody { get; set; }

        public DateTimeOffset StoredAt { get; set; }

        public DateTimeOffset DelayUntil { get; set; }
    }
}
