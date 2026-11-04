using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using YallaFinance.Application.Common.Transactions;
using YallaFinance.Application.Features.Payments.GeneratePaymentRequests;
using YallaFinance.Application.Repositories.Categories;
using YallaFinance.Application.Repositories.Charges;
using YallaFinance.Application.Repositories.Connections;
using YallaFinance.Application.Repositories.CustomerContracts;
using YallaFinance.Application.Repositories.Customers;
using YallaFinance.Application.Repositories.Debits;
using YallaFinance.Application.Repositories.Fleet;
using YallaFinance.Application.Repositories.IntegrationOperations;
using YallaFinance.Application.Repositories.Jobs;
using YallaFinance.Application.Repositories.PaymentMethods;
using YallaFinance.Application.Repositories.Payments;
using YallaFinance.Application.Repositories.Sessions;
using YallaFinance.Application.Repositories.Settings;
using YallaFinance.Application.Services.Payments.Pix;
using YallaFinance.Application.Services.Payments.Boleto;
using YallaFinance.Application.Services.Charges;
using YallaFinance.Application.Services.Authentication;
using YallaFinance.Application.Services.Payments;
using YallaFinance.Application.Services.Financial;
using YallaFinance.Infrastructure.Database.Generated;
using YallaFinance.Infrastructure.Database.Transactions;
using YallaFinance.Infrastructure.Services.Payments.Pix;
using YallaFinance.Infrastructure.Services.Payments.Boleto;
using YallaFinance.Infrastructure.Providers;
using YallaFinance.Infrastructure.Services.Financial;
using YallaFinance.Infrastructure.Common.Errors;
using YallaFinance.Infrastructure.Mockup;
using YallaFinance.Infrastructure.Mockup.Repositories;
using YallaFinance.Infrastructure.Mockup.Services;
using YallaFinance.Infrastructure.Repositories.Access;
using YallaFinance.Infrastructure.Repositories.Categories;
using YallaFinance.Infrastructure.Repositories.Charges;
using YallaFinance.Infrastructure.Repositories.Connections;
using YallaFinance.Infrastructure.Repositories.Customers;
using YallaFinance.Infrastructure.Repositories.CustomerContracts;
using YallaFinance.Infrastructure.Repositories.Debits;
using YallaFinance.Infrastructure.Repositories.IntegrationOperations;
using YallaFinance.Infrastructure.Repositories.Jobs;
using YallaFinance.Infrastructure.Repositories.PaymentMethods;
using YallaFinance.Infrastructure.Repositories.Payments;
using YallaFinance.Infrastructure.Services.Payments;
using YallaFinance.Infrastructure.Repositories.Settings;

