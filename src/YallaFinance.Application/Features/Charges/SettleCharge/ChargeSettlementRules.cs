using YallaFinance.Application.Repositories.Charges.Contracts.Settlement;
using YallaFinance.Application;
namespace YallaFinance.Application.Features.Charges.SettleCharge;

internal static class ChargeSettlementRules
{
    public static string Identifier(Guid paymentTransactionId, Guid chargeItemId) =>
        $"yf:{paymentTransactionId:N}:{chargeItemId:N}";

    public static string? Validate(ChargeSettlementSnapshot charge, Guid tenantId, Guid chargeId)
    {
        if (charge.TenantId != tenantId || charge.ChargeId != chargeId || !Money(charge.Amount)
            || charge.Items is not { Count: > 0 } || charge.Items.Any(chargeItem => chargeItem is null || chargeItem.TenantId != tenantId
                || chargeItem.ChargeId != chargeId || chargeItem.Id == Guid.Empty || !Money(chargeItem.Amount)
                || string.IsNullOrWhiteSpace(chargeItem.Provider)
                || !IsValidExternalIdentifier(chargeItem.ProviderExternalId))
            || charge.Items.Select(chargeItem => chargeItem.Id).Distinct().Count() != charge.Items.Count
            || charge.Items.Select(chargeItem => chargeItem.ProviderExternalId).Distinct(StringComparer.Ordinal)
                .Count() != charge.Items.Count
            || charge.Items.Select(chargeItem => chargeItem.Ordinal).Distinct().Count() != charge.Items.Count
            || charge.Items.Sum(chargeItem => chargeItem.Amount) != charge.Amount)
            return "InvalidChargeItems";
        if (charge.Payments is not { Count: > 0 } || charge.Payments.Any(receivedPayment => receivedPayment is null
            || receivedPayment.TenantId != tenantId || receivedPayment.ChargeId != chargeId || receivedPayment.Id == Guid.Empty
            || receivedPayment.BankConnectionId == Guid.Empty || receivedPayment.PaymentMethodId == Guid.Empty
            || receivedPayment.ReceivedAt == default || !Money(receivedPayment.Amount))
            || charge.Payments.Select(receivedPayment => receivedPayment.Id).Distinct().Count() != charge.Payments.Count
            )
            return "SettlementPaymentMismatch";
        if (charge.Payments.Sum(receivedPayment => receivedPayment.Amount) < charge.Amount)
            return "InsufficientReceivedAmount";
        return null;
    }

    public static IReadOnlyList<ChargeSettlementAllocation> Allocate(ChargeSettlementSnapshot charge)
    {
        var remainingAmountByChargeItemId = charge.Items.OrderBy(chargeItem => chargeItem.Ordinal).ToDictionary(
            chargeItem => chargeItem.Id,
            chargeItem => chargeItem.Amount);
        var allocations = new List<ChargeSettlementAllocation>();
        var remainingChargeAmount = charge.Amount;
        foreach (var receivedPayment in charge.Payments
            .OrderBy(receivedPayment => receivedPayment.ReceivedAt)
            .ThenBy(receivedPayment => receivedPayment.Id))
        {
            var availablePaymentAmount = Math.Min(receivedPayment.Amount, remainingChargeAmount);
            foreach (var chargeItem in charge.Items.OrderBy(chargeItem => chargeItem.Ordinal))
            {
                if (availablePaymentAmount <= 0)
                    break;
                var remainingItemAmount = remainingAmountByChargeItemId[chargeItem.Id];
                if (remainingItemAmount <= 0)
                    continue;
                var allocatedAmount = Math.Min(availablePaymentAmount, remainingItemAmount);
                allocations.Add(new(receivedPayment, chargeItem, allocatedAmount));
                remainingAmountByChargeItemId[chargeItem.Id] -= allocatedAmount;
                availablePaymentAmount -= allocatedAmount;
                remainingChargeAmount -= allocatedAmount;
            }
            if (remainingChargeAmount == 0)
                break;
        }
        return allocations;
    }

    public static bool Matches(ChargeSettlementCheckpoint checkpoint, ChargeSettlementSnapshot charge)
    {
        if (checkpoint.SchemaVersion != 2 || checkpoint.Revision < 1 || checkpoint.ChargeId != charge.ChargeId
            || checkpoint.Payments is not { Count: > 0 } || checkpoint.Items is null || checkpoint.Items.Count == 0
            || checkpoint.Items.Any(row => row?.Data is null)
            || checkpoint.Items.Select(row => (row.Data.PaymentTransactionId, row.Data.ChargeItemId)).Distinct()
                .Count() != checkpoint.Items.Count)
            return false;

        // A checkpoint records execution history. Its amounts, dates, account mappings,
        // and provider identifiers must not become the source for a later request.
        // Only require that its stable payment/item identities still describe the
        // current allocation plan; the handler rebuilds request data from the snapshot.
        var currentAllocations = Allocate(charge);
        var currentAllocationKeys = currentAllocations
            .Select(allocation => (allocation.Payment.Id, allocation.Item.Id))
            .ToHashSet();
        var savedPaymentIds = checkpoint.Payments.Select(payment => payment.Id).ToHashSet();
        var currentPaymentIds = currentAllocations.Select(allocation => allocation.Payment.Id).ToHashSet();
        if (!savedPaymentIds.SetEquals(currentPaymentIds))
            return false;

        foreach (var checkpointItem in checkpoint.Items)
        {
            var checkpointData = checkpointItem.Data;
            if (!currentAllocationKeys.Contains((checkpointData.PaymentTransactionId, checkpointData.ChargeItemId))
                || checkpointData.TenantId != charge.TenantId
                || checkpointData.Identifier != Identifier(checkpointData.PaymentTransactionId, checkpointData.ChargeItemId)
                || checkpointItem.State is not ("NotSent" or "Sending" or "Completed" or "Rejected" or "Indeterminate")
                || checkpointItem.State == "Completed" && !IsValidExternalIdentifier(checkpointItem.ProviderReceiptId))
                return false;
        }

        return checkpoint.Items.Count == currentAllocationKeys.Count;
    }

    private static bool Money(decimal amount) => amount > 0 && decimal.Round(amount, 2) == amount;

    internal static bool IsValidExternalIdentifier(string? externalIdentifier) =>
        !string.IsNullOrWhiteSpace(externalIdentifier) && !externalIdentifier.Any(char.IsControl);
}
