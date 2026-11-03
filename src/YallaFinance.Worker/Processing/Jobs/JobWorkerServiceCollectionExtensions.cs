
using YallaFinance.Application.Common.Context;
using YallaFinance.Application.Features.Jobs.Processing;

namespace YallaFinance.Worker.Processing.Jobs;

public static class JobWorkerServiceCollectionExtensions
{
    public static IServiceCollection AddJobWorker(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddJobEngine();
        services.AddScoped<JobProcessor>();
        services.AddScoped<JobTenantContext>();
        services.AddScoped<IJobTenantContext>(serviceProvider =>
            serviceProvider.GetRequiredService<JobTenantContext>());
        services.AddScoped<ICurrentTenantContext>(serviceProvider =>
            serviceProvider.GetRequiredService<JobTenantContext>());
        services.AddOptions<JobWorkerOptions>()
            .Bind(configuration.GetSection("Jobs"))
            .Validate(
                jobWorkerOptions => jobWorkerOptions.BatchSize > 0 &&
                    jobWorkerOptions.PollingIntervalSeconds > 0,
                "Job batch size and polling interval must be positive.")
            .ValidateOnStart();
        services.AddHostedService<JobWorker>();
        return services;
    }
}
