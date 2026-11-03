using YallaFinance.Infrastructure.Providers.Payments.Pix;
using YallaFinance.Infrastructure.Providers.Configuration;
using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Refit;

using YallaFinance.Infrastructure.Services.Payments.Pix;
using YallaFinance.Infrastructure.Providers.Sicoob.Payments.Pix.Requests;
using YallaFinance.Infrastructure.Providers.Sicoob.Payments.Pix.Responses;
using YallaFinance.Infrastructure.Common.Errors;
using YallaFinance.Application.Services.Payments.Pix.Contracts;

namespace YallaFinance.Infrastructure.Providers.Sicoob.Payments.Pix;

internal sealed class SicoobPixReceivedProvider(
    SicoobConfigurationReader configuration,
    SicoobHttpClientFactory clients,
    SicoobAuthentication authentication,
    TimeProvider clock,
    ILogger<SicoobPixReceivedProvider> logger) : IPixReceivedProvider
{
    public string Provider => "Sicoob";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    // A API Pix do Sicoob exige que inicio e fim estejam dentro do mesmo mês civil.
    public PixReceivedWindowRule WindowRule => PixReceivedWindowRule.CalendarMonth;

    public async Task<PixReceivedWindow> ResolveDefaultWindowAsync(
        ProviderConnection connection,
        CancellationToken cancellationToken)
    {
        var options = await configuration.ReadAsync(connection, false, cancellationToken);
        var days = PixReceivedDefaults.BaseLookbackDays;
        if (options.ReceivedLookbackDays is { Length: > 0 } configuredDays
            && (!int.TryParse(configuredDays, NumberStyles.None, CultureInfo.InvariantCulture,
                out days) || days is < 1 or > 31))
            throw new IntegrationConfigurationException("InvalidReceivedLookbackDays");
        var end = clock.GetUtcNow();
        return new(end.AddDays(-days), end);
    }

    public async Task<PixReceivedQueryResult> QueryAsync(
        PixReceivedQuery query,
        ProviderConnection connection,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        PixReceivedQueryResult Result(
            PixReceivedQueryOutcome outcome,
            string? error = null,
            IReadOnlyList<PixReceivedTransaction>? transactions = null,
            PixReceivedPagination? pagination = null,
            DateTimeOffset? start = null,
            DateTimeOffset? end = null)
            => new(outcome, connection.Id, Provider, transactions ?? [], pagination, error, start, end);
        if (!IsValidPixReceivedQuery(query))
            return Result(PixReceivedQueryOutcome.Rejected, "Validation");

        try
        {
            var options = await configuration.ReadAsync(connection, false, cancellationToken);
            var start = query.Start;
            var end = query.End;
            if (start is null && end is null)
            {
                if (!int.TryParse(options.ReceivedLookbackDays, NumberStyles.None, CultureInfo.InvariantCulture,
                    out var days) || days is < 1 or > 31)
                    throw new IntegrationConfigurationException("InvalidReceivedLookbackDays");
                end = clock.GetUtcNow();
                start = end.Value.AddDays(-days);
            }
            var effectiveQuery = query with
            {
                Start = start,
                End = end
            };
            using var client = clients.Create(options);
            var token = await authentication.GetAccessTokenAsync(client, options, cancellationToken);
            var sicoobPixApi = RestService.For<ISicoobPixApi>(
                client,
                new RefitSettings { ContentSerializer = new SystemTextJsonContentSerializer(JsonOptions) });
            var parameters = new SicoobPixReceivedQueryParameters(
                start!.Value.ToString("O", CultureInfo.InvariantCulture),
                end!.Value.ToString("O", CultureInfo.InvariantCulture),
                query.TxId,
                query.Page,
                query.ItemsPerPage);
            using var response = await sicoobPixApi.GetReceivedAsync(
                parameters,
                options.ClientId,
                "Bearer " + token,
                cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                var providerErrorBody = errorBody.Length > 1000 ? errorBody[..1000] : errorBody;
                logger.LogWarning(
                    "Sicoob Pix received query returned non-success. ConnectionId {ConnectionId}; HttpStatus {HttpStatus}; Inicio {Inicio}; Fim {Fim}; Page {Page}; ItemsPerPage {ItemsPerPage}; TxIdFiltered {TxIdFiltered}; ProviderErrorBody {ProviderErrorBody}",
                    connection.Id,
                    (int)response.StatusCode,
                    parameters.Start,
                    parameters.End,
                    parameters.Page,
                    parameters.ItemsPerPage,
                    parameters.TxId is not null,
                    providerErrorBody);
                return Result(PixReceivedQueryOutcome.Indeterminate,
                    $"ProviderOutcomeUnknown_HttpStatus_{(int)response.StatusCode}");
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!TryParseReceivedPixResponse(body, effectiveQuery, out var transactions, out var pagination,
                out var failureCode, out var failureReason, out var skippedItemReasons))
            {
                logger.LogWarning(
                    "Sicoob Pix received query response could not be validated. ConnectionId {ConnectionId}; WindowStartUtc {WindowStartUtc}; WindowEndUtc {WindowEndUtc}; FailureCode {FailureCode}; FailureReason {FailureReason}",
                    connection.Id,
                    start,
                    end,
                    failureCode,
                    failureReason);
                return Result(PixReceivedQueryOutcome.Indeterminate, failureCode);
            }
            if (skippedItemReasons.Count > 0)
            {
                logger.LogWarning(
                    "Sicoob Pix received page skipped invalid items and continued. ConnectionId {ConnectionId}; Page {Page}; SkippedItems {SkippedItems}; Reasons {Reasons}",
                    connection.Id,
                    effectiveQuery.Page,
                    skippedItemReasons.Count,
                    string.Join(",", skippedItemReasons.Distinct()));
            }
            return Result(PixReceivedQueryOutcome.Completed, transactions: transactions, pagination: pagination,
                start: start, end: end);
        }
        catch (IntegrationConfigurationException exception)
        {
            logger.LogWarning(
                "Sicoob Pix received query stopped by configuration. ConnectionId {ConnectionId}; FailureCode {FailureCode}",
                connection.Id,
                exception.Code);
            return Result(PixReceivedQueryOutcome.Rejected, exception.Code);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (IsCommunicationFailure(exception))
        {
            logger.LogError(
                "Sicoob Pix received query failed. ConnectionId {ConnectionId}; ExceptionType {ExceptionType}; RootExceptionType {RootExceptionType}",
                connection.Id,
                exception.GetType().Name,
                exception.GetBaseException().GetType().Name);
            return Result(PixReceivedQueryOutcome.Indeterminate, "ProviderOutcomeUnknown");
        }
    }

    private static bool TryParseReceivedPixResponse(
        string body,
        PixReceivedQuery query,
        out IReadOnlyList<PixReceivedTransaction> transactions,
        out PixReceivedPagination? pagination,
        out string failureCode,
        out string failureReason,
        out IReadOnlyList<string> skippedItemReasons)
    {
        transactions = [];
        skippedItemReasons = [];
        pagination = null;
        failureCode = "ProviderResponseInvalid";
        failureReason = "Unknown";
        try
        {
            using var document = JsonDocument.Parse(body);
            var response = JsonSerializer.Deserialize<SicoobPixReceivedResponse>(body, JsonOptions);
            if (response?.Pix is null || response.Parameters?.Pagination is null)
            {
                failureReason = "MissingPixOrPagination";
                return false;
            }
            if (!document.RootElement.TryGetProperty("pix", out var pixElement)
                || pixElement.ValueKind != JsonValueKind.Array
                || pixElement.GetArrayLength() != response.Pix.Count)
            {
                failureReason = "PixArrayShapeMismatch";
                return false;
            }

            var items = new List<PixReceivedTransaction>(response.Pix.Count);
            var skippedItems = new List<string>();
            for (var index = 0; index < response.Pix.Count; index++)
            {
                var item = response.Pix[index];
                if (string.IsNullOrWhiteSpace(item.EndToEndId) || item.EndToEndId.Length > 64
                    || item.EndToEndId.Any(char.IsControl))
                {
                    skippedItems.Add("EndToEndIdInvalid");
                    continue;
                }
                if (item.TxId is not null && (item.TxId.Length > 35 || item.TxId.Any(char.IsControl)))
                {
                    skippedItems.Add("TxIdInvalid");
                    continue;
                }
                if (item.Amount is null || !decimal.TryParse(item.Amount, NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture, out var amount) || amount <= 0)
                {
                    skippedItems.Add("AmountInvalid");
                    continue;
                }
                if (item.ReceivedAt is null || !TryParseOffsetDateTime(item.ReceivedAt, out var receivedAt))
                {
                    skippedItems.Add("ReceivedAtInvalid");
                    continue;
                }
                if (receivedAt < query.Start || receivedAt > query.End)
                {
                    skippedItems.Add("ReceivedAtOutsideQueryWindow");
                    continue;
                }
                if (query.TxId is not null && !string.Equals(query.TxId, item.TxId, StringComparison.Ordinal))
                {
                    skippedItems.Add("TxIdDoesNotMatchFilter");
                    continue;
                }
                items.Add(new(item.EndToEndId, item.TxId, amount, receivedAt.ToUniversalTime(),
                    pixElement[index].GetRawText()));
            }

            var sourcePagination = response.Parameters.Pagination;
            if (sourcePagination.Page is not { } page || page != query.Page
                || sourcePagination.ItemsPerPage is not { } itemsPerPage || itemsPerPage != query.ItemsPerPage
                || sourcePagination.TotalPages is not { } totalPages || totalPages < 0
                || sourcePagination.TotalItems is not { } totalItems || totalItems < 0
                || items.Count > itemsPerPage)
            {
                failureReason = "PaginationInvalid";
                return false;
            }

            transactions = items;
            skippedItemReasons = skippedItems;
            pagination = new(page, itemsPerPage, totalPages, totalItems);
            failureCode = string.Empty;
            failureReason = string.Empty;
            return true;
        }
        catch (JsonException)
        {
            failureCode = "ProviderResponseInvalidJson";
            failureReason = "JsonParseOrDeserializationFailed";
            return false;
        }
    }

    private static bool IsValidPixReceivedQuery(PixReceivedQuery query)
        => query.TenantId != Guid.Empty && query.ConnectionId != Guid.Empty
            && !string.IsNullOrWhiteSpace(query.ExpectedProvider)
            && ((query.Start is null && query.End is null)
                || (query.Start is { } start && query.End is { } end && start != default && end != default && start <= end))
            && (query.TxId is null || (query.TxId.Length is > 0 and <= 35
                && query.TxId.All(char.IsAsciiLetterOrDigit)))
            && query.Page >= 0 && query.ItemsPerPage is > 0 and <= 100;

    private static bool IsCommunicationFailure(Exception exception) => exception is HttpRequestException or IOException
        or OperationCanceledException or JsonException or ApiException or ApiRequestException or InvalidOperationException;

    private static bool TryParseOffsetDateTime(string value, out DateTimeOffset result)
    {
        result = default;
        var hasExplicitOffset = value.EndsWith('Z')
            || (value.Length >= 6 && value[^6] is '+' or '-' && value[^3] == ':');
        return hasExplicitOffset && DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out result);
    }
}
