namespace YallaFinance.Infrastructure.Providers.Nibo.Financial.Responses;

internal sealed record NiboCustomerResponse(
    string? Id,
    string? Name,
    NiboCustomerDocumentResponse? Document,
    bool? IsDeleted,
    bool? IsArchived,
    NiboCustomerCommunicationResponse? Communication = null,
    NiboCustomerAddressResponse? Address = null,
    string? Email = null,
    string? Phone = null)
{
    internal string? ContactEmail => NiboCustomerAddressResponse.Clean(Communication?.Email) ?? NiboCustomerAddressResponse.Clean(Email);
    internal string? ContactPhone => NiboCustomerAddressResponse.Clean(Communication?.Phone)
        ?? NiboCustomerAddressResponse.Clean(Communication?.CellPhone) ?? NiboCustomerAddressResponse.Clean(Phone);
}

internal sealed record NiboCustomerCommunicationResponse(string? Email, string? Phone, string? CellPhone);

internal sealed record NiboCustomerAddressResponse(string? Country, string? ZipCode, string? Line1,
    System.Text.Json.JsonElement Number, string? Line2, string? District, string? City, string? State)
{
    internal YallaFinance.Domain.Customers.CustomerAddressData? ToCustomerAddress()
    {
        var country = CountryCode(Country);
        var number = Number.ValueKind switch
        {
            System.Text.Json.JsonValueKind.String => Clean(Number.GetString()),
            System.Text.Json.JsonValueKind.Number => Number.GetRawText(),
            _ => null
        };
        var postalCode = Clean(ZipCode);
        if (country == "BR" && postalCode is not null)
            postalCode = postalCode.Replace("-", "").Replace(" ", "");
        var address = new YallaFinance.Domain.Customers.CustomerAddressData(country, postalCode,
            Clean(Line1), number, number is null ? null : false, Clean(Line2), Clean(District), Clean(City), Clean(State)?.ToUpperInvariant());
        if (address == new YallaFinance.Domain.Customers.CustomerAddressData()) return null;
        try
        {
            address.Validate();
        }
        catch (ArgumentException exception)
        {
            throw new System.Text.Json.JsonException("Invalid customer address returned by Nibo.", exception);
        }
        return address;
    }

    internal static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    internal static string? CountryCode(string? value)
    {
        var country = Clean(value);
        if (country is null) return null;
        if (country.Equals("Brasil", StringComparison.OrdinalIgnoreCase) || country.Equals("Brazil", StringComparison.OrdinalIgnoreCase)) return "BR";
        foreach (var culture in System.Globalization.CultureInfo.GetCultures(System.Globalization.CultureTypes.SpecificCultures))
        {
            var region = new System.Globalization.RegionInfo(culture.Name);
            if (new[] { region.TwoLetterISORegionName, region.ThreeLetterISORegionName, region.EnglishName, region.NativeName, region.DisplayName }
                .Contains(country, StringComparer.OrdinalIgnoreCase)) return region.TwoLetterISORegionName;
        }
        throw new System.Text.Json.JsonException("Unrecognized customer address country.");
    }
}
