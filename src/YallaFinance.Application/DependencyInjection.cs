using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using MediatR;
using YallaFinance.Application.Common.Behaviors;

using YallaFinance.Application.Features.Charges.IntegrateCharge;
using YallaFinance.Application.Features.Jobs.Processing;
using YallaFinance.Application.Features.Jobs.Shared;
using YallaFinance.Application.Repositories.Jobs.Contracts;
using YallaFinance.Application.Repositories.Jobs;
using YallaFinance.Application.Features.Inbound.Debits.ReceiveDebit;
using YallaFinance.Application.Repositories.Debits;
using YallaFinance.Application.Features.Charges.SettleCharge;
using YallaFinance.Application.Repositories.Charges.Contracts.Settlement;
using YallaFinance.Application.Repositories.Charges;
using YallaFinance.Application.Services.Charges.Contracts;
using YallaFinance.Application.Services.Charges;



namespace YallaFinance.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddMediatR(configuration => configuration.RegisterServicesFromAssembly(typeof(AssemblyReference).Assembly));
        services.AddValidatorsFromAssembly(typeof(AssemblyReference).Assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddJobEngine();
        services.AddChargeIntegrationJobs();
        services.AddChargeSettlementJobs();
        services.AddDebitInboundJobs();
        services.AddScoped<Features.Jobs.Shared.Monitoring.JobMonitoringService>();
        services.AddScoped<Features.Jobs.ReprocessJob.JobReprocessingService>();
        services.AddScoped<Features.Jobs.Retry.JobRetrySweeper>();
        services.AddScoped<Services.Charges.IChargeCreationService, Features.Charges.CreateCharge.ChargeCreationService>();
        return services;
    }
}
