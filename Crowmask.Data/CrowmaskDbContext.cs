using Microsoft.EntityFrameworkCore;

#nullable disable

namespace Crowmask.Data
{
    public class CrowmaskDbContext(DbContextOptions<CrowmaskDbContext> options) : DbContext(options)
    {
        public DbSet<UpdateActivity> UpdateActivities { get; set; }

        public DbSet<DeleteActivity> DeleteActivities { get; set; }

        public DbSet<Submission> Submissions { get; set; }
    }
}
