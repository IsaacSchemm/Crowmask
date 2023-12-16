using Microsoft.EntityFrameworkCore;

#nullable disable

namespace Crowmask.Data
{
    public class CrowmaskDbContext(DbContextOptions<CrowmaskDbContext> options) : DbContext(options)
    {
        public DbSet<Submission> Submissions { get; set; }
    }
}
