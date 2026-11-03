using FluentValidation;

namespace YallaFinance.Application.Features.AccessProfiles.GetAccessProfile;

public sealed class GetAccessProfileQueryValidator : AbstractValidator<GetAccessProfileQuery>
{
    public GetAccessProfileQueryValidator()
    {
        // Missing profiles, including Guid.Empty, are reported by the service as not found.
    }
}
