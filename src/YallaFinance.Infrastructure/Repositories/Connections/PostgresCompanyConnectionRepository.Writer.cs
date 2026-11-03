using Microsoft.EntityFrameworkCore;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Common.Transactions;
using YallaFinance.Application.Repositories.Connections;
using YallaFinance.Infrastructure.Database.Generated;
using YallaFinance.Infrastructure.Database.Generated.Entities;

namespace YallaFinance.Infrastructure.Repositories.Connections;

public sealed partial class PostgresCompanyConnectionRepository
{
    public Task<NotificationResult<Guid>> SaveConnectionAsync(Guid tenantId, Guid? connectionId,
        string name, string provider, string group, bool enabled, DateTime now,
        CancellationToken cancellationToken) => unitOfWork.ExecuteAsync(async token =>
    {
        ExternalConnection connection;
        if (connectionId is { } id)
        {
            var existingConnection = await database.ExternalConnections.SingleOrDefaultAsync(
                item => item.TenantId == tenantId && item.Id == id, token);
            if (existingConnection is null) return Failure<Guid>(ErrorCodes.NotFound, "Conexão não encontrada.");
            connection = existingConnection;

            if ((connection.Provider != provider || connection.Group != group)
                && (await database.ExternalConnectionSettings.AnyAsync(item => item.TenantId == tenantId && item.ConnectionId == id, token)
                    || await database.ExternalConnectionMappings.AnyAsync(item => item.TenantId == tenantId && item.ConnectionId == id, token)
                    || await database.PaymentMethodConnections.AnyAsync(item => item.TenantId == tenantId && item.ConnectionId == id, token)))
                return Failure<Guid>(ErrorCodes.Conflict, "Não é possível trocar o provider ou grupo de uma conexão que já possui configurações ou vínculos.");

            connection.Name = name;
            connection.Provider = provider;
            connection.Group = group;
            connection.Enabled = enabled;
        }
        else
        {
            connection = new ExternalConnection
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Name = name,
                Provider = provider,
                Group = group,
                Enabled = enabled,
            };
            database.ExternalConnections.Add(connection);
        }

        var commit = await unitOfWork.CommitAsync(token);
        return commit.IsSuccess
            ? NotificationResult<Guid>.Success(connection.Id)
            : NotificationResult<Guid>.Failure(commit.Errors);
    }, cancellationToken);

    public Task<NotificationResult<Guid>> SaveSettingsAsync(Guid tenantId, Guid connectionId,
        IReadOnlyDictionary<(string Group, string Key), string?> settings, DateTime now,
        CancellationToken cancellationToken) => unitOfWork.ExecuteAsync(async token =>
    {
        var connection = await database.ExternalConnections.AsNoTracking()
            .SingleOrDefaultAsync(item => item.TenantId == tenantId && item.Id == connectionId, token);
        if (connection is null) return Failure<Guid>(ErrorCodes.NotFound, "Conexão não encontrada.");
        var definitions = GetProviderSettingDefinitions(connection.Provider);
        if (definitions.Length == 0)
            return Failure<Guid>(ErrorCodes.Validation, "O provider desta conexão não possui configurações editáveis.");
        var expectedGroup = connection.Provider == "Nibo" ? "FinancialSystem" : "PaymentProvider";
        if (connection.Group != expectedGroup || settings.Count != definitions.Length
            || settings.Keys.Any(key => !definitions.Contains(key.Group + "." + key.Key, StringComparer.Ordinal)))
            return Failure<Guid>(ErrorCodes.Validation, "As configurações não correspondem ao provider da conexão.");

        var existing = await database.ExternalConnectionSettings
            .Where(item => item.TenantId == tenantId && item.ConnectionId == connectionId)
            .ToListAsync(token);
        foreach (var pair in settings.OrderBy(pair => pair.Key.Group, StringComparer.Ordinal).ThenBy(pair => pair.Key.Key, StringComparer.Ordinal))
        {
            var row = existing.SingleOrDefault(item => item.Group == pair.Key.Group && item.Key == pair.Key.Key);
            if (pair.Value is null)
            {
                if (row is not null) database.ExternalConnectionSettings.Remove(row);
                continue;
            }
            if (row is null)
            {
                database.ExternalConnectionSettings.Add(new ExternalConnectionSetting
                {
                    Id = Guid.NewGuid(), TenantId = tenantId, ConnectionId = connectionId,
                    Group = pair.Key.Group, Key = pair.Key.Key, Value = pair.Value,
                });
            }
            else
            {
                row.Value = pair.Value;
            }
        }

        var commit = await unitOfWork.CommitAsync(token);
        return commit.IsSuccess
            ? NotificationResult<Guid>.Success(connectionId)
            : NotificationResult<Guid>.Failure(commit.Errors);
    }, cancellationToken);

    public Task<NotificationResult<Guid>> SaveMappingAsync(Guid tenantId, Guid connectionId, Guid? mappingId,
        string mappingType, Guid internalId, string externalValue, DateTime now,
        CancellationToken cancellationToken) => unitOfWork.ExecuteAsync(async token =>
    {
        var connectionExists = await database.ExternalConnections.AnyAsync(item => item.TenantId == tenantId
            && item.Id == connectionId && item.Provider == "Nibo" && item.Group == "FinancialSystem", token);
        if (!connectionExists) return Failure<Guid>(ErrorCodes.NotFound, "Conexão Nibo não encontrada.");
        var internalExists = mappingType == "Category"
            ? await database.Categories.AnyAsync(item => item.TenantId == tenantId && item.Id == internalId, token)
            : await database.PaymentMethods.AnyAsync(item => item.TenantId == tenantId && item.Id == internalId, token);
        if (!internalExists) return Failure<Guid>(ErrorCodes.NotFound, "O cadastro interno não foi encontrado nesta empresa.");

        ExternalConnectionMapping mapping;
        if (mappingId is { } id)
        {
            var existingMapping = await database.ExternalConnectionMappings.SingleOrDefaultAsync(item => item.TenantId == tenantId
                && item.ConnectionId == connectionId && item.Id == id, token);
            if (existingMapping is null) return Failure<Guid>(ErrorCodes.NotFound, "Mapeamento não encontrado.");
            mapping = existingMapping;
            if (mapping.MappingType != mappingType)
                return Failure<Guid>(ErrorCodes.Conflict, "O tipo do mapeamento não pode ser alterado.");
            mapping.InternalValue = internalId.ToString();
            mapping.ExternalValue = externalValue;
        }
        else
        {
            mapping = new ExternalConnectionMapping
            {
                Id = Guid.NewGuid(), TenantId = tenantId, ConnectionId = connectionId,
                MappingType = mappingType, InternalValue = internalId.ToString(), ExternalValue = externalValue,
            };
            database.ExternalConnectionMappings.Add(mapping);
        }

        var commit = await unitOfWork.CommitAsync(token);
        return commit.IsSuccess
            ? NotificationResult<Guid>.Success(mapping.Id)
            : NotificationResult<Guid>.Failure(commit.Errors);
    }, cancellationToken);

    public Task<NotificationResult<Guid>> RemoveMappingAsync(Guid tenantId, Guid connectionId, Guid mappingId,
        CancellationToken cancellationToken) => unitOfWork.ExecuteAsync(async token =>
    {
        if (!await database.ExternalConnections.AnyAsync(item => item.TenantId == tenantId
            && item.Id == connectionId && item.Provider == "Nibo" && item.Group == "FinancialSystem", token))
            return Failure<Guid>(ErrorCodes.NotFound, "Conexão Nibo não encontrada.");
        var mapping = await database.ExternalConnectionMappings.SingleOrDefaultAsync(item => item.TenantId == tenantId
            && item.ConnectionId == connectionId && item.Id == mappingId, token);
        if (mapping is null) return Failure<Guid>(ErrorCodes.NotFound, "Mapeamento não encontrado.");
        database.ExternalConnectionMappings.Remove(mapping);
        var commit = await unitOfWork.CommitAsync(token);
        return commit.IsSuccess ? NotificationResult<Guid>.Success(mappingId) : NotificationResult<Guid>.Failure(commit.Errors);
    }, cancellationToken);

    public Task<NotificationResult<Guid>> SavePaymentMethodAsync(Guid tenantId, Guid connectionId, Guid? linkId,
        Guid paymentMethodId, bool enabled, DateTime now,
        CancellationToken cancellationToken, CompanyPaymentMethodConfiguration? configuration = null) => unitOfWork.ExecuteAsync(async token =>
    {
        var connection = await database.ExternalConnections.AsNoTracking().SingleOrDefaultAsync(item => item.TenantId == tenantId && item.Id == connectionId, token);
        if (connection is null || connection.Group != "PaymentProvider" || !paymentCapabilities.TryGetValue(connection.Provider, out var instruments))
            return Failure<Guid>(ErrorCodes.NotFound, "Conexão de pagamento não encontrada ou sem capacidade implementada.");
        var paymentMethodLockScope = $"company-payment-method-connection:{tenantId:D}:{paymentMethodId:D}";
        await database.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({paymentMethodLockScope}, 0))", token);
        var method = await LockedPaymentMethodQuery(tenantId, paymentMethodId).SingleOrDefaultAsync(token);
        if (method is null) return Failure<Guid>(ErrorCodes.NotFound, "Forma de pagamento não encontrada nesta empresa.");
        PaymentMethodConnection? existingLink = null;
        if (linkId is { } id)
        {
            existingLink = await database.PaymentMethodConnections.SingleOrDefaultAsync(item => item.TenantId == tenantId
                && item.ConnectionId == connectionId && item.Id == id, token);
            if (existingLink is null) return Failure<Guid>(ErrorCodes.NotFound, "Vínculo de pagamento não encontrado.");
            if (existingLink.PaymentMethodId != paymentMethodId)
                return Failure<Guid>(ErrorCodes.Conflict, "O vínculo existente não pode ser transferido para outra forma de pagamento.");
        }
        if (existingLink is null && !method.Active)
            return Failure<Guid>(ErrorCodes.BusinessRule, "A forma de pagamento está inativa.");
        var otherLinks = database.PaymentMethodConnections.AsNoTracking()
            .Where(item => item.TenantId == tenantId && item.PaymentMethodId == paymentMethodId);
        if (existingLink is not null) otherLinks = otherLinks.Where(item => item.Id != existingLink.Id);
        if (await otherLinks.AnyAsync(token))
            return Failure<Guid>(ErrorCodes.Conflict, "Esta forma de pagamento já possui um vínculo com uma conexão.");

        var instrumentName = configuration?.InstrumentType?.ToString() ?? method.InstrumentType;
        var preservesUnclassifiedBinding = existingLink is not null && instrumentName is null && method.InstrumentType is null;
        if (!preservesUnclassifiedBinding && (!Enum.TryParse<YallaFinance.Domain.Payments.PaymentInstrumentType>(instrumentName, out var instrument)
            || instrument.ToString() != instrumentName || !instruments.Contains(instrument)))
            return Failure<Guid>(ErrorCodes.Validation, "Informe uma modalidade implementada por este provider.");
        if (method.InstrumentType != instrumentName
            && (existingLink is not null || await HasPaymentMethodHistoryAsync(tenantId, paymentMethodId, token)))
            return Failure<Guid>(ErrorCodes.Conflict, "A modalidade desta forma possui vínculos ou histórico. Sua classificação exige atualização administrativa autorizada.");

        method.InstrumentType = instrumentName;
        if (configuration is not null)
            method.InternalDescription = string.IsNullOrWhiteSpace(configuration.InternalDescription) ? null : configuration.InternalDescription;
        PaymentMethodConnection link;
        if (existingLink is not null)
        {
            link = existingLink;
            link.Enabled = enabled;
        }
        else
        {
            link = new PaymentMethodConnection
            {
                Id = Guid.NewGuid(), TenantId = tenantId, ConnectionId = connectionId,
                PaymentMethodId = paymentMethodId, Enabled = enabled,
            };
            database.PaymentMethodConnections.Add(link);
        }

        var commit = await unitOfWork.CommitAsync(token);
        return commit.IsSuccess ? NotificationResult<Guid>.Success(link.Id) : NotificationResult<Guid>.Failure(commit.Errors);
    }, cancellationToken);

    public Task<NotificationResult<Guid>> RemovePaymentMethodAsync(Guid tenantId, Guid connectionId, Guid linkId,
        CancellationToken cancellationToken) => unitOfWork.ExecuteAsync(async token =>
    {
        var connection = await database.ExternalConnections.AsNoTracking().SingleOrDefaultAsync(item => item.TenantId == tenantId && item.Id == connectionId, token);
        if (connection is null || connection.Group != "PaymentProvider" || !paymentCapabilities.ContainsKey(connection.Provider))
            return Failure<Guid>(ErrorCodes.NotFound, "Conexão de pagamento não encontrada.");
        var link = await database.PaymentMethodConnections.SingleOrDefaultAsync(item => item.TenantId == tenantId
            && item.ConnectionId == connectionId && item.Id == linkId, token);
        if (link is null) return Failure<Guid>(ErrorCodes.NotFound, "Vínculo de pagamento não encontrado.");
        var paymentMethodLockScope = $"company-payment-method-connection:{tenantId:D}:{link.PaymentMethodId:D}";
        await database.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({paymentMethodLockScope}, 0))", token);
        if (await LockedPaymentMethodQuery(tenantId, link.PaymentMethodId).SingleOrDefaultAsync(token) is null)
            return Failure<Guid>(ErrorCodes.NotFound, "Forma de pagamento não encontrada nesta empresa.");
        if (await HasPaymentMethodHistoryAsync(tenantId, link.PaymentMethodId, token))
            return Failure<Guid>(ErrorCodes.Conflict, "Este vínculo possui solicitações ou pagamentos associados e não pode ser removido.");
        database.PaymentMethodConnections.Remove(link);
        var commit = await unitOfWork.CommitAsync(token);
        return commit.IsSuccess ? NotificationResult<Guid>.Success(linkId) : NotificationResult<Guid>.Failure(commit.Errors);
    }, cancellationToken);

    // FOR UPDATE also coordinates with the key-share lock taken by reference FKs.
    // Check history only after acquiring it, within the same business transaction.
    internal IQueryable<PaymentMethod> LockedPaymentMethodQuery(Guid tenantId, Guid paymentMethodId) =>
        database.PaymentMethods.FromSqlInterpolated($"SELECT * FROM finance.payment_method WHERE tenant_id = {tenantId} AND id = {paymentMethodId} FOR UPDATE");

    private async Task<bool> HasPaymentMethodHistoryAsync(Guid tenantId, Guid paymentMethodId, CancellationToken cancellationToken) =>
        await database.PaymentRequestMethods.AnyAsync(method => method.TenantId == tenantId && method.PaymentMethodId == paymentMethodId, cancellationToken)
        || await database.PaymentAttempts.AnyAsync(attempt => attempt.TenantId == tenantId && attempt.PaymentMethodId == paymentMethodId, cancellationToken);

    private static NotificationResult<T> Failure<T>(string code, string message) =>
        NotificationResult<T>.Failure(new NotificationError(code, message));
}
