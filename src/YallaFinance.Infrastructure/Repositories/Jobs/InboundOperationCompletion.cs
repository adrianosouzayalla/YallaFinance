using System.Text.Json;
using YallaFinance.Application.Features.Inbound.Debits.ReceiveDebit;
using YallaFinance.Application.Features.Jobs.Processing;

namespace YallaFinance.Infrastructure.Repositories.Jobs;

public static class InboundOperationCompletion
{
    public static JobHandlerResult Normalize(string jobType, JobHandlerResult result)
    {
        if (!result.Success)
            return result;
        return jobType == ReceiveDebitJob.JobType && DebtId(result) is not null
            ? result : new(
                false,
                Error: new(
                    "InvalidHandlerResult",
                    "Resultado da operação sem referência válida de débito."));
    }

    public static Guid? DebtId(JobHandlerResult result)
    {
        if (result.Result is not { ValueKind: JsonValueKind.Object } json
            || !json.TryGetProperty("debtId", out var value) || value.ValueKind != JsonValueKind.String
            || !value.TryGetGuid(out var id) || id == Guid.Empty)
            return null;
        return id;
    }
}
