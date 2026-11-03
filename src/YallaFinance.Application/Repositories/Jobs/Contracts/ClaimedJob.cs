using YallaFinance.Domain.Jobs;
using YallaFinance.Application;

namespace YallaFinance.Application.Repositories.Jobs.Contracts;

public sealed record ClaimedJob(Job Job, JobExecution Execution);
