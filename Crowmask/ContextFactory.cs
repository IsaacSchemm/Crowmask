using Crowmask.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.IO;
using System.Text.Json;

namespace Crowmask
{
    internal class ContextFactory : IDesignTimeDbContextFactory<CrowmaskDbContext>
    {
        private record Values(string SqlConnectionString);
        private record LocalConfig(Values Values);

        public CrowmaskDbContext CreateDbContext(string[] args)
        {
            string json = File.ReadAllText("local.settings.json");
            var localConfig = JsonSerializer.Deserialize<LocalConfig>(json);
            var optionsBuilder = new DbContextOptionsBuilder<CrowmaskDbContext>();
            optionsBuilder.UseSqlServer(localConfig.Values.SqlConnectionString);
            return new CrowmaskDbContext(optionsBuilder.Options);
        }
    }
}
