using System.ComponentModel.DataAnnotations;

namespace Crowmask.Data
{
    public class SubmissionTag
    {
        public Guid Id { get; set; }

        [Required]
        public string Tag { get; set; } = "";
    }
}
