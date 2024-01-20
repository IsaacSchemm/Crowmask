using Azure.Identity;
using Crowmask;
using Crowmask.Data;
using Crowmask.Interfaces;
using Crowmask.HighLevel;
using Crowmask.HighLevel.Feed;
using Crowmask.HighLevel.Remote;
using Crowmask.HighLevel.Signatures;
using Crowmask.LowLevel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services => {
        services.AddSingleton<IAdminActor>(
            new AdminActor(
                Environment.GetEnvironmentVariable("AdminActor")));

        if (Environment.GetEnvironmentVariable("CosmosDBAccountKey") is string accountKey)
            services.AddDbContext<CrowmaskDbContext>(options => options.UseCosmos(
                Environment.GetEnvironmentVariable("CosmosDBAccountEndpoint"),
                accountKey,
                databaseName: "Crowmask"));
        else
            services.AddDbContext<CrowmaskDbContext>(options => options.UseCosmos(
                Environment.GetEnvironmentVariable("CosmosDBAccountEndpoint"),
                new DefaultAzureCredential(),
                databaseName: "Crowmask"));

        services.AddSingleton<IContentNegotiationConfiguration>(
            new ContentNegotiationConfiguration(
                ReturnHTML: true,
                ReturnMarkdown: true,
                UpstreamRedirect: false));

        services.AddSingleton<IApplicationInformation>(new AppInfo(
            "Crowmask",
            "1.4",
            Environment.GetEnvironmentVariable("CrowmaskHost"),
            $"https://github.com/IsaacSchemm/Crowmask/"));

        services.AddSingleton<IHandle>(
            new Handle(
                Environment.GetEnvironmentVariable("HandleName"),
                Environment.GetEnvironmentVariable("HandleHost")));

        services.AddSingleton<IActorKeyProvider>(
            new KeyProvider(
                $"https://{Environment.GetEnvironmentVariable("KeyVaultHost")}"));

        services.AddSingleton<IWeasylApiKeyProvider>(
            new WeasylApiKeyProvider(
                Environment.GetEnvironmentVariable("WeasylApiKey")));

        services.AddHttpClient();

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

record AppInfo(
    string ApplicationName,
    string VersionNumber,
    string Hostname,
    string WebsiteUrl) : IApplicationInformation
{
    string IApplicationInformation.UserAgent => $"{ApplicationName}/{VersionNumber} ({WebsiteUrl})";
}

record ContentNegotiationConfiguration(bool ReturnHTML, bool ReturnMarkdown, bool UpstreamRedirect) : IContentNegotiationConfiguration;

record Handle(string PreferredUsername, string Hostname) : IHandle;

record WeasylApiKeyProvider(string ApiKey) : IWeasylApiKeyProvider;
