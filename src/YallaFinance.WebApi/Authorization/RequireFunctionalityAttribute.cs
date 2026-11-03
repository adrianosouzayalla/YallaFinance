using Microsoft.AspNetCore.Authorization;

namespace YallaFinance.WebApi.Authorization;

public sealed class RequireFunctionalityAttribute : AuthorizeAttribute
{
    public RequireFunctionalityAttribute(params string[] codes)
    {
        ArgumentNullException.ThrowIfNull(codes);
        if (codes.Length == 0) throw new ArgumentException("Ao menos um código de funcionalidade é obrigatório.", nameof(codes));
        Policy = codes.Length == 1
            ? $"Functionality:{codes[0]}"
            : $"FunctionalityAnyOf:{string.Join('|', codes)}";
    }
}
