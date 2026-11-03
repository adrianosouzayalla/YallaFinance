using System.Text.Json;
using System.Text.Json.Nodes;
using YallaFinance.Application.Features.Jobs.Processing;
using YallaFinance.Application.Features.Jobs.Shared;
using YallaFinance.Application.Repositories.Jobs.Contracts;
using YallaFinance.Application.Repositories.Jobs;
using YallaFinance.Application;
namespace YallaFinance.Application.Features.Jobs.Shared.Monitoring;

public static class JobMonitoringPayload
{
    private static readonly HashSet<string> SensitiveFields = new(StringComparer.Ordinal)
    {
        "authorization", "apikey", "connectionstring", "cookie", "setcookie", "credentials", "headers",
        "cpf", "cnpj", "document", "documentnumber", "taxid", "email", "phone", "telefone",
        "address", "endereco", "name", "nome", "fullname", "birthdate", "datanascimento",
        "customer", "client", "cliente", "pixkey", "chavepix", "creditcard", "bankaccount",
        "stacktrace", "exception", "innerexception", "responsebody", "httpresponse", "rawresponse"
    };

    public static JsonElement? ForDisplay(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;
        try
        {
            var payloadNode = JsonNode.Parse(json);
            // TODO: adriano - Proteção genérica dos Jobs: mascara dados sensíveis somente para exibição; não criptografa nem altera o payload original armazenado e usado pelo Worker.
            Protect(payloadNode);
            // Unstructured text has no field names on which to apply the protection policy.
            if (payloadNode is JsonValue jsonValueNode && jsonValueNode.TryGetValue<string>(out _))
                return JsonSerializer.SerializeToElement("[Conteúdo textual protegido]");
            return JsonSerializer.SerializeToElement(payloadNode);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static void Protect(JsonNode? node)
    {
        if (node is JsonObject jsonObject)
        {
            foreach (var jsonProperty in jsonObject.ToArray())
            {
                var normalizedPropertyName = new string(jsonProperty.Key.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant)
                    .ToArray());
                if (SensitiveFields.Contains(normalizedPropertyName) || normalizedPropertyName.Contains("token") || normalizedPropertyName.Contains("password")
                    || normalizedPropertyName.Contains("senha") || normalizedPropertyName.Contains("secret") || normalizedPropertyName.Contains("credential")
                    || normalizedPropertyName.Contains("apikey") || normalizedPropertyName.Contains("connectionstring") || normalizedPropertyName.Contains("authorization"))
                    jsonObject[jsonProperty.Key] = "[Protegido]";
                else
                    Protect(jsonProperty.Value);
            }
        }
        else if (node is JsonArray jsonArray)
            foreach (var childNode in jsonArray)
                Protect(childNode);
    }
}
