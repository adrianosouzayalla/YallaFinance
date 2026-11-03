using FluentValidation;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using YallaFinance.Application.Common.Context;
using YallaFinance.Application.Common.Results;



using YallaFinance.Domain.Integrations;
using YallaFinance.Application.Repositories.IntegrationOperations;
using YallaFinance.Application.Repositories.Authentication;
using YallaFinance.Application.Features.Jobs.Processing;
using YallaFinance.Application.Common.Transactions;
using YallaFinance.Application;

namespace YallaFinance.Application.Features.Inbound.Debits.ReceiveDebit;

public sealed class ReceiveInboundDebitHandler(
    ICurrentTenantContext context,
    IIntegrationOperationRepository operations,
    ITenantAccessRepository tenants,
    JobService jobs,
    IUnitOfWork uow,
    IValidator<ReceiveDebitPayload> validator,
    TimeProvider clock,
    ILogger<ReceiveInboundDebitHandler> logger) : IRequestHandler<ReceiveInboundDebitCommand, NotificationResult<ReceiveInboundDebitReceipt>>
{
    public const string OperationType = "FINANCIAL_DEBIT_INBOUND";

    public async Task<NotificationResult<ReceiveInboundDebitReceipt>> Handle(
        ReceiveInboundDebitCommand request,
        CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (!Guid.TryParse(context.TenantId, out var tenantId) || tenantId == Guid.Empty
            || !await tenants.IsTenantAvailableAsync(tenantId, token))
            return CreateFailureResult(ErrorCodes.Forbidden, "Sessão sem tenant autorizado e disponível.");
        if (request.Payload?.Debit is null || request.Payload.Customer is null)
            return CreateFailureResult(ErrorCodes.Validation, "Confira o contrato de entrada do débito.");
        var validation = await validator.ValidateAsync(request.Payload, token);
        if (!validation.IsValid)
            return NotificationResult<ReceiveInboundDebitReceipt>.Failure(validation.Errors.Select(failure =>
                new NotificationError(ErrorCodes.Validation, failure.ErrorMessage,
                    string.Join('.', failure.PropertyName.Split('.').Select(JsonNamingPolicy.CamelCase.ConvertName)))).ToArray());
        var inboundDebitPayload = request.Payload;
        var requestFingerprint = InboundDebitFingerprint.Compute(inboundDebitPayload);
        var existingOperation = await operations.FindAsync(tenantId, OperationType, inboundDebitPayload.Debit.ExternalId, token);
        if (existingOperation is not null)
            return Replay(existingOperation, requestFingerprint);
        var operationId = Guid.NewGuid();
        var operationResult = await uow.ExecuteAsync(
            async cancellation =>
        {
                await operations.LockAsync(tenantId, OperationType, inboundDebitPayload.Debit.ExternalId, cancellation);
                var existingOperationInsideLock = await operations.FindAsync(
                    tenantId,
                    OperationType,
                    inboundDebitPayload.Debit.ExternalId,
                    cancellation);
                if (existingOperationInsideLock is not null)
                    return Replay(existingOperationInsideLock, requestFingerprint);
                var jobId = await jobs.CreateAsync(tenantId, new ReceiveDebitJob(inboundDebitPayload), cancellation);
                var inboundOperation = new IntegrationOperation(operationId, tenantId, OperationType, inboundDebitPayload.Debit.ExternalId,
                    requestFingerprint, jobId, null, null, clock.GetUtcNow(), null);
                await operations.AddAsync(inboundOperation, cancellation);
                var commitResult = await uow.CommitAsync(cancellation);
                if (commitResult.IsFailure)
                    return NotificationResult<ReceiveInboundDebitReceipt>.Failure(commitResult.Errors);
                logger.LogInformation(
                    "InboundDebitAccepted OperationId={OperationId} TenantId={TenantId} JobId={JobId}",
                    operationId,
                    tenantId,
                    jobId);
                return NotificationResult<ReceiveInboundDebitReceipt>.Success(new(operationId, true));
            },
            token);
        if (operationResult.Errors.Any(error => error.Code == ErrorCodes.IntegrationOperationConflict))
        {
            // ExecuteAsync has already rolled back and released its transaction.
            var existingOperationAfterConflict = await operations.FindAsync(tenantId, OperationType, inboundDebitPayload.Debit.ExternalId, token);
            if (existingOperationAfterConflict is null)
                throw new InvalidOperationException("The confirmed integration operation could not be reloaded.");
            return Replay(existingOperationAfterConflict, requestFingerprint);
        }
        return operationResult;
    }

    private NotificationResult<ReceiveInboundDebitReceipt> Replay(
        IntegrationOperation operation,
        string requestFingerprint)
    {
        var hasSameRequestFingerprint = operation.RequestFingerprint == requestFingerprint;
        logger.LogInformation(
            "InboundDebitReplay OperationId={OperationId} TenantId={TenantId} Equivalent={Equivalent}",
            operation.Id,
            operation.TenantId,
            hasSameRequestFingerprint);
        return hasSameRequestFingerprint ? NotificationResult<ReceiveInboundDebitReceipt>.Success(new(operation.Id, false))
            : CreateFailureResult(
                ErrorCodes.Conflict,
                "Identificador externo já recebido com uma solicitação diferente.");
    }

    private static NotificationResult<ReceiveInboundDebitReceipt> CreateFailureResult(
        string code,
        string message) =>
        NotificationResult<ReceiveInboundDebitReceipt>.Failure(new NotificationError(code, message));
}
