using YallaFinance.Application;
namespace YallaFinance.Application.Repositories.Charges.Contracts;

public sealed record ChargeItemDebtResponse(
    string Contract,
    string? Plate,
    string? Product,
    string? Fleet,
    DateOnly OccurrenceDate,
    DateOnly? DueDate,
    string Description,
    string Origin);
