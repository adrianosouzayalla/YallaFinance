using YallaFinance.Infrastructure.Providers.Payments.Pix;
using YallaFinance.Infrastructure.Providers.Configuration;
using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Refit;

using YallaFinance.Infrastructure.Services.Payments.Pix;
using YallaFinance.Infrastructure.Providers.Sicoob.Payments.Pix.Requests;
using YallaFinance.Infrastructure.Common.Errors;
using YallaFinance.Application.Services.Payments.Pix.Contracts;

namespace YallaFinance.Infrastructure.Providers.Sicoob.Payments.Pix;

internal sealed partial class SicoobPixChargeProvider(
    SicoobConfigurationReader configuration,
    SicoobHttpClientFactory clients,
    SicoobAuthentication authentication,
    TimeProvider clock,
    ILogger<SicoobPixChargeProvider> logger) : IPixChargeProvider
{
    public string Provider => "Sicoob";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<CreatePixChargeResult> CreateAsync(
        CreatePixChargeData data,
        ProviderConnection connection,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CreatePixChargeResult Result(
            PixChargeCreationOutcome outcome,
            string? error,
            PixChargeSnapshot? charge = null)
            => new(outcome, connection.Id, Provider, data.ProviderReference, charge, error);
        if (!SicoobPixValidation.IsValidPixChargeData(data))
            return Result(PixChargeCreationOutcome.Rejected, "Validation");
        var dispatched = false;
        int? responseStatus = null;
        var stage = "ConfigurationRead";
        logger.LogInformation(
            "Sicoob Pix emission started. OperationId {OperationId}; ConnectionId {ConnectionId}; Provider {Provider}",
            data.OperationId,
            connection.Id,
            Provider);
        try
        {
            var options = await configuration.ReadAsync(connection, true, cancellationToken);
            logger.LogInformation(
                "Sicoob Pix configuration read completed. OperationId {OperationId}; ConnectionId {ConnectionId}; Stage {Stage}",
                data.OperationId,
                connection.Id,
                stage);
            stage = "ClientPreparation";
            using var client = clients.Create(options);
            logger.LogInformation(
                "Sicoob Pix client prepared. OperationId {OperationId}; ConnectionId {ConnectionId}; Stage {Stage}",
                data.OperationId,
                connection.Id,
                stage);
            stage = "OAuth";
            var token = await authentication.GetAccessTokenAsync(client, options, cancellationToken);
            logger.LogInformation(
                "Sicoob OAuth completed. OperationId {OperationId}; ConnectionId {ConnectionId}; Stage {Stage}",
                data.OperationId,
                connection.Id,
                stage);
            var sicoobPixApi = RestService.For<ISicoobPixApi>(
                client,
                new RefitSettings { ContentSerializer = new SystemTextJsonContentSerializer(JsonOptions) });
            var payer = data.Payer is null ? null : new SicoobPixPayerRequest(
                data.Payer.TaxId.Length == 11 ? data.Payer.TaxId : null,
                data.Payer.TaxId.Length == 14 ? data.Payer.TaxId : null, data.Payer.Name);
            var request = new SicoobCreatePixChargeRequest(
                new(data.ExpirationSeconds),
                new(data.Amount.ToString("F2", CultureInfo.InvariantCulture)),
                options.ReceiverKey!,
                payer,
                data.Description);
            cancellationToken.ThrowIfCancellationRequested();
            // Consulta previa pela referencia: reaproveita a cobranca existente (sem duplicar) ou confirma que ela nao existe.
            stage = "PixPreLookup";
            using var lookup = await sicoobPixApi.GetAsync(
                data.ProviderReference,
                options.ClientId,
                "Bearer " + token,
                cancellationToken);
            var lookupBody = await lookup.Content.ReadAsStringAsync(cancellationToken);
            if (lookup.IsSuccessStatusCode)
            {
                var existing = ParsePixChargeResponse(
                    lookupBody,
                    data.ProviderReference,
                    options.ReceiverKey,
                    data,
                    out var existingFailureCode);
                if (existing is null)
                {
                    logger.LogWarning(
                        "Sicoob Pix charge already exists and does not match the request. OperationId {OperationId}; ConnectionId {ConnectionId}; FailureCode {FailureCode}",
                        data.OperationId,
                        connection.Id,
                        existingFailureCode);
                    return Result(PixChargeCreationOutcome.Rejected, "ProviderChargeMismatch");
                }
                if (existing.Status != PixChargeStatus.Active || existing.CopyPaste is null)
                    return Result(PixChargeCreationOutcome.Rejected, "ProviderChargeNotPayable");
                return Result(PixChargeCreationOutcome.Completed, null, existing);
            }
            if (!IsKnownProblem(lookup, lookupBody, 404, "NaoEncontrado")
                && !IsKnownProblem(lookup, lookupBody, 404, "CobNaoEncontrada"))
                return Result(PixChargeCreationOutcome.Rejected, "ProviderLookupUnavailable");

            // Everything after this point may have reached the bank, even if the response is lost.
            stage = "PixPutDispatch";
            logger.LogInformation(
                "Sicoob Pix PUT dispatching. OperationId {OperationId}; ConnectionId {ConnectionId}; ExpirationSeconds {ExpirationSeconds}",
                data.OperationId,
                connection.Id,
                data.ExpirationSeconds);
            dispatched = true;
            using var response = await sicoobPixApi.CreateAsync(
                data.ProviderReference,
                request,
                options.ClientId,
                "Bearer " + token,
                cancellationToken);
            responseStatus = (int)response.StatusCode;
            stage = "PixPutResponse";
            var sentRequest = response.RequestMessage;
            logger.LogInformation(
                "Sicoob Pix HTTP response. OperationId {OperationId}; ConnectionId {ConnectionId}; HttpMethod {HttpMethod}; Host {Host}; Path {Path}; RequestContentType {RequestContentType}; RequestContentLength {RequestContentLength}; TransferEncodingChunked {TransferEncodingChunked}; HttpStatus {HttpStatus}; ResponseContentType {ResponseContentType}",
                data.OperationId,
                connection.Id,
                sentRequest?.Method.Method,
                sentRequest?.RequestUri?.Host,
                sentRequest?.RequestUri?.AbsolutePath.Replace(
                    data.ProviderReference,
                    "{txid}",
                    StringComparison.Ordinal),
                sentRequest?.Content?.Headers.ContentType?.ToString(),
                sentRequest?.Content?.Headers.ContentLength,
                sentRequest?.Headers.TransferEncodingChunked,
                responseStatus,
                response.Content.Headers.ContentType?.ToString());
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var problemType = ReadProblemType(body);
                var rejected = IsKnownProblem(response, body, 400, "RequisicaoInvalida");
                var errorCode = rejected ? "ProviderRejected_RequisicaoInvalida" : MapUnknownOutcomeToFailureCode(
                    response,
                    problemType);
                logger.LogWarning(
                    "Sicoob Pix PUT returned non-success. OperationId {OperationId}; ConnectionId {ConnectionId}; Stage {Stage}; HttpStatus {HttpStatus}; ProviderProblemType {ProviderProblemType}; Outcome {Outcome}; FailureCode {FailureCode}",
                    data.OperationId,
                    connection.Id,
                    stage,
                    (int)response.StatusCode,
                    problemType,
                    PixChargeCreationOutcome.Rejected,
                    errorCode);
                return rejected
                    ? Result(PixChargeCreationOutcome.Rejected, errorCode)
                    : Result(PixChargeCreationOutcome.Rejected, errorCode);
            }
            stage = "PixResponseValidation";
            var charge = ParsePixChargeResponse(
                body,
                data.ProviderReference,
                options.ReceiverKey,
                data,
                out var validationCode);
            if (charge is null || (charge.Status == PixChargeStatus.Active && charge.CopyPaste is null))
            {
                // A response that contradicts the request, or that has no usable BR Code, is never
                // followed by a query: a follow-up GET would not be compared with the request and
                // could confirm a charge whose payer or description differs from what was sent.
                var unusableCode = validationCode ?? "ProviderResponseMissingCopyPaste";
                logger.LogWarning(
                    "Sicoob Pix creation response is not usable and is not confirmed. OperationId {OperationId}; ConnectionId {ConnectionId}; Stage {Stage}; FailureCode {FailureCode}",
                    data.OperationId,
                    connection.Id,
                    stage,
                    unusableCode);
                return Result(PixChargeCreationOutcome.Rejected, unusableCode);
            }
            if (!charge.IsPayable)
            {
                LogNonPayablePixCharge(
                    data.OperationId,
                    connection.Id,
                    stage,
                    responseStatus,
                    charge,
                    validationCode);
                return Result(PixChargeCreationOutcome.Rejected, "InstrumentUnavailable", charge);
            }
            logger.LogInformation(
                "Sicoob Pix emission completed. OperationId {OperationId}; ConnectionId {ConnectionId}; Stage {Stage}; PixStatus {PixStatus}",
                data.OperationId,
                connection.Id,
                stage,
                charge.Status);
            return Result(PixChargeCreationOutcome.Completed, null, charge);
        }
        catch (IntegrationConfigurationException exception) when (!dispatched)
        {
            logger.LogWarning(
                "Sicoob Pix emission stopped before dispatch. OperationId {OperationId}; ConnectionId {ConnectionId}; Stage {Stage}; FailureCode {FailureCode}",
                data.OperationId,
                connection.Id,
                stage,
                exception.Code);
            return Result(PixChargeCreationOutcome.Rejected, exception.Code);
        }
        catch (OperationCanceledException) when (!dispatched && cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (IsCommunicationFailure(exception))
        {
            var outcome = PixChargeCreationOutcome.Rejected;
            var failureCode = dispatched ? MapUnknownOutcomeToFailureCode(exception) : MapPreparationExceptionToFailureCode(exception);
            logger.LogError(
                "Sicoob Pix emission failed. OperationId {OperationId}; ConnectionId {ConnectionId}; Stage {Stage}; Dispatched {Dispatched}; HttpStatus {HttpStatus}; Outcome {Outcome}; FailureCode {FailureCode}; ExceptionType {ExceptionType}; RootExceptionType {RootExceptionType}; HResult {HResult}",
                data.OperationId,
                connection.Id,
                stage,
                dispatched,
                responseStatus,
                outcome,
                failureCode,
                exception.GetType().Name,
                exception.GetBaseException().GetType().Name,
                exception.HResult);
            return Result(outcome, failureCode);
        }
    }

    public async Task<PixChargeQueryResult> GetAsync(
        PixChargeQuery query,
        ProviderConnection connection,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        PixChargeQueryResult Result(
            PixChargeQueryOutcome outcome,
            string? error,
            PixChargeSnapshot? charge = null)
            => new(outcome, connection.Id, Provider, query.ProviderReference, charge, error);
        if (!SicoobPixValidation.IsValidProviderReference(query.ProviderReference))
            return Result(
                PixChargeQueryOutcome.Indeterminate,
                "Validation");
        try
        {
            var options = await configuration.ReadAsync(connection, false, cancellationToken);
            using var client = clients.Create(options);
            var token = await authentication.GetAccessTokenAsync(client, options, cancellationToken);
            var sicoobPixApi = RestService.For<ISicoobPixApi>(
                client,
                new RefitSettings { ContentSerializer = new SystemTextJsonContentSerializer(JsonOptions) });
            using var response = await sicoobPixApi.GetAsync(
                query.ProviderReference,
                options.ClientId,
                "Bearer " + token,
                cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var notFound = IsKnownProblem(response, body, 404, "NaoEncontrado") ||
                    IsKnownProblem(
                        response,
                        body,
                        404,
                        "CobNaoEncontrada");
                var problemType = ReadProblemType(body);
                var failureCode = notFound ? null : MapUnknownOutcomeToFailureCode(response, problemType);
                logger.LogWarning(
                    "Sicoob Pix query returned non-success. ConnectionId {ConnectionId}; HttpStatus {HttpStatus}; ProviderProblemType {ProviderProblemType}; Outcome {Outcome}; FailureCode {FailureCode}",
                    connection.Id,
                    (int)response.StatusCode,
                    problemType,
                    notFound ? PixChargeQueryOutcome.NotFound : PixChargeQueryOutcome.Indeterminate,
                    failureCode);
                return notFound ? Result(PixChargeQueryOutcome.NotFound, null)
                    : Result(PixChargeQueryOutcome.Indeterminate, failureCode);
            }
            var charge = ParsePixChargeResponse(
                body,
                query.ProviderReference,
                options.ReceiverKey,
                null,
                out var validationCode);
            if (charge is null)
            {
                logger.LogWarning(
                    "Sicoob Pix query response could not be validated. ConnectionId {ConnectionId}; FailureCode {FailureCode}",
                    connection.Id,
                    validationCode);
                return Result(PixChargeQueryOutcome.Indeterminate, validationCode);
            }
            if (!charge.IsPayable)
                LogNonPayablePixCharge(
                    null,
                    connection.Id,
                    "PixGetResponseValidation",
                    (int)response.StatusCode,
                    charge,
                    validationCode);
            return Result(
                PixChargeQueryOutcome.Found,
                charge.CopyPaste is null ? "InstrumentUnavailable" : null,
                charge);
        }
        catch (IntegrationConfigurationException exception)
        {
            logger.LogWarning(
                "Sicoob Pix query stopped by configuration. ConnectionId {ConnectionId}; FailureCode {FailureCode}",
                connection.Id,
                exception.Code);
            return Result(PixChargeQueryOutcome.Indeterminate, exception.Code);
        }
        catch (Exception exception) when (IsCommunicationFailure(exception))
        {
            var failureCode = MapUnknownOutcomeToFailureCode(exception);
            logger.LogError(
                "Sicoob Pix query failed. ConnectionId {ConnectionId}; FailureCode {FailureCode}; ExceptionType {ExceptionType}; RootExceptionType {RootExceptionType}; HResult {HResult}",
                connection.Id,
                failureCode,
                exception.GetType().Name,
                exception.GetBaseException().GetType()
                    .Name,
                exception.HResult);
            return Result(PixChargeQueryOutcome.Indeterminate, failureCode);
        }
    }

    private void LogNonPayablePixCharge(
        Guid? operationId,
        Guid connectionId,
        string stage,
        int? httpStatus,
        PixChargeSnapshot charge,
        string? validationCode)
    {
        var nonPayableReason = charge.Status != PixChargeStatus.Active
            ? "ChargeStatusNotActive"
            : charge.ExpiresAt <= charge.ObservedAt
                ? "ChargeExpiredAtObservation"
                : charge.CopyPaste is null
                    ? validationCode ?? "PixCodeUnavailable"
                    : "ProviderChargeNotPayable";
        logger.LogWarning(
            "Sicoob Pix response is not payable. OperationId {OperationId}; ConnectionId {ConnectionId}; Stage {Stage}; HttpStatus {HttpStatus}; PixStatus {PixStatus}; HasValidatedPixCode {HasValidatedPixCode}; HasLocation {HasLocation}; ExpiresAtUtc {ExpiresAtUtc}; ObservedAtUtc {ObservedAtUtc}; NonPayableReason {NonPayableReason}",
            operationId,
            connectionId,
            stage,
            httpStatus,
            charge.Status,
            charge.CopyPaste is not null,
            charge.Location is not null,
            charge.ExpiresAt,
            charge.ObservedAt,
            nonPayableReason);
    }

    private static bool IsCommunicationFailure(Exception exception) => exception is HttpRequestException or IOException
        or OperationCanceledException or JsonException or ApiException or ApiRequestException or InvalidOperationException;

    private static string MapPreparationExceptionToFailureCode(Exception exception)
    {
        static string TypeName(Exception error)
        {
            var name = new string(error.GetType().Name.Where(char.IsAsciiLetterOrDigit).ToArray());
            return string.IsNullOrEmpty(name) ? "Unknown" : name;
        }

        var rootCause = exception.GetBaseException();
        var nativeCode = rootCause is Win32Exception win32 ? $"_{win32.NativeErrorCode}" : string.Empty;
        return $"ConnectionPreparationFailed_{TypeName(exception)}_{TypeName(rootCause)}{nativeCode}";
    }

    private static string MapUnknownOutcomeToFailureCode(HttpResponseMessage response, string? problemType = null) =>
        $"ProviderOutcomeUnknown_HttpStatus_{(int)response.StatusCode}{(problemType is null ? string.Empty : $"_{problemType}")}";

    private static string MapUnknownOutcomeToFailureCode(Exception exception)
    {
        static string TypeName(Exception error)
        {
            var name = new string(error.GetType().Name.Where(char.IsAsciiLetterOrDigit).ToArray());
            return string.IsNullOrEmpty(name) ? "Unknown" : name;
        }

        var rootCause = exception.GetBaseException();
        var nativeCode = rootCause is Win32Exception win32 ? $"_{win32.NativeErrorCode}" : string.Empty;
        var code = $"ProviderOutcomeUnknown_{TypeName(rootCause)}{nativeCode}";
        return code.Length <= 80 ? code : code[..80];
    }
}
