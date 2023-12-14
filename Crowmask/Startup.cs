using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;

[assembly: FunctionsStartup(typeof(Crowmask.Startup))]

namespace Crowmask
{
    internal class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            string connectionString = Environment.GetEnvironmentVariable("SqlConnectionString")
                ?? "Data Source=(LocalDB)\\MSSQLLocalDB;Integrated Security=true;Database=Crowmask20231213";
            builder.Services.AddDbContext<CrowmaskDbContext>(options => options.UseSqlServer(connectionString));
        }
    }
}
