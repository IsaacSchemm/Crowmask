using System.ComponentModel.DataAnnotations;

namespace Crowmask.Data
{
    public class SubmissionMedia
    {
        public Guid Id { get; set; }

        [Required]
        public string Url { get; set; } = "";
    }
}
