using Crowmask.Models;
using Microsoft.EntityFrameworkCore;

namespace Crowmask
{
    public class CrowmaskDbContext : DbContext
    {
        public CrowmaskDbContext(DbContextOptions<CrowmaskDbContext> options) : base(options) { }

        public DbSet<Follower> Followers { get; set; }
        public DbSet<Following> Followings { get; set; }
        public DbSet<Post> Posts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Follower>().HasIndex(x => new { x.Actor });
            modelBuilder.Entity<Follower>().HasIndex(x => new { x.Uri });

            modelBuilder.Entity<Following>().HasIndex(x => new { x.Actor });
            modelBuilder.Entity<Following>().HasIndex(x => new { x.Uri });
        }
    }
}
