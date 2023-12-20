using CrosspostSharp3.Weasyl;
using Crowmask.ActivityPub;
using Crowmask.Cache;
using Crowmask.Data;
using Crowmask.Remote;
using Crowmask.Weasyl;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

[assembly: FunctionsStartup(typeof(Crowmask.Startup))]

namespace Crowmask
{
    internal class Startup : FunctionsStartup
    {
        private record AdminActor(string Handle) : IAdminActor;

        private record WeasylApiKeyProvider(string ApiKey) : IWeasylApiKeyProvider;

        public override void Configure(IFunctionsHostBuilder builder)
        {
            if (Environment.GetEnvironmentVariable("AdminActor") is string handle)
                builder.Services.AddSingleton<IAdminActor>(new AdminActor(handle));

            if (Environment.GetEnvironmentVariable("CosmosDBConnectionString") is string connectionString)
                builder.Services.AddDbContext<CrowmaskDbContext>(options => options.UseCosmos(connectionString, databaseName: "Crowmask"));

            builder.Services.AddSingleton<IKeyProvider>(new KeyProvider());

            if (Environment.GetEnvironmentVariable("WeasylApiKey") is string apiKey)
                builder.Services.AddSingleton<IWeasylApiKeyProvider>(new WeasylApiKeyProvider(apiKey));

            builder.Services.AddScoped<CrowmaskCache>();
            builder.Services.AddScoped<OutboundActivityProcessor>();

            builder.Services.AddScoped<WeasylClient>();
        }
    }
}
