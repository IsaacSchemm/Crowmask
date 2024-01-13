using Azure.Identity;
using Crowmask;
using Crowmask.Data;
using Crowmask.Dependencies.Mapping;
using Crowmask.Dependencies.Weasyl;
using Crowmask.Formats;
using Crowmask.Interfaces;
using Crowmask.Library.Cache;
using Crowmask.Library.Feed;
using Crowmask.Library.Remote;
using Crowmask.Library.Signatures;
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
            services.AddSingleton<IKeyVaultHost>(new Host(keyVaultHost));

        if (Environment.GetEnvironmentVariable("WeasylApiKey") is string apiKey)
            services.AddSingleton<IWeasylApiKeyProvider>(new WeasylApiKeyProvider(apiKey));

        services.AddHttpClient();

        services.AddScoped<ICrowmaskKeyProvider, KeyProvider>();
        services.AddScoped<IInteractionLookup, FastInteractionLookup>();

        services.AddScoped<ActivityPubTranslator>();
        services.AddScoped<ActivityStreamsIdMapper>();
        services.AddScoped<ContentNegotiator>();
        services.AddScoped<CrowmaskCache>();
        services.AddScoped<DatabaseActions>();
        services.AddScoped<FeedBuilder>();
        services.AddScoped<MarkdownTranslator>();
        services.AddScoped<MastodonVerifier>();
        services.AddScoped<OutboundActivityProcessor>();
        services.AddScoped<RemoteActions>();
        services.AddScoped<Requester>();
        services.AddScoped<Summarizer>();
        services.AddScoped<WeasylUserClient>();
    })
    .Build();

host.Run();

record AdminActor(string Id) : IAdminActor;

record Host(string Hostname) : ICrowmaskHost, IHandleHost, IKeyVaultHost;

record WeasylApiKeyProvider(string ApiKey) : IWeasylApiKeyProvider;
