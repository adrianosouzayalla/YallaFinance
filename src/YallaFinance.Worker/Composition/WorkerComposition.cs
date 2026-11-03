using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using YallaFinance.Application.Features.Charges.IntegrateCharge;
using YallaFinance.Application.Features.Inbound.Debits.ReceiveDebit;
using YallaFinance.Application.Features.Charges.SettleCharge;
using YallaFinance.Application.Features.Jobs.Processing;
using YallaFinance.Application.Features.Jobs.Shared;
using YallaFinance.Application.Repositories.Charges;
using YallaFinance.Application.Repositories.Charges.Contracts.Settlement;
using YallaFinance.Application.Repositories.Debits;
using YallaFinance.Application.Repositories.Jobs;
using YallaFinance.Application.Repositories.Jobs.Contracts;
using YallaFinance.Application.Services.Charges;
using YallaFinance.Application.Services.Charges.Contracts;
using YallaFinance.Infrastructure.Composition;
using YallaFinance.Worker.Processing.Continuous;
using YallaFinance.Worker.Processing.Jobs;

namespace YallaFinance.Worker.Composition;

public static class WorkerComposition
{
    public static IServiceCollection AddWorkerComposition(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        services.AddLogging();
        services.AddJobEngine();
        services.AddChargeIntegrationJobs();
        services.AddChargeSettlementJobs();
        services.AddDebitInboundJobs();
        services.AddInfrastructure(configuration);
        if (string.IsNullOrWhiteSpace(configuration.GetConnectionString("YallaFinance")))
        {
            throw new InvalidOperationException("Worker requires ConnectionStrings:YallaFinance.");
        }

        services.AddJobWorker(configuration);
        services.AddHostedService<PaymentReceiptWorker>();

        return services;
    }
}
