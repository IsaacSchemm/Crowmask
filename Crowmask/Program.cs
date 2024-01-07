using Azure.Identity;
using Crowmask;
using Crowmask.ActivityPub;
using Crowmask.Cache;
using Crowmask.Data;
using Crowmask.DomainModeling;
using Crowmask.Feed;
using Crowmask.Markdown;
using Crowmask.Remote;
using Crowmask.Signatures;
using Crowmask.Weasyl;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services => {
        if (Environment.GetEnvironmentVariable("AdminActor") is string id)
            services.AddSingleton<IAdminActor>(new AdminActor(id));

        if (Environment.GetEnvironmentVariable("CosmosDBAccountEndpoint") is string accountEndpoint)
            if (Environment.GetEnvironmentVariable("CosmosDBAccountKey") is string accountKey)
                services.AddDbContext<CrowmaskDbContext>(options => options.UseCosmos(
                    accountEndpoint,
                    accountKey,
                    databaseName: "Crowmask"));
            else
                services.AddDbContext<CrowmaskDbContext>(options => options.UseCosmos(
                    accountEndpoint,
                    new DefaultAzureCredential(),
                    databaseName: "Crowmask"));

        if (Environment.GetEnvironmentVariable("CrowmaskHost") is string crowmaskHost)
            services.AddSingleton<ICrowmaskHost>(new Host(crowmaskHost));

        if (Environment.GetEnvironmentVariable("HandleHost") is string handleHost)
            services.AddSingleton<IHandleHost>(new Host(handleHost));

        if (Environment.GetEnvironmentVariable("KeyVaultHost") is string keyVaultHost)
        {
            var provider = new KeyProvider(new Uri($"https://{keyVaultHost}"));
            services.AddSingleton<ISigner>(provider);
            services.AddSingleton<IPublicKeyProvider>(provider);
        }

        if (Environment.GetEnvironmentVariable("WeasylApiKey") is string apiKey)
            services.AddSingleton<IWeasylApiKeyProvider>(new WeasylApiKeyProvider(apiKey));

        services.AddHttpClient();

        services.AddScoped<ActivityStreamsIdMapper>();
        services.AddScoped<CrowmaskCache>();
        services.AddScoped<EngagementTranslator>();
        services.AddScoped<FeedBuilder>();
        services.AddScoped<MarkdownTranslator>();
        services.AddScoped<MastodonVerifier>();
        services.AddScoped<Notifier>();
        services.AddScoped<OutboundActivityProcessor>();
        services.AddScoped<Requester>();
        services.AddScoped<Translator>();
        services.AddScoped<WeasylBaseClient>();
        services.AddScoped<WeasylScraper>();
        services.AddScoped<WeasylUserClient>();
    })
    .Build();

host.Run();

record AdminActor(string Id) : IAdminActor;

record Host(string Hostname) : ICrowmaskHost, IHandleHost;

record WeasylApiKeyProvider(string ApiKey) : IWeasylApiKeyProvider;
