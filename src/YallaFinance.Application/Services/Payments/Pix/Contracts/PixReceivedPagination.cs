using YallaFinance.Application;
namespace YallaFinance.Application.Services.Payments.Pix.Contracts;

public sealed record PixReceivedPagination(int Page, int ItemsPerPage, int TotalPages, int TotalItems);
