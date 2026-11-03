namespace YallaFinance.Domain.Customers;

public sealed record CustomerPhoneData(string CountryCallingCode, string NationalNumber)
{
    public string ToInternationalNumber()
    {
        if (CountryCallingCode is not { Length: >= 1 and <= 3 } || CountryCallingCode[0] == '0'
            || !CountryCallingCode.All(char.IsAsciiDigit) || string.IsNullOrEmpty(NationalNumber)
            || !NationalNumber.All(char.IsAsciiDigit) || CountryCallingCode.Length + NationalNumber.Length > 15)
            throw new ArgumentException("Informe DDI e número nacional válidos, com até 15 dígitos no total.");
        return $"+{CountryCallingCode}{NationalNumber}";
    }
}

public sealed record CustomerAddressData(string? CountryCode = null, string? PostalCode = null,
    string? Street = null, string? Number = null, bool? WithoutNumber = null, string? Complement = null,
    string? District = null, string? City = null, string? StateCode = null)
{
    public void Validate()
    {
        string?[] fields = [CountryCode, PostalCode, Street, Number, Complement, District, City, StateCode];
        if (fields.Any(value => value is not null && (string.IsNullOrWhiteSpace(value) || value.Any(char.IsControl)))
            || fields.All(value => value is null) && WithoutNumber is null)
            throw new ArgumentException("Informe ao menos um dado válido do endereço; use nulo para campos ausentes.");
        if (CountryCode is not null && !IsCountryCode(CountryCode))
            throw new ArgumentException("Informe o país do endereço com duas letras maiúsculas.");
        if (WithoutNumber == false ? Number is null : Number is not null)
            throw new ArgumentException("Confira o número e a indicação de imóvel sem número.");
        if (CountryCode == "BR" && PostalCode is not null
            && (PostalCode.Length != 8 || !PostalCode.All(char.IsAsciiDigit)))
            throw new ArgumentException("Informe um CEP com oito dígitos.");
        if (StateCode is not null && (StateCode.Length != 2 || !StateCode.All(char.IsAsciiLetterUpper)
            || CountryCode == "BR" && !BrazilianStates.Contains(StateCode)))
            throw new ArgumentException("Informe uma UF válida para o endereço.");
    }

    public static bool IsCountryCode(string value) => value.Length == 2 && value.All(char.IsAsciiLetterUpper);

    private static readonly HashSet<string> BrazilianStates =
        ["AC", "AL", "AP", "AM", "BA", "CE", "DF", "ES", "GO", "MA", "MT", "MS", "MG", "PA", "PB", "PR", "PE", "PI", "RJ", "RN", "RS", "RO", "RR", "SC", "SP", "SE", "TO"];
}
