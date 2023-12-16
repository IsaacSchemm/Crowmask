using CrosspostSharp3.Weasyl;
using Crowmask.Data;
using Crowmask.Weasyl;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;

[assembly: FunctionsStartup(typeof(Crowmask.Startup))]

namespace Crowmask
{
    internal class Startup : FunctionsStartup
    {
        private record WeasylApiKeyProvider(string ApiKey) : IWeasylApiKeyProvider;

        private record AdminActor(string Handle) : IAdminActor;

        public override void Configure(IFunctionsHostBuilder builder)
        {
            if (Environment.GetEnvironmentVariable("AdminActor") is string handle)
                builder.Services.AddSingleton<IAdminActor>(new AdminActor(handle));

            if (Environment.GetEnvironmentVariable("SqlConnectionString") is string connectionString)
                builder.Services.AddDbContext<CrowmaskDbContext>(options => options.UseSqlServer(connectionString));

            if (Environment.GetEnvironmentVariable("WeasylApiKey") is string apiKey)
                builder.Services.AddSingleton<IWeasylApiKeyProvider>(new WeasylApiKeyProvider(apiKey));

            builder.Services.AddScoped<WeasylClient>();
        }
    }
}
