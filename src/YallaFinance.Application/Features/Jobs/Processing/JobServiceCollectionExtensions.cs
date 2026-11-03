using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Text.Json;
using YallaFinance.Application.Features.Jobs.Shared;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.Jobs.Processing;

public static class JobServiceCollectionExtensions
{
    public static IServiceCollection AddJob<TPayload, THandler>(
        this IServiceCollection services,
        string type,
        string? title = null)
        where THandler : class, IJobHandler<TPayload>
    {
        ArgumentNullException.ThrowIfNull(services);

        if (string.IsNullOrWhiteSpace(type) || type.Length > 100)
            throw new ArgumentException(
                "A stable job type of at most 100 characters is required.",
                nameof(type));

        if (title is not null && string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("A non-empty job title is required when provided.", nameof(title));

        if (services.Any(serviceDescriptor => serviceDescriptor.ServiceType == typeof(JobRegistration) &&
            serviceDescriptor.ImplementationInstance is JobRegistration entry &&
            entry.Type == type))
            throw new InvalidOperationException("Duplicate job type registration.");

        services.TryAddScoped<THandler>();

        // Metadados são singleton; o registro executável é scoped e captura o handler tipado.
        services.AddSingleton(new JobRegistration(type, typeof(TPayload), title?.Trim() ?? type));
        services.AddScoped(provider => new JobHandlerRegistration(
            type,
            async (json, token) =>
            {
                TPayload payload;
                try
                {
                    payload = JsonSerializer.Deserialize<TPayload>(json, JsonSerializerOptions.Web) ??
                        throw new InvalidJobPayloadException();
                }
                catch (JsonException)
                {
                    throw new InvalidJobPayloadException();
                }

                // Resolve the known handler only after payload validation and context initialization.
                var jobHandler = provider.GetRequiredService<THandler>();
                return await jobHandler.HandleAsync(payload, token);
            }));
        return services;
    }

    public static IServiceCollection AddJobEngine(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddScoped<IJobRegistry, JobRegistry>();
        services.TryAddScoped<IJobHandlerExecutor, JobHandlerExecutor>();
        services.TryAddScoped<JobExecutionContext>();
        services.TryAddScoped<IJobExecutionContext>(provider => provider.GetRequiredService<JobExecutionContext>());
        services.TryAddScoped<JobService>();
        services.TryAddScoped<JobQueries>();
        return services;
    }
}
