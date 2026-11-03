using YallaFinance.Application.Features.Inbound.Debits.ReceiveDebit;
using YallaFinance.Infrastructure.Database.Generated.Entities;
using YallaFinance.Domain.Customers;
using YallaFinance.Application.Common.Results;
namespace YallaFinance.Infrastructure.Repositories.Debits;

public static class ReceiveDebitPersistenceRules
{
    public static NotificationResult<bool> CompleteMissingCustomerData(Customer customer, CustomerInboundData input)
    {
        var name = ExistingOrIncoming(customer.Name, input.Name);
        var email = ExistingOrIncoming(customer.Email, input.Email);
        var country = ExistingOrIncoming(customer.DocumentIssuingCountry, input.DocumentIssuingCountry);
        var phone = customer.Phone;
        var callingCode = customer.PhoneCountryCallingCode;
        var nationalNumber = customer.PhoneNationalNumber;
        var existingAddress = customer.CustomerAddress;
        var previousAddress = existingAddress is null ? null : new CustomerAddressData(existingAddress.CountryCode,
            existingAddress.PostalCode, existingAddress.Street, existingAddress.Number, existingAddress.WithoutNumber,
            existingAddress.Complement, existingAddress.District, existingAddress.City, existingAddress.StateCode);
        CustomerAddressData? address = null;
        try
        {
            if (input.PhoneCountryCallingCode is not null && input.PhoneNationalNumber is not null)
            {
                var proposedCode = ExistingOrIncoming(callingCode, input.PhoneCountryCallingCode)!;
                var proposedNational = ExistingOrIncoming(nationalNumber, input.PhoneNationalNumber)!;
                var international = new CustomerPhoneData(proposedCode, proposedNational).ToInternationalNumber();
                // A populated legacy phone must not be rewritten or assigned contradictory components.
                if (string.IsNullOrWhiteSpace(phone) || phone == international)
                {
                    phone = string.IsNullOrWhiteSpace(phone) ? international : phone;
                    callingCode = proposedCode;
                    nationalNumber = proposedNational;
                }
            }
            else
                phone = ExistingOrIncoming(phone, input.Phone);

            if (input.Address is { } receivedAddress)
            {
                var numberKnown = previousAddress?.WithoutNumber is not null || !string.IsNullOrWhiteSpace(previousAddress?.Number);
                address = new CustomerAddressData(
                    ExistingOrIncoming(previousAddress?.CountryCode, receivedAddress.CountryCode),
                    ExistingOrIncoming(previousAddress?.PostalCode, receivedAddress.PostalCode),
                    ExistingOrIncoming(previousAddress?.Street, receivedAddress.Street),
                    numberKnown ? previousAddress?.Number : receivedAddress.Number?.Trim(),
                    numberKnown ? previousAddress!.WithoutNumber ?? false : receivedAddress.WithoutNumber,
                    ExistingOrIncoming(previousAddress?.Complement, receivedAddress.Complement),
                    ExistingOrIncoming(previousAddress?.District, receivedAddress.District),
                    ExistingOrIncoming(previousAddress?.City, receivedAddress.City),
                    ExistingOrIncoming(previousAddress?.StateCode, receivedAddress.StateCode));
                address.Validate();
            }
        }
        catch (ArgumentException)
        {
            return NotificationResult<bool>.Failure(new NotificationError("InboundCustomerConflict",
                "Os dados recebidos não permitem completar o cadastro preservando os valores existentes."));
        }

        var changed = customer.Name != name || customer.Email != email || customer.DocumentIssuingCountry != country
            || customer.Phone != phone || customer.PhoneCountryCallingCode != callingCode || customer.PhoneNationalNumber != nationalNumber
            || address is not null && address != previousAddress;
        customer.Name = name!;
        customer.Email = email;
        customer.DocumentIssuingCountry = country;
        customer.Phone = phone;
        customer.PhoneCountryCallingCode = callingCode;
        customer.PhoneNationalNumber = nationalNumber;
        if (address is not null && address != previousAddress)
        {
            var target = customer.CustomerAddress ??= new CustomerAddress
            {
                Id = Guid.NewGuid(), TenantId = customer.TenantId, CustomerId = customer.Id, Customer = customer
            };
            target.CountryCode = address.CountryCode;
            target.PostalCode = address.PostalCode;
            target.Street = address.Street;
            target.Number = address.Number;
            target.WithoutNumber = address.WithoutNumber;
            target.Complement = address.Complement;
            target.District = address.District;
            target.City = address.City;
            target.StateCode = address.StateCode;
        }
        return NotificationResult<bool>.Success(changed);
    }

    private static string? ExistingOrIncoming(string? existing, string? incoming) =>
        !string.IsNullOrWhiteSpace(existing) ? existing : string.IsNullOrWhiteSpace(incoming) ? existing : incoming.Trim();

    public static bool CustomerMatches(Customer customer, CustomerInboundData input) =>
        customer.Active && customer.NormalizedDocument == CustomerDocument.Normalize(input.Document)
            && customer.DocumentType == input.DocumentType;

    public static bool ContractMatches(Contract contract, Guid customerId, ContractInboundData input) =>
        contract.Active && contract.CustomerId == customerId && contract.Number == input.Number;
}
