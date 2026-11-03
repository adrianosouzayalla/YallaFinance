using FluentValidation;
using System.Net.Mail;
using YallaFinance.Domain.Customers;
using YallaFinance.Application;
namespace YallaFinance.Application.Features.Inbound.Debits.ReceiveDebit;

public sealed class ReceiveDebitPayloadValidator : AbstractValidator<ReceiveDebitPayload>
{
    public ReceiveDebitPayloadValidator()
    {
        ConfigureDebitAndIdentityRules();
        ConfigureCompleteCustomerRules();
        // Only the job executor selects this ruleset. HTTP always applies the complete default contract.
        RuleSet("StoredJob", () =>
        {
            ConfigureDebitAndIdentityRules();
            When(payload => payload.Customer?.HasExtendedData() == true, ConfigureCompleteCustomerRules);
        });
    }

    private void ConfigureDebitAndIdentityRules()
    {
        RuleFor(receiveDebitPayload => receiveDebitPayload.Debit).NotNull();
        RuleFor(receiveDebitPayload => receiveDebitPayload.Customer).NotNull();
        When(
            receiveDebitPayload => receiveDebitPayload.Debit is not null,
            () =>
            {
                RuleFor(receiveDebitPayload => receiveDebitPayload.Debit.ExternalId).NotEmpty();
                RuleFor(receiveDebitPayload => receiveDebitPayload.Debit.Description).NotEmpty();
                RuleFor(receiveDebitPayload => receiveDebitPayload.Debit.CategoryCode).NotEmpty();
                RuleFor(receiveDebitPayload => receiveDebitPayload.Debit.Amount).GreaterThan(0).LessThanOrEqualTo(9_999_999_999_999_999.99m).Must(amount => decimal.Round(
                    amount,
                    2) == amount);
                RuleFor(receiveDebitPayload => receiveDebitPayload.Debit.OccurrenceDate).NotEqual(default(DateOnly));
                RuleFor(receiveDebitPayload => receiveDebitPayload.Debit.DueDate).Must(dueDate => dueDate is null || dueDate != default(DateOnly));
                RuleFor(receiveDebitPayload => receiveDebitPayload.Debit.Contract).NotNull();
                When(receiveDebitPayload => receiveDebitPayload.Debit.Contract is not null, () =>
                {
                    RuleFor(receiveDebitPayload => receiveDebitPayload.Debit.Contract.ExternalId).NotEmpty();
                    RuleFor(receiveDebitPayload => receiveDebitPayload.Debit.Contract.Number).NotEmpty();
                });
            });
        When(
            receiveDebitPayload => receiveDebitPayload.Customer is not null,
            () =>
            {
                RuleFor(receiveDebitPayload => receiveDebitPayload.Customer.ExternalId).NotEmpty();
                RuleFor(receiveDebitPayload => receiveDebitPayload.Customer.Name).NotEmpty();
                RuleFor(receiveDebitPayload => receiveDebitPayload.Customer.DocumentType).Must(documentType => documentType is "CPF" or "CNPJ");
                RuleFor(receiveDebitPayload => receiveDebitPayload.Customer).Must(customer =>
                    CustomerDocument.HasValidFormat(customer.Document, customer.DocumentType));
            });
    }

    private void ConfigureCompleteCustomerRules()
    {
        When(payload => payload.Customer is not null, () =>
        {
            RuleFor(payload => payload.Customer.Name).Must(name => name is null || !name.Any(char.IsControl))
                .WithMessage("O nome do cliente não pode conter caracteres de controle.");
            RuleFor(payload => payload.Customer.DocumentIssuingCountry).Equal("BR")
                .WithMessage("Informe BR como país emissor do CPF ou CNPJ.");
            RuleFor(payload => payload.Customer.Email).Cascade(CascadeMode.Stop).NotEmpty()
                .WithMessage("Informe o e-mail do cliente.")
                .Must(email => email is { Length: <= 254 } && !email.Any(char.IsWhiteSpace)
                    && MailAddress.TryCreate(email, out var parsed) && parsed.Address == email)
                .WithMessage("Informe um e-mail válido para o cliente.");
            RuleFor(payload => payload.Customer.PhoneCountryCallingCode).Cascade(CascadeMode.Stop).NotEmpty()
                .WithMessage("Informe o DDI do telefone do cliente.").Matches("^[1-9][0-9]{0,2}$")
                .WithMessage("Informe um DDI com um a três dígitos, sem zero inicial.");
            RuleFor(payload => payload.Customer.PhoneNationalNumber).Cascade(CascadeMode.Stop).NotEmpty()
                .WithMessage("Informe DDD e número do telefone do cliente.").Matches("^[0-9]{1,14}$")
                .WithMessage("Informe o número nacional do telefone somente com dígitos.");
            RuleFor(payload => payload.Customer).Must(customer =>
                    (customer.PhoneCountryCallingCode?.Length ?? 0) + (customer.PhoneNationalNumber?.Length ?? 0) <= 15)
                .OverridePropertyName("Customer.PhoneNationalNumber")
                .WithMessage("DDI e número nacional devem ter até quinze dígitos no total.");
            RuleFor(payload => payload.Customer.Phone).Must((payload, phone) => phone is null
                    || phone == $"+{payload.Customer.PhoneCountryCallingCode}{payload.Customer.PhoneNationalNumber}")
                .WithMessage("Se informar phone, use a representação internacional correspondente ao DDI e número nacional.");
            RuleFor(payload => payload.Customer.Address).NotNull().WithMessage("Informe o endereço do cliente.");
            When(payload => payload.Customer.Address is not null, () =>
            {
                RuleFor(payload => payload.Customer.Address!).SetValidator(new InboundCustomerAddressValidator(), "default");
            });
        });
    }
}

internal sealed class InboundCustomerAddressValidator : AbstractValidator<CustomerInboundAddressData>
{
    public InboundCustomerAddressValidator()
    {
        RuleFor(address => address.CountryCode).Equal("BR").WithMessage("Informe BR como país do endereço nesta versão do contrato.");
        RuleFor(address => address.PostalCode).Cascade(CascadeMode.Stop).NotEmpty().WithMessage("Informe o CEP.")
            .Matches("^[0-9]{8}$").WithMessage("Informe o CEP com oito dígitos.");
        RuleFor(address => address.Street).NotEmpty().WithMessage("Informe o logradouro.");
        RuleFor(address => address.District).NotEmpty().WithMessage("Informe o bairro.");
        RuleFor(address => address.City).NotEmpty().WithMessage("Informe a cidade.");
        RuleFor(address => address.StateCode).NotEmpty().WithMessage("Informe a UF.");
        RuleFor(address => address.WithoutNumber).NotNull().WithMessage("Informe se o imóvel é sem número.");
        When(address => address.WithoutNumber == false, () =>
            RuleFor(address => address.Number).NotEmpty().WithMessage("Informe o número do endereço ou indique imóvel sem número."));
        RuleFor(address => address).Custom((address, context) =>
        {
            try { address.ToCanonicalData().Validate(); }
            catch (ArgumentException) { context.AddFailure("Confira a UF, os campos do endereço e a indicação de imóvel sem número."); }
        });
    }
}
