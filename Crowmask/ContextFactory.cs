using Crowmask.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Crowmask
{
    internal class ContextFactory : IDesignTimeDbContextFactory<CrowmaskDbContext>
    {
        private record LocalConfig(string SqlConnectionString);

        public CrowmaskDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<CrowmaskDbContext>();
            optionsBuilder.UseSqlServer("Data Source=(LocalDB)\\MSSQLLocalDB;Integrated Security=true;Database=Crowmask20231213");
            return new CrowmaskDbContext(optionsBuilder.Options);
        }
    }
}
