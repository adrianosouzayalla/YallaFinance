namespace YallaFinance.Infrastructure.Providers.Nibo.Financial.Requests;

internal sealed record CreateNiboCustomerRequest(
    string Name,
    CreateNiboCustomerDocumentRequest? Document,
    CreateNiboCustomerCommunicationRequest? Communication,
    CreateNiboCustomerAddressRequest? Address = null);

internal sealed record CreateNiboCustomerAddressRequest(string? Line1, string? Line2, int? Number,
    string? District, string? City, string? State, string? ZipCode, string? Country)
{
    internal static CreateNiboCustomerAddressRequest? FromCustomerAddress(YallaFinance.Domain.Customers.CustomerAddressData? address)
    {
        if (address is null) return null;
        address.Validate();
        int? number = null;
        if (address.Number is not null)
        {
            if (!int.TryParse(address.Number, System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture, out var parsedNumber))
                throw new ArgumentException("Nibo requires an integer house number.");
            number = parsedNumber;
        }
        var country = address.CountryCode is null ? null : address.CountryCode == "BR" ? "Brasil"
            : new System.Globalization.RegionInfo(address.CountryCode).EnglishName;
        return new(address.Street, address.Complement, number, address.District, address.City,
            address.StateCode, address.PostalCode, country);
    }
}
