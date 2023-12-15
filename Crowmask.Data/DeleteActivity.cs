using System.ComponentModel.DataAnnotations;

namespace Crowmask.Data
{
    public class DeleteActivity
    {
        [Key]
        public Guid Id { get; set; }

        public int SubmitId { get; set; }

        public DateTimeOffset PublishedAt { get; set; }
    }
}
