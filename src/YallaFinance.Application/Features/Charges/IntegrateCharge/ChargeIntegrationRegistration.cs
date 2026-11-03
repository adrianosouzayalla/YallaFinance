using Microsoft.Extensions.DependencyInjection;
using YallaFinance.Application.Features.Jobs.Processing;
using YallaFinance.Application.Features.Jobs.Shared;
using YallaFinance.Application.Repositories.Jobs.Contracts;
using YallaFinance.Application.Repositories.Jobs;
using YallaFinance.Application;


namespace YallaFinance.Application.Features.Charges.IntegrateCharge;

public static class ChargeIntegrationRegistration
{
    public static IServiceCollection AddChargeIntegrationJobs(this IServiceCollection services)
    {
        services.AddJob<IntegrateChargePayload, IntegrateChargeHandler>(
            IntegrateChargeJob.JobType,
            IntegrateChargeJob.JobTitle);
        services.AddScoped<ChargeProcessingQueries>();
        return services;
    }
}
