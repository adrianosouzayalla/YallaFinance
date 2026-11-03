using MediatR;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Features.Jobs.Shared.Monitoring;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.Jobs.ListJobTypes;

public sealed record ListJobTypesQuery : IRequest<NotificationResult<IReadOnlyList<JobMonitoringType>>>;
