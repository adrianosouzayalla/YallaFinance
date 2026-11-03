using System.Security.Cryptography;
using System.Text;
using YallaFinance.Application.Features.Jobs.Processing;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.Inbound.Debits.ReceiveDebit;

public sealed record ReceiveDebitJob(ReceiveDebitPayload Payload) : IJobDefinition<ReceiveDebitPayload>
{
    public const string JobType = "FINANCIAL_DEBIT_INBOUND_V1";
    public const string JobTitle = "Receber débito de integração";
    public const string Origin = "Financial Inbound";
    public string Type => JobType;
    public string BuildKey()
    {
        var jobKey = $"debit-inbound:{Payload.Debit.ExternalId}";
        return jobKey.Length <= 250 ? jobKey : $"debit-inbound:sha256:{Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(Payload.Debit.ExternalId)))}";
    }
    public ReceiveDebitPayload BuildPayload() => Payload;
}
