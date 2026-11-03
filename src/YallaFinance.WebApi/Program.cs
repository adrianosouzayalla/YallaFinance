using Serilog;
using System.Text.Json.Serialization;
using YallaFinance.Application;
using YallaFinance.Infrastructure.Composition;
using YallaFinance.Application.Common.Context;
using YallaFinance.WebApi.Context;
using YallaFinance.WebApi.Payments;
using Microsoft.OpenApi;
using YallaFinance.WebApi.Swagger;
using YallaFinance.WebApi.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using YallaFinance.WebApi.Authorization;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    foreach (var key in new[] { "FinancialIntegrations:EvidenceDirectory" })
    {
        if (builder.Configuration[key] is { Length: > 0 } path)
            builder.Configuration[key] = Path.GetFullPath(path, builder.Environment.ContentRootPath);
    }

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddInfrastructureAuthentication(builder.Configuration);
    builder.Services.AddSingleton<IAuthorizationPolicyProvider, FunctionalityPolicyProvider>();
    builder.Services.AddScoped<IAuthorizationHandler, FunctionalityAuthorizationHandler>();
    builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, FunctionalityAuthorizationMiddlewareResultHandler>();
    // Existing Bearer remains available only on the integration controllers.
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = InteractiveSessionHandler.SchemeName;
        options.DefaultChallengeScheme = InteractiveSessionHandler.SchemeName;
    }).AddScheme<AuthenticationSchemeOptions, InteractiveSessionHandler>(InteractiveSessionHandler.SchemeName, _ => { });
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUserContext, HttpCurrentUserContext>();
    builder.Services.AddScoped<ICurrentTenantContext, HttpCurrentTenantContext>();
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(
            "FrontendLocal",
            policy =>
            {
                policy
                    .WithOrigins("http://localhost:3000", "http://127.0.0.1:3000", "http://192.168.15.3:3000", "http://192.168.15.5:3000")
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
    });
    builder.Services.AddHealthChecks();
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
        .AddMvcOptions(options => options.Filters.Add<ValidationExceptionFilter>());
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SchemaFilter<InboundCustomerSchemaFilter>();
        options.AddSecurityDefinition(
            "Bearer",
            new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Obtenha o token em /api/integrations/auth/token e informe somente o JWT."
            });
        options.OperationFilter<BearerAuthorizationOperationFilter>();
    });

    var app = builder.Build();

    app.UseSerilogRequestLogging(PaymentRequestLogPath.ConfigureRequestLogging);

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }
    app.UseCors("FrontendLocal");
    app.UseMiddleware<SessionFailureMiddleware>();
    app.UseAuthentication();
    app.UseAuthorization();
    app.Use(async (context, next) =>
    {
        await YallaFinance.WebApi.ExecutionActorBoundary.InitializeAsync(context);
        await next(context);
    });
    app.MapControllers();
    app.MapGet("/", () => Results.Ok(new
    {
        service = "YallaFinance WebApi",
        status = "running",
        health = "/health",
        swagger = "/swagger"
    })).ExcludeFromDescription();
    app.MapHealthChecks("/health");

    app.Run();
}
catch (Exception exception)
{
    Log.Fatal(exception, "A WebApi foi encerrada inesperadamente.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;

namespace YallaFinance.WebApi
{
    public static class ExecutionActorBoundary
    {
        public static async Task InitializeAsync(HttpContext context)
        {
            var action = context.GetEndpoint()?.Metadata
                .GetMetadata<Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor>();
            var technicalEntry = action is not null &&
                ((action.ControllerTypeInfo.AsType() == typeof(YallaFinance.WebApi.Controllers.PaymentsController)
                    && action.ActionName is nameof(YallaFinance.WebApi.Controllers.PaymentsController.GetCheckout)
                        or nameof(YallaFinance.WebApi.Controllers.PaymentsController.StartAttempt))
                 || (action.ControllerTypeInfo.AsType() == typeof(YallaFinance.WebApi.Controllers.IntegrationDebitsController)
                     && action.ActionName == nameof(YallaFinance.WebApi.Controllers.IntegrationDebitsController.Receive)));
            if (technicalEntry)
            {
                var identities = context.RequestServices.GetRequiredService<YallaFinance.Application.Repositories.Users.IUserIdentityRepository>();
                var systemId = await identities.FindOperationalUserIdAsync("system", context.RequestAborted)
                    ?? throw new InvalidOperationException("The operational SYSTEM identity is unavailable.");
                context.RequestServices.GetRequiredService<CurrentExecutionActorContext>().SetOperationalUser(systemId);
            }
        }
    }
}
