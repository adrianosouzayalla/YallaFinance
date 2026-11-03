using YallaFinance.Application.Features.Jobs.Processing;
using YallaFinance.Application.Features.Jobs.Shared;
using YallaFinance.Application.Repositories.Jobs.Contracts;
using YallaFinance.Application.Repositories.Jobs;
using YallaFinance.Application;
namespace YallaFinance.Application.Features.Jobs.ReprocessJob;

public sealed record JobReprocessingReceipt(string Message);
