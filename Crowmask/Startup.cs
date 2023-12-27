using Crowmask.ActivityPub;
using Crowmask.Cache;
using Crowmask.Data;
using Crowmask.Remote;
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
        private record AdminActor(string Id) : IAdminActor;

        private record CrowmaskHost(string Hostname) : ICrowmaskHost;

        private record WeasylApiKeyProvider(string ApiKey) : IWeasylApiKeyProvider;

        public override void Configure(IFunctionsHostBuilder builder)
        {
            if (Environment.GetEnvironmentVariable("AdminActor") is string id)
                builder.Services.AddSingleton<IAdminActor>(new AdminActor(id));

            if (Environment.GetEnvironmentVariable("CosmosDBConnectionString") is string connectionString)
                builder.Services.AddDbContext<CrowmaskDbContext>(options => options.UseCosmos(connectionString, databaseName: "Crowmask"));

            if (Environment.GetEnvironmentVariable("CrowmaskHost") is string hostname)
                builder.Services.AddSingleton<ICrowmaskHost>(new CrowmaskHost(hostname));

            if (Environment.GetEnvironmentVariable("KeyVaultHost") is string keyVaultHost && Uri.TryCreate($"https://{keyVaultHost}", UriKind.Absolute, out Uri vaultUri))
            {
                var provider = new KeyProvider(vaultUri);
                builder.Services.AddSingleton<ISigner>(provider);
                builder.Services.AddSingleton<IPublicKeyProvider>(provider);
            }

            if (Environment.GetEnvironmentVariable("WeasylApiKey") is string apiKey)
                builder.Services.AddSingleton<IWeasylApiKeyProvider>(new WeasylApiKeyProvider(apiKey));

            builder.Services.AddHttpClient();

            builder.Services.AddScoped<CrowmaskCache>();
            builder.Services.AddScoped<Notifier>();
            builder.Services.AddScoped<OutboundActivityProcessor>();
            builder.Services.AddScoped<Requester>();
            builder.Services.AddScoped<Translator>();
            builder.Services.AddScoped<Synchronizer>();
            builder.Services.AddScoped<WeasylClient>();
        }
    }
}
