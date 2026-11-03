using System.Data;
using System.Data.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using YallaFinance.Application.Common.Errors.Access;

namespace YallaFinance.WebApi.Authentication;
public sealed class UserManagementExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var result = context.Exception switch
        {
            UserManagementException e => new ObjectResult(new { code = e.Code, detail = e.Message }) { StatusCode = e.Status },
            DBConcurrencyException => new ObjectResult(new { code = "USER_CONFLICT", detail = "Registro já utilizado. Atualize os dados e tente novamente." }) { StatusCode = 409 },
            DbUpdateException e when e.InnerException is Npgsql.PostgresException { SqlState: "23505" } =>
                new ObjectResult(new { code = "USER_CONFLICT", detail = "Login, e-mail ou código já utilizado." }) { StatusCode = 409 },
            DbException or DbUpdateException or TimeoutException => new ObjectResult(new { code = "DIRECTORY_UNAVAILABLE", detail = "Não foi possível salvar ou consultar os cadastros. Tente novamente." }) { StatusCode = 503 },
            _ => null
        };
        if (result is null) return;
        context.HttpContext.Response.Headers.CacheControl = "private, no-store";
        context.Result = result; context.ExceptionHandled = true;
    }
}
