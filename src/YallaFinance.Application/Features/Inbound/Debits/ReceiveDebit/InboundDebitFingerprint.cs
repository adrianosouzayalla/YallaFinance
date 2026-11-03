using System.Security.Cryptography;
using System.Text.Json;
using YallaFinance.Application;
namespace YallaFinance.Application.Features.Inbound.Debits.ReceiveDebit;

public static class InboundDebitFingerprint
{
    public static string Compute(ReceiveDebitPayload payload)
    {
        // Fix property order and decimal representation; optional values are always present.
        var inboundDebit = payload.Debit;
        var canonicalFingerprintJson = JsonSerializer.SerializeToUtf8Bytes(
            new
            {
                inboundDebit.ExternalId,
                Amount = inboundDebit.Amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                inboundDebit.OccurrenceDate,
                inboundDebit.DueDate,
                inboundDebit.Description,
                inboundDebit.CategoryCode,
                Contract = new
                {
                    inboundDebit.Contract.ExternalId,
                    inboundDebit.Contract.Number,
                    inboundDebit.Contract.Plate,
                    inboundDebit.Contract.Product,
                    inboundDebit.Contract.Fleet
                },
                Customer = new
                {
                    payload.Customer.ExternalId,
                    payload.Customer.Name,
                    payload.Customer.Document,
                    payload.Customer.DocumentType,
                    payload.Customer.Email,
                    payload.Customer.Phone
                }
            },
            JsonSerializerOptions.Web);
        var legacyFingerprint = Convert.ToHexStringLower(SHA256.HashData(canonicalFingerprintJson));
        if (!payload.Customer.HasExtendedData())
            return legacyFingerprint;

        var completeCustomerFingerprintJson = JsonSerializer.SerializeToUtf8Bytes(new
        {
            Version = 2,
            LegacyFingerprint = legacyFingerprint,
            payload.Customer.DocumentIssuingCountry,
            payload.Customer.PhoneCountryCallingCode,
            payload.Customer.PhoneNationalNumber,
            payload.Customer.Address
        }, JsonSerializerOptions.Web);
        return Convert.ToHexStringLower(SHA256.HashData(completeCustomerFingerprintJson));
    }
}
