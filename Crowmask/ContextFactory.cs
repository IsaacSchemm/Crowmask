using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.IO;
using System.Text.Json;

namespace Crowmask
{
    internal class ContextFactory : IDesignTimeDbContextFactory<CrowmaskDbContext>
    {
        private record LocalConfig(string SqlConnectionString);

        public CrowmaskDbContext CreateDbContext(string[] args)
        {
            string json = File.ReadAllText("local.settings.json");
            var localConfig = JsonSerializer.Deserialize<LocalConfig>(json);
            var optionsBuilder = new DbContextOptionsBuilder<CrowmaskDbContext>();
            optionsBuilder.UseSqlServer(localConfig.SqlConnectionString);
            return new CrowmaskDbContext(optionsBuilder.Options);
        }
    }
}
