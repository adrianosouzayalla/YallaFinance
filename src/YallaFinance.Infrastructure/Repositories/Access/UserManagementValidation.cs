using System.Net.Mail;
using System.Text.RegularExpressions;
using YallaFinance.Application.Common.Errors.Access;

namespace YallaFinance.Infrastructure.Repositories.Access;

public static class UserManagementValidation
{
    public static string Login(string? value)
    {
        var login = (value ?? "").Trim().ToLowerInvariant();
        if (login.Length is < 3 or > 100 || login.Any(char.IsWhiteSpace))
            Invalid("Login deve ter de 3 a 100 caracteres, sem espaços.");
        return login;
    }
    public static string Name(string? value, int max = 200)
    {
        var name = (value ?? "").Trim();
        if (name.Length < 1 || name.Length > max)
            Invalid($"Nome deve ter de 1 a {max} caracteres.");
        return name;
    }
    public static string Email(string? value)
    {
        var email = (value ?? "").Trim().ToLowerInvariant();
        if (email.Length > 254 || !MailAddress.TryCreate(email, out var address) || address.Address != email || email.Any(char.IsWhiteSpace))
            Invalid("Informe um e-mail válido.");
        return email;
    }
    public static string ProfileCode(string? value)
    {
        var code = (value ?? "").Trim().ToUpperInvariant();
        if (!Regex.IsMatch(code, @"\A[A-Z0-9_]{2,50}\z"))
            Invalid("Código deve ter de 2 a 50 letras, números ou underscore.");
        return code;
    }
    public static void Password(string? value)
    {
        if (value is null || value.Length is < 12 or > 128 || string.IsNullOrWhiteSpace(value))
            Invalid("Senha deve ter de 12 a 128 caracteres.");
    }
    public static void Paging(int page, int size, string sort, string direction, params string[] allowed)
    {
        if (page < 1 || page > 1000000 || size is < 1 or > 100 || !allowed.Contains(sort) || direction is not ("asc" or "desc"))
            Invalid("Filtros, paginação ou ordenação inválidos.");
    }
    public static void Invalid(string message) => throw new UserManagementException("INVALID_USER_INPUT", message);
}
