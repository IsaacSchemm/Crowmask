﻿using Azure.Identity;
using Crowmask;
using Crowmask.Data;
using Crowmask.HighLevel;
using Crowmask.HighLevel.ATProto;
using Crowmask.HighLevel.Feed;
using Crowmask.HighLevel.Notifications;
using Crowmask.HighLevel.Remote;
using Crowmask.HighLevel.Signatures;
using Crowmask.LowLevel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.FSharp.Collections;
using System;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services => {
        if (Environment.GetEnvironmentVariable("CosmosDBAccountKey") is string accountKey)
        {
            services.AddDbContext<CrowmaskDbContext>(options => options.UseCosmos(
                Environment.GetEnvironmentVariable("CosmosDBAccountEndpoint"),
                accountKey,
                databaseName: "Crowmask"));
            services.AddDbContextFactory<CrowmaskDbContext>(options => options.UseCosmos(
                Environment.GetEnvironmentVariable("CosmosDBAccountEndpoint"),
                accountKey,
                databaseName: "Crowmask"));
        }
        else
        {
            services.AddDbContext<CrowmaskDbContext>(options => options.UseCosmos(
                Environment.GetEnvironmentVariable("CosmosDBAccountEndpoint"),
                new DefaultAzureCredential(),
                databaseName: "Crowmask"));
            services.AddDbContextFactory<CrowmaskDbContext>(options => options.UseCosmos(
                Environment.GetEnvironmentVariable("CosmosDBAccountEndpoint"),
                new DefaultAzureCredential(),
                databaseName: "Crowmask"));
        }

        services.AddSingleton(new ApplicationInformation(
            applicationName: "Crowmask",
            versionNumber: "2.0",   
            applicationHostname: Environment.GetEnvironmentVariable("CrowmaskHost"),
            websiteUrl: "https://github.com/IsaacSchemm/Crowmask/",
            username: Environment.GetEnvironmentVariable("HandleName"),
            handleHostname: Environment.GetEnvironmentVariable("HandleHost"),
            webFingerDomains: Environment.GetEnvironmentVariable("WebFingerDomain") is string wf
                ? SetModule.Singleton(wf)
                : SetModule.Empty<string>(),
            blueskyBotAccounts: Environment.GetEnvironmentVariable("BlueskyPDS") is string pds && Environment.GetEnvironmentVariable("BlueskyDID") is string did
                ? SetModule.Singleton(new BlueskyAccountConfiguration(
                    pds,
                    did,
                    Environment.GetEnvironmentVariable("BlueskyIdentifier"),
                    Environment.GetEnvironmentVariable("BlueskyPassword")))
                : SetModule.Empty<BlueskyAccountConfiguration>()));

        services.AddSingleton<IActorKeyProvider>(
            new KeyProvider(
                $"https://{Environment.GetEnvironmentVariable("KeyVaultHost")}"));

        services.AddSingleton(
            new WeasylAuthorizationProvider(
                Environment.GetEnvironmentVariable("WeasylApiKey")));

        services.AddHttpClient();

        services.AddScoped<ActivityPubTranslator>();
        services.AddScoped<BlueskyAgent>();
        services.AddScoped<ContentNegotiator>();
        services.AddScoped<SubmissionCache>();
        services.AddScoped<FeedBuilder>();
        services.AddScoped<IdMapper>();
        services.AddScoped<InboxHandler>();
        services.AddScoped<MarkdownTranslator>();
        services.AddScoped<MastodonVerifier>();
        services.AddScoped<NotificationCollector>();
        services.AddScoped<OutboundActivityProcessor>();
        services.AddScoped<RemoteInboxLocator>();
        services.AddScoped<Requester>();
        services.AddScoped<Summarizer>();
        services.AddScoped<UserCache>();
        services.AddScoped<WeasylClient>();
    })
    .Build();

host.Run();
