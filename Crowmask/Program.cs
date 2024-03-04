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
using System.Collections.Generic;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services => {
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

        services.AddSingleton<IApplicationInformation>(new AppInfo(
            ApplicationName: "Crowmask",
            VersionNumber: "1.5.1",
            ApplicationHostname: Environment.GetEnvironmentVariable("CrowmaskHost"),
            WebsiteUrl: $"https://github.com/IsaacSchemm/Crowmask/",
            Username: Environment.GetEnvironmentVariable("HandleName"),
            HandleHostname: Environment.GetEnvironmentVariable("HandleHost"),
            AdminActorId: Environment.GetEnvironmentVariable("AdminActor"),
            ReturnHTML: true,
            ReturnMarkdown: true,
            UpstreamRedirect: false,
            ATProtoPDS: Environment.GetEnvironmentVariable("ATProtoPDS"),
            ATProtoHandle: Environment.GetEnvironmentVariable("ATProtoHandle"),
            ATProtoIdentifier: Environment.GetEnvironmentVariable("ATProtoIdentifier"),
            ATProtoPassword: Environment.GetEnvironmentVariable("ATProtoPassword")));

        services.AddSingleton<IActorKeyProvider>(
            new KeyProvider(
                $"https://{Environment.GetEnvironmentVariable("KeyVaultHost")}"));

        services.AddSingleton<IWeasylApiKeyProvider>(
            new WeasylApiKeyProvider(
                Environment.GetEnvironmentVariable("WeasylApiKey")));

        services.AddHttpClient();

        services.AddScoped<ActivityPubTranslator>();
        services.AddScoped<IdMapper>();
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

record AppInfo(
    string ApplicationName,
    string VersionNumber,
    string ApplicationHostname,
    string WebsiteUrl,
    string Username,
    string HandleHostname,
    string AdminActorId,
    bool ReturnHTML,
    bool ReturnMarkdown,
    bool UpstreamRedirect,
    string ATProtoPDS,
    string ATProtoHandle,
    string ATProtoIdentifier,
    string ATProtoPassword) : IApplicationInformation, IATProtoAccount
{
    string IApplicationInformation.UserAgent =>
        $"{ApplicationName}/{VersionNumber} ({WebsiteUrl})";

    IEnumerable<string> IApplicationInformation.AdminActorIds
    {
        get
        {
            if (!string.IsNullOrEmpty(AdminActorId))
                yield return AdminActorId;

            yield return "https://activitypub.academy/users/bagulia_aeldaed";
        }
    }

    IEnumerable<IATProtoAccount> IApplicationInformation.ATProtoBotAccounts {
        get
        {
            if (!string.IsNullOrEmpty(ATProtoPDS) && !string.IsNullOrEmpty(ATProtoHandle))
                yield return this;
        }
    }

    string IATProtoAccount.Hostname => ATProtoPDS;

    string IATProtoAccount.Handle => ATProtoHandle;

    string IATProtoAccount.Identifier => ATProtoIdentifier;

    string IATProtoAccount.Password => ATProtoPassword;
}

record WeasylApiKeyProvider(string ApiKey) : IWeasylApiKeyProvider;
