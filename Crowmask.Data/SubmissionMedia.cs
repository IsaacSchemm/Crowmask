using System.ComponentModel.DataAnnotations;

namespace Crowmask.Data
{
    public class SubmissionMedia
    {
        public long Id { get; set; }

        [Required]
        public string Url { get; set; } = "";
    }
}
