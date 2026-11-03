using YallaFinance.Application;
namespace YallaFinance.Application.Services.AccessProfiles.Contracts;

public sealed record ProfilePage(IReadOnlyList<ProfileDetail> Items, int TotalCount, int Page, int PageSize);
