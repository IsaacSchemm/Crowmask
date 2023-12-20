using CrosspostSharp3.Weasyl;
using Crowmask.ActivityPub;
using Crowmask.Cache;
using Crowmask.Data;
using Crowmask.Remote;
using Crowmask.Weasyl;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

[assembly: FunctionsStartup(typeof(Crowmask.Startup))]

namespace Crowmask
{
    internal class Startup : FunctionsStartup
    {
        private record AdminActor(string Handle) : IAdminActor;

        private record PublicKey(string Pem) : IPublicKey, IPublicKeyProvider
        {
            async Task<IPublicKey> IPublicKeyProvider.GetPublicKeyAsync() => this;
        }

        private record WeasylApiKeyProvider(string ApiKey) : IWeasylApiKeyProvider;

        public override void Configure(IFunctionsHostBuilder builder)
        {
            if (Environment.GetEnvironmentVariable("AdminActor") is string handle)
                builder.Services.AddSingleton<IAdminActor>(new AdminActor(handle));

            if (Environment.GetEnvironmentVariable("CosmosDBConnectionString") is string connectionString)
                builder.Services.AddDbContext<CrowmaskDbContext>(options => options.UseCosmos(connectionString, databaseName: "Crowmask"));

            var k = new PublicKey("-----BEGIN PUBLIC KEY-----\\nMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAoHfLR9OTkg8mMvziXlrt8uQqWH3u13RJSlCN1w0TE7R0WvG4w1SEL+QWQY61X+STRJ/emzPX3fi6X/FTapLrMdVg4CHio3VW5Jr8qvgG56NfJ5QCxDsB+VzLiCWVp7Dge2v6WGgitfndNhMu/nvUMRft8a+Q7QWqNQ9iNCVBS1KRm2WEVs0hUvfCubQtv0DzUFTmnFi1sjHG/G1kwlukp/V+fLqGQzBjkrdQ0vvorRZwKvnTjdqRNjgq9580x+tEHfnCX4DScnwu/jWEMD9VmpZfE4/UD91yQMCihqv/NvAU0EVdgnH1hI2xWDhCeQ1zEKCS/bCcHxT30SLfsMI2PQIDAQAB\\n-----END PUBLIC KEY-----");
            builder.Services.AddSingleton<IPublicKey>(k);
            builder.Services.AddSingleton<IPublicKeyProvider>(k);

            if (Environment.GetEnvironmentVariable("WeasylApiKey") is string apiKey)
                builder.Services.AddSingleton<IWeasylApiKeyProvider>(new WeasylApiKeyProvider(apiKey));

            builder.Services.AddScoped<CrowmaskCache>();
            builder.Services.AddScoped<OutboundActivityProcessor>();

            builder.Services.AddScoped<WeasylClient>();
        }
    }
}
