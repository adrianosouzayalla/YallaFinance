using MediatR;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Repositories.Categories.Contracts;
using YallaFinance.Application;


namespace YallaFinance.Application.Features.Categories.ListCategories;

public sealed record ListCategoriesQuery : IRequest<NotificationResult<IReadOnlyList<CategoryResponse>>>;
