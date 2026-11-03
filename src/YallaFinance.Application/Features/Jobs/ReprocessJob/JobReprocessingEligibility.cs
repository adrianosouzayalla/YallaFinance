using YallaFinance.Domain.Jobs;
using YallaFinance.Application.Features.Jobs.Processing;
using YallaFinance.Application.Features.Jobs.Shared;
using YallaFinance.Application.Repositories.Jobs.Contracts;
using YallaFinance.Application.Repositories.Jobs;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.Jobs.ReprocessJob;

public static class JobReprocessingEligibility
{
    public static bool CanReprocess(Job job) => job.Status == JobStatus.Failed;
}
