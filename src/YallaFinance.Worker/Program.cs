using Serilog;
using YallaFinance.Worker.Composition;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var workerHostBuilder = Host.CreateApplicationBuilder(args);

    var evidenceDirectoryConfigurationKeys = new[]
    {
        "FinancialIntegrations:EvidenceDirectory"
    };

    foreach (var evidenceDirectoryConfigurationKey in evidenceDirectoryConfigurationKeys)
    {
        var configuredEvidenceDirectoryPath =
            workerHostBuilder.Configuration[evidenceDirectoryConfigurationKey];

        if (configuredEvidenceDirectoryPath is { Length: > 0 })
        {
            workerHostBuilder.Configuration[evidenceDirectoryConfigurationKey] = Path.GetFullPath(
                configuredEvidenceDirectoryPath,
                workerHostBuilder.Environment.ContentRootPath);
        }
    }

    workerHostBuilder.Services.AddSerilog(
        (serviceProvider, loggerConfiguration) => loggerConfiguration
            .ReadFrom.Configuration(workerHostBuilder.Configuration)
            .ReadFrom.Services(serviceProvider)
            .Enrich.FromLogContext()
            .WriteTo.Console());
    workerHostBuilder.Services.AddWorkerComposition(workerHostBuilder.Configuration);

    var workerHost = workerHostBuilder.Build();
    workerHost.Run();
}
catch (Exception exception)
{
    Log.Fatal(exception, "O Worker foi encerrado inesperadamente.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
