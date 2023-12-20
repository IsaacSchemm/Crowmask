using Microsoft.EntityFrameworkCore;

#nullable disable

namespace Crowmask.Data
{
    public class CrowmaskDbContext(DbContextOptions<CrowmaskDbContext> options) : DbContext(options)
    {
        public DbSet<Follower> Followers { get; set; }

        public DbSet<OutboundActivity> OutboundActivities { get; set; }

        public DbSet<Submission> Submissions { get; set; }

        public DbSet<User> Users { get; set; }
    }
}
