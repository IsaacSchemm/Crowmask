using Microsoft.EntityFrameworkCore;

#nullable disable

namespace Crowmask.Data
{
    public class CrowmaskDbContext(DbContextOptions<CrowmaskDbContext> options) : DbContext(options)
    {
        public DbSet<BlueskySession> BlueskySessions { get; set; }

        public DbSet<Follower> Followers { get; set; }

        public DbSet<Interaction> Interactions { get; set; }

        public DbSet<Journal> Journals { get; set; }

        public DbSet<KnownInbox> KnownInboxes { get; set; }

        public DbSet<Mention> Mentions { get; set; }

        public DbSet<OutboundActivity> OutboundActivities { get; set; }

        public DbSet<Submission> Submissions { get; set; }

        internal DbSet<User> Users { get; set; }

        private const int INTERNAL_USER_ID = 0;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultContainer(nameof(CrowmaskDbContext));
            base.OnModelCreating(modelBuilder);
        }

        public async Task<User> GetUserAsync()
        {
            var user = await Users
                .Include(u => u.Avatars)
                .Include(u => u.Links)
                .Where(u => u.InternalUserId == INTERNAL_USER_ID)
                .SingleOrDefaultAsync();
            if (user == null)
            {
                user = new User { InternalUserId = INTERNAL_USER_ID };
                await Users.AddAsync(user);
                await SaveChangesAsync();
            }
            return user;
        }
    }
}
