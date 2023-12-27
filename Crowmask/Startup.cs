using Crowmask.ActivityPub;
using Crowmask.Cache;
using Crowmask.Data;
using Crowmask.DomainModeling;
using Crowmask.Markdown;
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

        private record Host(string Hostname) : ICrowmaskHost, IHandleHost;

        private record WeasylApiKeyProvider(string ApiKey) : IWeasylApiKeyProvider;

        public override void Configure(IFunctionsHostBuilder builder)
        {
            if (Environment.GetEnvironmentVariable("AdminActor") is string id)
                builder.Services.AddSingleton<IAdminActor>(new AdminActor(id));

            if (Environment.GetEnvironmentVariable("CosmosDBConnectionString") is string connectionString)
                builder.Services.AddDbContext<CrowmaskDbContext>(options => options.UseCosmos(connectionString, databaseName: "Crowmask"));

            if (Environment.GetEnvironmentVariable("CrowmaskHost") is string crowmaskHost)
                builder.Services.AddSingleton<ICrowmaskHost>(new Host(crowmaskHost));

            if (Environment.GetEnvironmentVariable("HandleHost") is string handleHost)
                builder.Services.AddSingleton<IHandleHost>(new Host(handleHost));

            if (Environment.GetEnvironmentVariable("KeyVaultHost") is string keyVaultHost)
            {
                var provider = new KeyProvider(new Uri($"https://{keyVaultHost}"));
                builder.Services.AddSingleton<ISigner>(provider);
                builder.Services.AddSingleton<IPublicKeyProvider>(provider);
            }

            if (Environment.GetEnvironmentVariable("WeasylApiKey") is string apiKey)
                builder.Services.AddSingleton<IWeasylApiKeyProvider>(new WeasylApiKeyProvider(apiKey));

            builder.Services.AddHttpClient();

            builder.Services.AddScoped<CrowmaskCache>();
            builder.Services.AddScoped<MarkdownTranslator>();
            builder.Services.AddScoped<Notifier>();
            builder.Services.AddScoped<OutboundActivityProcessor>();
            builder.Services.AddScoped<Requester>();
            builder.Services.AddScoped<Translator>();
            builder.Services.AddScoped<Synchronizer>();
            builder.Services.AddScoped<WeasylClient>();
        }
    }
}
