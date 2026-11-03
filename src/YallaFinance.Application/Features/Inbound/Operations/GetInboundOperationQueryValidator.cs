using FluentValidation;

namespace YallaFinance.Application.Features.Inbound.Operations;

public sealed class GetInboundOperationQueryValidator : AbstractValidator<GetInboundOperationQuery>
{
    public GetInboundOperationQueryValidator() =>
        RuleFor(query => query.OperationId).NotEmpty()
            .WithMessage("Identificador de operação inválido.");
}
