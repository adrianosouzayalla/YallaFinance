namespace YallaFinance.Infrastructure.Repositories.Debits;

internal sealed class ManualDebtRequestConcurrencyException(Exception inner) : Exception(
    "A manual debt request with this identity already exists.",
    inner);
