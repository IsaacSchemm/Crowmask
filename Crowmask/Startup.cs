using CrosspostSharp3.Weasyl;
using Crowmask.ActivityPub;
using Crowmask.Cache;
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
        private record AdminActor(string Handle) : IAdminActor;

        private record PublicKey(string Pem) : IPublicKey;

        private record WeasylApiKeyProvider(string ApiKey) : IWeasylApiKeyProvider;

        public override void Configure(IFunctionsHostBuilder builder)
        {
            if (Environment.GetEnvironmentVariable("AdminActor") is string handle)
                builder.Services.AddSingleton<IAdminActor>(new AdminActor(handle));

            if (Environment.GetEnvironmentVariable("SqlConnectionString") is string connectionString)
                builder.Services.AddDbContext<CrowmaskDbContext>(options => options.UseSqlServer(connectionString));

            if (Environment.GetEnvironmentVariable("PublicKeyPem") is string pem)
                builder.Services.AddSingleton<IPublicKey>(new PublicKey(pem));

            if (Environment.GetEnvironmentVariable("WeasylApiKey") is string apiKey)
                builder.Services.AddSingleton<IWeasylApiKeyProvider>(new WeasylApiKeyProvider(apiKey));

            builder.Services.AddScoped<CrowmaskCache, CrowmaskCache>();

            builder.Services.AddScoped<WeasylClient>();
        }
    }
}
