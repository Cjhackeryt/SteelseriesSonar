using MacroDeck.Plugin.Hosting;
using MacroDeck.Plugin.Serilog;
using Microsoft.Extensions.DependencyInjection;
using SteelSeriesSonarPlugin;

var builder = MacroDeckPlugin.CreatePlugin(args)
    .UseMacroDeckLogging()
    .UseLocalization(Strings.LocalizationCatalog);

// Register SonarClient with configured HttpClient via IHttpClientFactory
builder.Services.AddHttpClient<SonarClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(5);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    // SteelSeries GG uses self-signed certificates for localhost IPC
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
});

var plugin = builder
    .RegisterIntegration<SonarIntegration>()
    .Build();

await plugin.RunAsync();
