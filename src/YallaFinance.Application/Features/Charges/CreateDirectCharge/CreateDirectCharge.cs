using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using MediatR;
using YallaFinance.Application.Common.Context;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Features.Charges.CreateCharge;
using YallaFinance.Application.Features.Charges.IntegrateCharge;
using YallaFinance.Application.Repositories.Charges;
using YallaFinance.Application.Services.Charges;
using YallaFinance.Domain.Charges;
using YallaFinance.Domain.Customers;

namespace YallaFinance.Application.Features.Charges.CreateDirectCharge;

public sealed record DirectChargeItem(string CategoryName, DateOnly OccurrenceDate,
    [property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)] decimal Amount,
    string DescriptionDetail, string CategoryId = "");

public sealed record CreateDirectChargeCommand(string CustomerDocument, string Contract, string Plate,
    string CostCenterName, DateOnly DueDate, string? CustomerNote, IReadOnlyList<DirectChargeItem> Items,
    [property: JsonIgnore] string? IdempotencyKey = null, string CostCenterReference = "",
    string CustomerName = "", string CustomerDocumentType = "", string? CustomerEmail = null,
    string? CustomerPhone = null, string? ContractProduct = null, string? ContractFleet = null)
    : IRequest<NotificationResult<ChargeDetailsResult>>;

public sealed record GetDirectChargeStatusQuery(Guid ChargeId) : IRequest<NotificationResult<ChargeDetailsResult>>;

public sealed class GetDirectChargeStatusQueryValidator : AbstractValidator<GetDirectChargeStatusQuery>
{
    public GetDirectChargeStatusQueryValidator()
        => RuleFor(query => query.ChargeId).NotEmpty().WithMessage("Informe uma cobrança válida.");
}

public sealed class GetDirectChargeStatusQueryHandler(
    ICurrentTenantContext currentTenant, IChargeRepository charges, ChargeProcessingQueries processing)
    : IRequestHandler<GetDirectChargeStatusQuery, NotificationResult<ChargeDetailsResult>>
{
    public async Task<NotificationResult<ChargeDetailsResult>> Handle(GetDirectChargeStatusQuery request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!Guid.TryParse(currentTenant.TenantId, out var tenantId) || tenantId == Guid.Empty)
            return NotificationResult<ChargeDetailsResult>.Failure(new NotificationError(ErrorCodes.Unauthorized, "Sessão sem empresa ativa válida."));
        if (request.ChargeId == Guid.Empty)
            return NotificationResult<ChargeDetailsResult>.Failure(new NotificationError(ErrorCodes.Validation, "Informe uma cobrança válida."));
        var charge = await charges.GetAsync(tenantId.ToString(), request.ChargeId.ToString(), cancellationToken);
        if (charge is null || charge.TenantId != tenantId.ToString() || charge.Source != ChargeSource.Direct)
            return NotificationResult<ChargeDetailsResult>.Failure(new NotificationError(ErrorCodes.NotFound, "Cobrança direta não encontrada."));
        var result = await processing.EnrichAsync(charge, cancellationToken);
        return NotificationResult<ChargeDetailsResult>.Success(result.Processing is null
            ? result with { Processing = new(null, "NotScheduled", ErrorCode: "ChargeSchedulingFailed") } : result);
    }
}