namespace YallaFinance.Infrastructure.Composition;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("YallaFinance");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContext<YallaFinanceDbContext>((provider, options) =>
                options.UseNpgsql(connectionString)
                    .UseSnakeCaseNamingConvention()
                    .AddInterceptors(provider.GetRequiredService<YallaFinance.Infrastructure.Database.Auditing.AuditSaveChangesInterceptor>()));
        }

        //TODO: Adriano Remover a carga de dados mestres por JSON e concluir os repositórios de tenant/frota no banco; não ampliar o mecanismo de dados isolados por arquivo.
        var dataDirectory = configuration["Mockup:DataDirectory"]
            ?? Path.Combine(AppContext.BaseDirectory, "Mockup", "Data");
        var seed = JsonMockDataLoader.LoadAsync(dataDirectory).GetAwaiter().GetResult();
        services.AddSingleton(seed);
        services.AddScoped<PostgresDebtRepository>();
        services.AddScoped<IDebtRepository>(provider => provider.GetRequiredService<PostgresDebtRepository>());
        services.AddScoped<YallaFinance.Application.Repositories.Connections.ICompanyConnectionRepository, YallaFinance.Infrastructure.Repositories.Connections.PostgresCompanyConnectionRepository>();
        services.AddScoped<PostgresCustomerRepository>();
        services.AddScoped<PostgresContractRepository>();
        services.AddScoped<IPaymentRepository, PostgresPaymentRepository>();
        services.AddScoped<YallaFinance.Application.Services.Payments.IPaymentReceiptSynchronizationService,
            YallaFinance.Infrastructure.Services.Payments.PaymentReceiptSynchronizationService>();
        services.AddSingleton<YallaFinance.Infrastructure.Services.Payments.PaymentReceiptSynchronizationCursor>();
        services.AddScoped<PostgresChargeSettlementStore>();
        services.AddScoped<IChargeSettlementConfiguration, FinancialSettlementConfiguration>();
        services.AddScoped<YallaFinance.Application.Repositories.CustomerContracts.ICustomerContractRepository>(provider => provider.GetRequiredService<PostgresContractRepository>());
        services.AddScoped<IChargeRepository, PostgresChargeRepository>();
        services.AddScoped<PostgresChargeReadOperations>();
        services.AddScoped<PostgresChargeItemRepository>();
        services.AddScoped<PostgresChargeWriter>();
        services.AddScoped<IUnitOfWork, PostgresUnitOfWork>();
        services.AddScoped<IIntegrationOperationRepository, PostgresIntegrationOperationRepository>();
        services.AddScoped<ICustomerRepository>(provider => provider.GetRequiredService<PostgresCustomerRepository>());
        services.AddSingleton<IFleetMovementRepository, JsonFleetMovementRepository>();
        services.AddScoped<ICategoryRepository, PostgresCategoryRepository>();
        services.AddScoped<IPaymentMethodRepository, PostgresPaymentMethodRepository>();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<YallaFinance.Application.Common.Context.CurrentSessionContext>();
        services.TryAddScoped<YallaFinance.Application.Common.Context.CurrentExecutionActorContext>();
        services.TryAddScoped<YallaFinance.Infrastructure.Database.Auditing.AuditSaveChangesInterceptor>();
        services.TryAddScoped<AccountMutationLock>();
        services.TryAddScoped<YallaFinance.Application.Repositories.Users.IUserIdentityRepository, PostgresUserIdentityRepository>();
        services.TryAddScoped<YallaFinance.Application.Services.Authentication.IUserPasswordHasher, YallaFinance.Infrastructure.Services.Authentication.UserPasswordHasher>();
        services.TryAddScoped<YallaFinance.Application.Repositories.Users.IUserRepository, PostgresUserRepository>();
        services.AddScoped<YallaFinance.Application.Repositories.Sessions.IUserSessionRepository, PostgresUserSessionRepository>();
        services.AddScoped<YallaFinance.Application.Services.Authentication.IUserSessionService, YallaFinance.Infrastructure.Services.Authentication.UserSessionService>();
        services.AddScoped<YallaFinance.Application.Repositories.Settings.ITenantSettingRepository, YallaFinance.Infrastructure.Repositories.Settings.PostgresTenantSettingRepository>();

        services.AddScoped<YallaFinance.Application.Services.Payments.IPublicPaymentAttemptService, YallaFinance.Infrastructure.Services.Payments.PostgresPublicPaymentAttemptService>();
        services.AddScoped<YallaFinance.Application.Services.Payments.IPaymentPublicationSettings, YallaFinance.Infrastructure.Services.Payments.PaymentPublicationSettings>();
        services.AddScoped<YallaFinance.Application.Features.Payments.GeneratePaymentRequests.PaymentRequestBatchService>();
        services.AddScoped<IJobRepository, PostgresJobRepository>();
        services.AddScoped<YallaFinance.Application.Repositories.Jobs.IJobRetryRepository, PostgresJobRepository>();
        services.AddScoped<IJobExecutionRepository, PostgresJobExecutionRepository>();

        services.AddFinancialIntegrations(configuration);
        return services;
    }

    public static IServiceCollection AddFinancialIntegrations(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        services.AddLogging();
        services.TryAddScoped<ICompanyConnectionRepository, PostgresCompanyConnectionRepository>();
        var interval = configuration.GetValue<int?>("FinancialIntegrations:MinimumRequestIntervalMilliseconds") ?? 100;
        if (interval is < 10 or > 60000)
            throw new IntegrationConfigurationException("InvalidRequestInterval");
        services.AddSingleton(new FinancialOperationCoordinator(interval));
        services.AddScoped<IFinancialIntegrationService, FinancialIntegrationService>();
        services.AddScoped<IFinancialReceivableSettlementService, FinancialReceivableSettlementService>();
        services.AddProviderIntegrations(configuration);
        services.TryAddScoped<IPixService, PixService>();
        services.TryAddScoped<YallaFinance.Application.Services.Payments.CreditCard.ICreditCardService,
            YallaFinance.Infrastructure.Services.Payments.CreditCard.CreditCardService>();
        services.TryAddScoped<IBoletoService, BoletoService>();
        return services;
    }

    private static IServiceCollection AddProviderIntegrations(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var infrastructureTypes = typeof(DependencyInjection).Assembly.GetTypes();
        foreach (var providerModuleType in infrastructureTypes.Where(type =>
            !type.IsAbstract &&
            !type.IsInterface &&
            typeof(IProviderModule).IsAssignableFrom(type)))
        {
            var providerModule = (IProviderModule?)Activator.CreateInstance(providerModuleType, nonPublic: true)
                ?? throw new InvalidOperationException($"Could not create provider module {providerModuleType.Name}.");
            providerModule.Register(services, configuration);
        }

        foreach (var providerImplementationType in infrastructureTypes.Where(type =>
            type.IsClass &&
            !type.IsAbstract &&
            type.GetInterfaces().Any(interfaceType =>
                interfaceType != typeof(IProviderCapability) &&
                typeof(IProviderCapability).IsAssignableFrom(interfaceType))))
        {
            foreach (var capabilityType in providerImplementationType.GetInterfaces().Where(interfaceType =>
                interfaceType != typeof(IProviderCapability) &&
                typeof(IProviderCapability).IsAssignableFrom(interfaceType)))
            {
                services.TryAddEnumerable(ServiceDescriptor.Scoped(capabilityType, providerImplementationType));
            }
        }

        services.TryAddScoped(typeof(IProviderResolver<>), typeof(ProviderResolver<>));
        return services;
    }

    public static IServiceCollection AddInfrastructureAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        var jwt = configuration.GetSection(JwtAuthenticationOptions.SectionName).Get<JwtAuthenticationOptions>() ?? new();
        // Legacy integration Bearer only. Interactive login uses database-backed opaque sessions.
        // Preserve integration behavior: restarting the process invalidates integration tokens.
        jwt.SigningKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        services.AddSingleton<IOptions<JwtAuthenticationOptions>>(Options.Create(jwt));
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                ValidateIssuer = true,
                ValidIssuer = jwt.Issuer,
                ValidateAudience = true,
                ValidAudience = jwt.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });
        services.AddAuthorization();
        // Authentication can also be composed independently of the worker module.
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddScoped<YallaFinance.Application.Common.Context.CurrentSessionContext>();
        services.TryAddScoped<YallaFinance.Application.Common.Context.CurrentExecutionActorContext>();
        services.TryAddScoped<YallaFinance.Infrastructure.Database.Auditing.AuditSaveChangesInterceptor>();
        services.TryAddScoped<IUnitOfWork, PostgresUnitOfWork>();
        services.TryAddScoped<YallaFinance.Application.Repositories.Settings.ITenantSettingRepository, YallaFinance.Infrastructure.Repositories.Settings.PostgresTenantSettingRepository>();
        services.TryAddScoped<YallaFinance.Application.Repositories.Sessions.IUserSessionRepository, PostgresUserSessionRepository>();
        services.TryAddScoped<YallaFinance.Application.Services.Authentication.IUserSessionService, YallaFinance.Infrastructure.Services.Authentication.UserSessionService>();
        services.AddScoped<YallaFinance.Application.Repositories.Authentication.ITenantAccessRepository, PostgresTenantAccessRepository>();
        services.TryAddScoped<YallaFinance.Infrastructure.Repositories.Access.AccountMutationLock>();
        services.TryAddScoped<YallaFinance.Application.Services.Authentication.IUserPasswordHasher, YallaFinance.Infrastructure.Services.Authentication.UserPasswordHasher>();
        services.TryAddScoped<YallaFinance.Application.Repositories.Users.IUserIdentityRepository, PostgresUserIdentityRepository>();
        services.TryAddScoped<YallaFinance.Application.Repositories.Users.IUserRepository, PostgresUserRepository>();
        services.TryAddScoped<YallaFinance.Application.Repositories.AccessProfiles.IAccessProfileRepository, PostgresAccessProfileRepository>();
        services.TryAddScoped<YallaFinance.Application.Repositories.Authorization.IFunctionalityAccessRepository, PostgresFunctionalityAccessRepository>();
        services.AddScoped<IAuthenticationService, CatalogAuthenticationService>();
        services.AddScoped<IIntegrationAuthenticationService, IntegrationAuthenticationService>();
        services.AddScoped<YallaFinance.Application.Services.Authentication.IPasswordChangeTicketStore,
            YallaFinance.Infrastructure.Services.Authentication.PasswordChangeTicketStore>();
        return services;
    }
}
