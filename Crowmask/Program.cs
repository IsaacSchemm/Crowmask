﻿using Crowmask;
using Crowmask.ActivityPub;
using Crowmask.Cache;
using Crowmask.Data;
using Crowmask.DomainModeling;
using Crowmask.Markdown;
using Crowmask.Remote;
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

        if (Environment.GetEnvironmentVariable("CosmosDBConnectionString") is string connectionString)
            services.AddDbContext<CrowmaskDbContext>(options => options.UseCosmos(connectionString, databaseName: "Crowmask"));

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

        services.AddScoped<CrowmaskCache>();
        services.AddScoped<MarkdownTranslator>();
        services.AddScoped<Notifier>();
        services.AddScoped<OutboundActivityProcessor>();
        services.AddScoped<Requester>();
        services.AddScoped<Translator>();
        services.AddScoped<Synchronizer>();
        services.AddScoped<WeasylClient>();
    })
    .Build();

host.Run();

record AdminActor(string Id) : IAdminActor;

record Host(string Hostname) : ICrowmaskHost, IHandleHost;

record WeasylApiKeyProvider(string ApiKey) : IWeasylApiKeyProvider;