using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using YallaFinance.Application.Features.Inbound.Debits.ReceiveDebit;

namespace YallaFinance.WebApi.Swagger;

// MVC reads validation attributes on record constructor parameters, not on their generated properties.
public sealed class InboundCustomerSchemaFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type != typeof(CustomerInboundData) && context.Type != typeof(CustomerInboundAddressData))
            return;
        if (schema is not OpenApiSchema objectSchema)
            return;
        objectSchema.Required ??= new HashSet<string>();
        foreach (var parameter in context.Type.GetConstructors().SelectMany(constructor => constructor.GetParameters())
                     .Where(parameter => parameter.IsDefined(typeof(RequiredAttribute), inherit: false)))
            objectSchema.Required.Add(JsonNamingPolicy.CamelCase.ConvertName(parameter.Name!));
    }
}
