using MediatR;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Features.Jobs.Shared.Monitoring;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.Jobs.GetJob;

public sealed record GetJobQuery(string Id) : IRequest<NotificationResult<JobMonitoringDetail>>;
