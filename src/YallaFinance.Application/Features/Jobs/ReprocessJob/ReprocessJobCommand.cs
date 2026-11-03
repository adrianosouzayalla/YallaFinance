using MediatR;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Features.Jobs.ReprocessJob;
using YallaFinance.Application;


namespace YallaFinance.Application.Features.Jobs.ReprocessJob;

public sealed record ReprocessJobCommand(string Id) : IRequest<NotificationResult<JobReprocessingReceipt>>;
