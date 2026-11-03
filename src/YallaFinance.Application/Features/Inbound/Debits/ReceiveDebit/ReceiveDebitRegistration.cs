using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using YallaFinance.Application.Features.Jobs.Processing;
using YallaFinance.Application.Features.Jobs.Shared;
using YallaFinance.Application.Repositories.Jobs.Contracts;
using YallaFinance.Application.Repositories.Jobs;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.Inbound.Debits.ReceiveDebit;

public static class ReceiveDebitRegistration
{
    public static IServiceCollection AddDebitInboundJobs(this IServiceCollection services)
    {
        services.AddJob<ReceiveDebitPayload, ReceiveDebitHandler>(
            ReceiveDebitJob.JobType,
            ReceiveDebitJob.JobTitle);
        services.TryAddScoped<IValidator<ReceiveDebitPayload>, ReceiveDebitPayloadValidator>();
        return services;
    }
}
