using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Net.Http.Headers;
using YallaFinance.Application.Features.Inbound.Debits.ReceiveDebit;
namespace YallaFinance.WebApi.Binding;

public sealed class InboundDebitPayloadModelBinder : IModelBinder
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var request = bindingContext.HttpContext.Request;
        if (!MediaTypeHeaderValue.TryParse(request.ContentType, out var contentType)
            || !(contentType.MediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase)
                || contentType.MediaType.Value?.EndsWith("+json", StringComparison.OrdinalIgnoreCase) == true))
        {
            bindingContext.ModelState.TryAddModelError(
                bindingContext.ModelName,
                "Envie o contrato em application/json.");
            return;
        }
        try
        {
            var payload = await JsonSerializer.DeserializeAsync<ReceiveDebitPayload>(
                request.Body,
                Options,
                bindingContext.HttpContext.RequestAborted);
            if (payload is null)
            {
                bindingContext.ModelState.TryAddModelError(
                    bindingContext.ModelName,
                    "Contrato de entrada obrigatório.");
                return;
            }
            bindingContext.Result = ModelBindingResult.Success(payload);
        }
        catch (JsonException)
        {
            bindingContext.ModelState.TryAddModelError(
                bindingContext.ModelName,
                "JSON inválido ou com campos não previstos no contrato.");
        }
    }
}
