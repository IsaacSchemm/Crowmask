using System.ComponentModel.DataAnnotations;

namespace Crowmask.Data
{
    public class SubmissionThumbnail
    {
        public Guid Id { get; set; }

        [Required]
        public string Url { get; set; } = "";

        [Required]
        public string ContentType { get; set; } = "";
    }
}