public sealed class CreateDirectChargeCommandValidator : AbstractValidator<CreateDirectChargeCommand>
{
    public const decimal MaximumAmount = 9_999_999_999_999_999.99m;
    public CreateDirectChargeCommandValidator()
    {
        RuleFor(command => command.CustomerDocument).Must((command, document) =>
            CustomerDocument.HasValidFormat(document, command.CustomerDocumentType)
            && document == CustomerDocument.Normalize(document))
            .WithMessage("Informe um CPF ou CNPJ sem máscara, preservando as letras do CNPJ em maiúsculas.");
        RuleFor(command => command.CustomerName).NotEmpty().WithMessage("Consulte o cliente e informe seu nome.")
            .Must(name => name is null || !name.Trim().Any(char.IsControl))
            .WithMessage("O nome do cliente não pode conter quebras de linha ou caracteres de controle.");
        RuleFor(command => command.CustomerEmail).Must(value =>
        {
            if (string.IsNullOrWhiteSpace(value)) return true;
            var email = value.Trim();
            return System.Net.Mail.MailAddress.TryCreate(email, out var address) && address.Address == email;
        }).WithMessage("Informe um e-mail válido para o cliente ou deixe o campo em branco.");
        RuleFor(command => command.CustomerPhone).Must(value => string.IsNullOrWhiteSpace(value)
            || value.Trim().All(character => character is >= '0' and <= '9' or '+' or '-' or '(' or ')' or ' ')
                && value.Count(character => character is >= '0' and <= '9') is >= 8 and <= 15)
            .WithMessage("Informe um telefone válido, com 8 a 15 dígitos, ou deixe o campo em branco.");
        RuleFor(command => command.CustomerDocumentType).Must((command, type) =>
            type == (command.CustomerDocument?.Length == 11 ? "CPF" : command.CustomerDocument?.Length == 14 ? "CNPJ" : ""))
            .WithMessage("Confira o tipo de documento do cliente.");
        RuleFor(command => command.Contract).NotEmpty().WithMessage("Informe o contrato.")
            .Must(contract => contract is null || !contract.Trim().Any(char.IsControl))
            .WithMessage("O contrato não pode conter quebras de linha ou caracteres de controle.");
        RuleFor(command => command.Plate).NotEmpty().Matches("^[A-Z]{3}[0-9][A-Z0-9][0-9]{2}$").WithMessage("Informe uma placa válida.");
        RuleFor(command => command.CostCenterName).NotEmpty().WithMessage("Informe o centro de custo.");
        RuleFor(command => command.CostCenterReference).NotEmpty().WithMessage("Selecione o centro de custo.");
        RuleFor(command => command.DueDate).NotEqual(default(DateOnly)).WithMessage("Informe um vencimento válido.");
        RuleFor(command => command.IdempotencyKey).Must(key => Guid.TryParse(key, out var attemptId) && attemptId != Guid.Empty)
            .WithMessage("Informe uma chave de tentativa válida.");
        RuleFor(command => command.Items).NotEmpty().WithMessage("Adicione pelo menos um débito.");
        RuleFor(command => command.Items).Must(items =>
        {
            if (items is null) return false;
            decimal sum = 0;
            foreach (var item in items)
            {
                if (item is null || item.Amount <= 0 || item.Amount > MaximumAmount - sum) return false;
                sum += item.Amount;
            }
            return sum > 0;
        }).WithMessage("O total da cobrança excede o limite permitido ou contém valores inválidos.");
        RuleForEach(command => command.Items).NotNull().WithMessage("Informe um débito válido.").ChildRules(item =>
        {
            item.RuleFor(debt => debt.CategoryName).NotEmpty().WithMessage("Informe a categoria.");
            item.RuleFor(debt => debt.CategoryId).Must(value => Guid.TryParse(value, out var id) && id != Guid.Empty)
                .WithMessage("Selecione uma categoria Yalla válida.");
            item.RuleFor(debt => debt.OccurrenceDate).NotEqual(default(DateOnly)).WithMessage("Informe uma ocorrência válida.");
            item.RuleFor(debt => debt.Amount).GreaterThan(0).LessThanOrEqualTo(MaximumAmount)
                .Must(amount => decimal.Round(amount, 2) == amount).WithMessage("Informe um valor válido com até duas casas decimais.");
            item.RuleFor(debt => debt.DescriptionDetail).NotEmpty().WithMessage("Informe a descrição.")
                .Must(description => description is null || !description.Trim().Any(char.IsControl))
                .WithMessage("A descrição não pode conter quebras de linha ou caracteres de controle.");
        });
    }
}

public sealed class CreateDirectChargeCommandHandler(ICurrentTenantContext currentTenant, IChargeCreationService charges)
    : IRequestHandler<CreateDirectChargeCommand, NotificationResult<ChargeDetailsResult>>
{
    public Task<NotificationResult<ChargeDetailsResult>> Handle(CreateDirectChargeCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return !Guid.TryParse(currentTenant.TenantId, out var tenantId) || tenantId == Guid.Empty
            ? Task.FromResult(NotificationResult<ChargeDetailsResult>.Failure(new NotificationError(ErrorCodes.Unauthorized, "Sessão sem empresa ativa válida.")))
            : charges.CreateDirectAsync(tenantId.ToString(), request, cancellationToken);
    }
}

public static class DirectChargeFingerprint
{
    public static string Hash(Guid tenantId, CreateDirectChargeCommand request)
    {
        var normalized = new
        {
            TenantId = tenantId, Source = "Direct",
            request.CustomerDocument, CustomerName = request.CustomerName.Trim(), request.CustomerDocumentType,
            Email = request.CustomerEmail?.Trim() ?? "", Phone = request.CustomerPhone?.Trim() ?? "",
            Contract = request.Contract.Trim(), request.Plate,
            Product = request.ContractProduct?.Trim() ?? "", Fleet = request.ContractFleet?.Trim() ?? "",
            request.CostCenterReference, CostCenterName = request.CostCenterName.Trim(), request.DueDate,
            Note = request.CustomerNote ?? "",
            Items = request.Items.Select(item => new { item.CategoryId, CategoryName = item.CategoryName.Trim(),
                item.OccurrenceDate, item.Amount, Detail = item.DescriptionDetail.Trim() }).ToArray()
        };
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(normalized))));
    }
}
