using FluentValidation;

namespace YallaFinance.Application.Features.Users.GetUser;

public sealed class GetUserQueryValidator : AbstractValidator<GetUserQuery>
{
    public GetUserQueryValidator()
    {
        // Missing users, including Guid.Empty, are reported by the service as not found.
    }
}
