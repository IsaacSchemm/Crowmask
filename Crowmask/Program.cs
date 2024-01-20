using Azure.Identity;
using Crowmask;
using Crowmask.Data;
using Crowmask.Formats;
using Crowmask.Interfaces;
using Crowmask.Library;
using Crowmask.Library.Feed;
using Crowmask.Library.Remote;
using Crowmask.Library.Signatures;
using Crowmask.LowLevel;
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

        services.AddSingleton<IContentNegotiationConfiguration>(
            new ContentNegotiationConfiguration(
                ReturnHTML: true,
                ReturnMarkdown: true,
                UpstreamRedirect: false));

        if (Environment.GetEnvironmentVariable("CrowmaskHost") is string crowmaskHost)
            services.AddSingleton<ICrowmaskHost>(new Host(crowmaskHost));

        if (Environment.GetEnvironmentVariable("HandleHost") is string handleHost)
            services.AddSingleton<IHandleHost>(new Host(handleHost));

        if (Environment.GetEnvironmentVariable("HandleName") is string handleName)
            services.AddSingleton<IHandleName>(new HandleName(handleName));

        if (Environment.GetEnvironmentVariable("KeyVaultHost") is string keyVaultHost)
            services.AddSingleton<IKeyVaultHost>(new Host(keyVaultHost));

        services.AddSingleton<ICrowmaskVersion>(new Version("1.3"));

        if (Environment.GetEnvironmentVariable("WeasylApiKey") is string apiKey)
            services.AddSingleton<IWeasylApiKeyProvider>(new WeasylApiKeyProvider(apiKey));

        services.AddHttpClient();

        services.AddScoped<ICrowmaskKeyProvider, KeyProvider>();
        services.AddScoped<IInteractionLookup, FastInteractionLookup>();

        services.AddScoped<ActivityPubTranslator>();
        services.AddScoped<ActivityStreamsIdMapper>();
        services.AddScoped<ContentNegotiator>();
        services.AddScoped<SubmissionCache>();
        services.AddScoped<FeedBuilder>();
        services.AddScoped<InboxHandler>();
        services.AddScoped<MarkdownTranslator>();
        services.AddScoped<MastodonVerifier>();
        services.AddScoped<OutboundActivityProcessor>();
        services.AddScoped<RemoteInboxLocator>();
        services.AddScoped<Requester>();
        services.AddScoped<Summarizer>();
        services.AddScoped<UserCache>();
        services.AddScoped<WeasylClient>();
    })
    .Build();

host.Run();

record AdminActor(string Id) : IAdminActor;

record ContentNegotiationConfiguration(bool ReturnHTML, bool ReturnMarkdown, bool UpstreamRedirect) : IContentNegotiationConfiguration;

record Host(string Hostname) : ICrowmaskHost, IHandleHost, IKeyVaultHost;

record HandleName(string PreferredUsername) : IHandleName;

record Version(string VersionNumber) : ICrowmaskVersion;

record WeasylApiKeyProvider(string ApiKey) : IWeasylApiKeyProvider;
