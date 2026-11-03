using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
namespace YallaFinance.WebApi.Binding;

// Keeps strict endpoint-specific parsing while exposing the JSON body to API Explorer.
public sealed class InboundDebitBodyAttribute : ModelBinderAttribute
{
    public InboundDebitBodyAttribute() : base(typeof(InboundDebitPayloadModelBinder))
    {
        BindingSource = Microsoft.AspNetCore.Mvc.ModelBinding.BindingSource.Body;
    }
}
