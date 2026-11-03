using System.Globalization;
using MediatR;
using YallaFinance.Application.Common.Results;
using YallaFinance.Application.Common.Transactions;
using YallaFinance.Application.Features.Charges.CreateDirectCharge;
using YallaFinance.Application.Features.Charges.IntegrateCharge;
using YallaFinance.Application.Features.FinancialCatalog;
using YallaFinance.Application.Repositories.Categories;
using YallaFinance.Application.Repositories.Categories.Contracts;
using YallaFinance.Application.Repositories.Charges;
using YallaFinance.Application.Repositories.CustomerContracts;
using YallaFinance.Application.Repositories.Customers;
using YallaFinance.Application.Repositories.Customers.Contracts;
using YallaFinance.Application.Repositories.Debits;
using YallaFinance.Application.Repositories.Fleet;
using YallaFinance.Application.Repositories.Fleet.Contracts;
using YallaFinance.Application.Services.Charges;
using YallaFinance.Application.Services.Financial;
using YallaFinance.Application.Services.Financial.Contracts;
using YallaFinance.Domain.Charges;

namespace YallaFinance.Application.Features.Charges.CreateCharge;

public sealed class ChargeCreationService(
    IChargeRepository charges,
    ICategoryRepository categories,
    ICustomerRepository customers,
    ICustomerContractRepository contracts,
    IDebtRepository debts,
    IFleetMovementRepository movements,
    IPublisher publisher,
    IUnitOfWork unitOfWork,
    ChargeProcessingQueries processing,
    IFinancialIntegrationService? financial = null)
    : IChargeCreationService
{
    public async Task<NotificationResult<ChargeDetailsResult>> CreateDirectAsync(
        string tenantId,
        CreateDirectChargeCommand request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!Guid.TryParse(tenantId, out var tenant) || tenant == Guid.Empty)
            return DirectFailure(ErrorCodes.Unauthorized, "Sessão sem empresa ativa válida.");

        var validation = await new CreateDirectChargeCommandValidator().ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return NotificationResult<ChargeDetailsResult>.Failure(validation.Errors.Select(
                error => new NotificationError(ErrorCodes.Validation, error.ErrorMessage)));

        var directRequestHash = DirectChargeFingerprint.Hash(tenant, request);
        var priorCharge = await charges.FindByIdempotencyKeyAsync(tenant, request.IdempotencyKey!, cancellationToken);
        if (priorCharge is not null)
        {
            if (priorCharge.Source != ChargeSource.Direct.ToString() || priorCharge.RequestHash != directRequestHash)
                return DirectFailure(ErrorCodes.Conflict, "Chave idempotente utilizada com outro conteúdo.");
            return await CompleteCreationAsync(
                tenantId,
                NotificationResult<(Guid ChargeId, bool Created)>.Success((priorCharge.Id, false)),
                cancellationToken);
        }

        if (financial is null)
            return DirectFailure(ErrorCodes.ServiceUnavailable, "Consulta financeira indisponível para validar a cobrança direta.");

        // Provider calls remain outside the local database transaction.
        var customerLookup = await financial.FindCustomersByDocumentAsync(
            new(tenant, request.CustomerDocument),
            cancellationToken);
        if (!customerLookup.Success)
            return NotificationResult<ChargeDetailsResult>.Failure(
                FinancialCatalogResults.ToNotification(customerLookup).Errors);
        if (customerLookup.ConnectionId is null || customerLookup.Items.Count != 1)
            return DirectFailure(
                ErrorCodes.Validation,
                customerLookup.Items.Count == 0
                    ? "Cliente não encontrado no sistema financeiro. Consulte novamente."
                    : "A consulta do cliente não identificou um único cadastro. Consulte novamente.");

        var providerCustomer = customerLookup.Items[0];
        if (YallaFinance.Domain.Customers.CustomerDocument.Normalize(providerCustomer.Document)
                != request.CustomerDocument
            || providerCustomer.Name.Trim() != request.CustomerName.Trim()
            || providerCustomer.DocumentType != request.CustomerDocumentType)
            return DirectFailure(ErrorCodes.Validation, "Os dados do cliente mudaram. Consulte o documento novamente.");
        if ((!string.IsNullOrWhiteSpace(providerCustomer.Email)
                && providerCustomer.Email.Trim() != request.CustomerEmail?.Trim())
            || (!string.IsNullOrWhiteSpace(providerCustomer.Phone)
                && providerCustomer.Phone.Trim() != request.CustomerPhone?.Trim()))
            return DirectFailure(
                ErrorCodes.Validation,
                "E-mail ou telefone da integração não pode ser alterado. Consulte o cliente novamente.");

        var costCenterName = await FindCostCenterNameAsync(
            tenant,
            customerLookup.ConnectionId.Value,
            request.CostCenterReference,
            cancellationToken);
        if (costCenterName.IsFailure)
            return NotificationResult<ChargeDetailsResult>.Failure(costCenterName.Errors);

        var preparation = await unitOfWork.ExecuteAsync(async ct =>
        {
            await charges.AcquireIdempotencyLockAsync(tenant, request.IdempotencyKey!, ct);
            var replay = await charges.FindByIdempotencyKeyAsync(tenant, request.IdempotencyKey!, ct);
            if (replay is not null)
            {
                if (replay.Source != ChargeSource.Direct.ToString() || replay.RequestHash != directRequestHash)
                    return CreateFailure(ErrorCodes.Conflict, "Chave idempotente utilizada com outro conteúdo.");
                return NotificationResult<(Guid ChargeId, bool Created)>.Success((replay.Id, false));
            }

            var currentUtcTime = DateTime.UtcNow;
            var today = GetTodayInBusinessTimeZone(currentUtcTime);
            if (request.DueDate < today || request.Items.Any(item => item.OccurrenceDate > today))
                return CreateFailure(
                    ErrorCodes.Validation,
                    "Confira o vencimento e a ocorrência dos débitos; são aplicadas as regras atuais de datas.");

            var normalizedContractNumber = request.Contract.Trim();
            await customers.AcquireDocumentLockAsync(tenant, request.CustomerDocument, ct);
            await contracts.AcquireInboundContractNumberLockAsync(tenant, normalizedContractNumber, ct);
            var persistedCustomers = await customers.FindLocalByDocumentForChargeAsync(
                tenant,
                request.CustomerDocument,
                ct);
            var persistedContracts = await contracts.FindForChargeAsync(tenant, normalizedContractNumber, ct);
            var directCategoryIds = request.Items
                .Select(item => Guid.Parse(item.CategoryId))
                .Distinct()
                .ToArray();
            var persistedCategories = await categories.FindActiveByIdsAsync(tenant, directCategoryIds, ct);
            var directCharge = BuildDirectCharge(
                tenant,
                request,
                providerCustomer,
                costCenterName.Value!,
                persistedCustomers,
                persistedContracts,
                persistedCategories,
                currentUtcTime);
            if (directCharge.IsFailure)
                return NotificationResult<(Guid ChargeId, bool Created)>.Failure(directCharge.Errors);

            var graph = directCharge.Value!;
            if (graph.CustomerToAdd is not null)
                await customers.AddForChargeAsync(graph.CustomerToAdd, ct);
            else
                await customers.UpdateForChargeAsync(graph.Customer, ct);
            if (graph.ContractToAdd is not null)
                await contracts.AddForChargeAsync(graph.ContractToAdd, ct);
            else
                await contracts.UpdateForChargeAsync(graph.Contract, ct);
            foreach (var debt in graph.Debts)
                await debts.AddForChargeAsync(debt, ct);
            await charges.AddCreationAsync(graph.Charge, ct);

            var commit = await unitOfWork.CommitAsync(ct);
            return commit.IsSuccess
                ? NotificationResult<(Guid ChargeId, bool Created)>.Success((graph.Charge.Id, true))
                : CreateFailure(
                    ErrorCodes.Conflict,
                    "Não foi possível concluir a gravação. Nenhum job foi agendado. Tente novamente com a mesma chave.");
        }, cancellationToken);

        return await CompleteCreationAsync(tenantId, preparation, cancellationToken);
    }

    public async Task<NotificationResult<ChargeDetailsResult>> CreateAsync(
        string tenantId,
        CreateChargeCommand request,
        CancellationToken cancellationToken)
    {
        var preparation = await unitOfWork.ExecuteAsync(async ct =>
        {
            var preparedCharge = await PrepareChargeAsync(tenantId, request, ct);
            if (preparedCharge.IsFailure)
                return preparedCharge;

            var commit = await unitOfWork.CommitAsync(ct);
            return commit.IsSuccess
                ? preparedCharge
                : CreateFailure(ErrorCodes.Conflict, "Débito ou chave idempotente já utilizado.");
        }, cancellationToken);

        return await CompleteCreationAsync(tenantId, preparation, cancellationToken);
    }

    private async Task<NotificationResult<(Guid ChargeId, bool Created)>> PrepareChargeAsync(
        string tenantId,
        CreateChargeCommand request,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(tenantId, out var tenant) || tenant == Guid.Empty)
            return CreateFailure(ErrorCodes.Unauthorized, "Sessão sem tenant válido.");
        if (request.Source is not (ChargeSource.Selected or ChargeSource.Standalone)
            || string.IsNullOrWhiteSpace(request.IdempotencyKey)
            || !Guid.TryParse(request.CustomerId, out var customerId)
            || (request.Source == ChargeSource.Selected && request.DebtIds.Count == 0)
            || request.DebtIds.Any(debtId => !Guid.TryParse(debtId, out _))
            || request.Amount <= 0
            || request.Amount > 9_999_999_999_999_999.99m
            || decimal.Round(request.Amount, 2) != request.Amount)
            return CreateFailure(ErrorCodes.Validation, "Confira os dados da cobrança.");

        var debtIds = request.DebtIds.Select(Guid.Parse).Order().ToArray();
        if (debtIds.Distinct().Count() != debtIds.Length)
            return CreateFailure(ErrorCodes.Validation, "Não duplique débitos na seleção.");
        if (request.Source == ChargeSource.Standalone
            && (request.DebtIds.Count != 0
                || !Guid.TryParse(request.SelectedMovementId, out _)
                || !Guid.TryParse(request.CategoryId, out _)))
            return CreateFailure(ErrorCodes.Validation, "Informe categoria e movimentação válidas para a avulsa.");

        var requestHash = ChargeCommandFingerprint.Hash(tenant, request);
        await charges.AcquireIdempotencyLockAsync(tenant, request.IdempotencyKey!, cancellationToken);
        var priorCharge = await charges.FindByIdempotencyKeyAsync(
            tenant,
            request.IdempotencyKey!,
            cancellationToken);
        if (priorCharge is not null)
        {
            if (priorCharge.RequestHash != requestHash
                && priorCharge.RequestHash != ChargeCommandFingerprint.LegacyHash(tenant, request))
                return CreateFailure(ErrorCodes.Conflict, "Chave idempotente utilizada com outro conteúdo.");
            return NotificationResult<(Guid ChargeId, bool Created)>.Success((priorCharge.Id, false));
        }

        var today = CreateChargeCommandValidator.Today;
        if (request.DueDate < today)
            return CreateFailure(ErrorCodes.Validation, "Vencimento anterior a hoje.");

        if (request.Source == ChargeSource.Standalone)
        {
            var standaloneDebt = await CreateStandaloneDebtAsync(
                tenant,
                customerId,
                request,
                today,
                cancellationToken);
            if (standaloneDebt.IsFailure)
                return NotificationResult<(Guid ChargeId, bool Created)>.Failure(standaloneDebt.Errors);

            var debt = standaloneDebt.Value!;
            var chargeId = Guid.NewGuid();
            var chargeCreatedAt = DateTime.UtcNow;
            await charges.AddCreationAsync(new(
                chargeId,
                tenant,
                debt.Amount,
                request.DueDate,
                request.CustomerNote,
                request.Source.ToString(),
                request.IdempotencyKey!,
                requestHash,
                chargeCreatedAt,
                [new(Guid.NewGuid(), debt.Id, debt.Amount, 1)]), cancellationToken);
            await debts.MarkInCollectionAsync(tenant, [debt.Id], chargeCreatedAt, cancellationToken);
            return NotificationResult<(Guid ChargeId, bool Created)>.Success((chargeId, true));
        }

        var lockedDebts = await debts.LockAndLoadForChargeAsync(tenant, debtIds, cancellationToken);
        if (lockedDebts.Count != debtIds.Length)
            return CreateFailure(ErrorCodes.NotFound, "Um ou mais débitos não foram encontrados.");
        if (lockedDebts.Any(debt => debt.Status != "Open"
                || debt.ContractTenantId != tenant
                || debt.CustomerTenantId != tenant
                || debt.CustomerId != customerId
                || !debt.ContractActive
                || !debt.CustomerActive
                || !debt.CategoryActive
                || debt.CategoryTenantId != tenant)
            || lockedDebts.Sum(debt => debt.Amount) != request.Amount)
            return CreateFailure(
                ErrorCodes.BusinessRule,
                "Selecione débitos em aberto do mesmo cliente e confira o total.");
        if (await charges.ContainsAnyDebtAsync(tenant, debtIds, cancellationToken))
            return CreateFailure(ErrorCodes.Conflict, "Um ou mais débitos já pertencem a uma cobrança.");

        var createdAt = DateTime.UtcNow;
        var selectedChargeId = Guid.NewGuid();
        var selectedCharge = new ChargeCreationData(
            selectedChargeId,
            tenant,
            lockedDebts.Sum(debt => debt.Amount),
            request.DueDate,
            request.CustomerNote,
            request.Source.ToString(),
            request.IdempotencyKey!,
            requestHash,
            createdAt,
            lockedDebts.Select((debt, ordinal) => new ChargeCreationItemData(
                Guid.NewGuid(),
                debt.Id,
                debt.Amount,
                ordinal + 1)).ToArray());
        await charges.AddCreationAsync(selectedCharge, cancellationToken);
        await debts.MarkInCollectionAsync(tenant, debtIds, createdAt, cancellationToken);
        return NotificationResult<(Guid ChargeId, bool Created)>.Success((selectedChargeId, true));
    }

    private async Task<NotificationResult<ChargeDebtData>> CreateStandaloneDebtAsync(
        Guid tenantId,
        Guid customerId,
        CreateChargeCommand request,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(request.CategoryId, out var categoryId)
            || request.OccurrenceDate == default
            || request.OccurrenceDate > today
            || string.IsNullOrWhiteSpace(request.DescriptionDetail)
            || request.DebtIds.Count != 0)
            return CreateFailure<ChargeDebtData>(ErrorCodes.Validation, "Confira os dados do débito avulso.");

        var tenantIdText = tenantId.ToString();
        var activeCategory = (await categories.FindActiveByIdsAsync(tenantId, [categoryId], cancellationToken))
            .SingleOrDefault(category => category.Id == categoryId.ToString());
        if (activeCategory is null)
            return CreateFailure<ChargeDebtData>(ErrorCodes.Forbidden, "Categoria indisponível para o tenant.");

        var externalCustomers = (await customers.SearchExternalAsync(tenantIdText, "", cancellationToken))
            .Where(customer => customer.TenantId == tenantIdText && customer.CustomerId == customerId.ToString())
            .ToArray();
        if (externalCustomers.Length != 1)
            return CreateFailure<ChargeDebtData>(ErrorCodes.NotFound, "Cliente não encontrado na origem do tenant ativo.");
        var externalCustomer = externalCustomers[0];

        var matchingMovements = (await movements.FindAsync(
                tenantIdText,
                customerId.ToString(),
                request.OccurrenceDate,
                cancellationToken))
            .Where(movement => movement.Id == request.SelectedMovementId
                && movement.TenantId == tenantIdText
                && movement.CustomerId == customerId.ToString()
                && movement.OccurrenceDate == request.OccurrenceDate)
            .ToArray();
        if (matchingMovements.Length != 1 || string.IsNullOrWhiteSpace(matchingMovements[0].Contract))
            return CreateFailure<ChargeDebtData>(ErrorCodes.BusinessRule, "Selecione uma movimentação do cliente na data informada.");
        var movement = matchingMovements[0];

        var normalizedDocument = YallaFinance.Domain.Customers.CustomerDocument.Normalize(externalCustomer.TaxId);
        if (string.IsNullOrWhiteSpace(externalCustomer.Name)
            || !YallaFinance.Domain.Customers.CustomerDocument.HasValidFormat(externalCustomer.TaxId,
                normalizedDocument.Length == 11 ? "CPF" : "CNPJ"))
            return CreateFailure<ChargeDebtData>(ErrorCodes.Validation, "Cadastro externo sem nome ou documento válido.");

        await customers.AcquireCustomerLockAsync(customerId, cancellationToken);
        await customers.AcquireDocumentLockAsync(tenantId, normalizedDocument, cancellationToken);
        var localCustomer = await customers.FindLocalForChargeAsync(tenantId, customerId, cancellationToken);
        if (localCustomer is not null
            && (localCustomer.TenantId != tenantId
                || !localCustomer.Active
                || YallaFinance.Domain.Customers.CustomerDocument.Normalize(localCustomer.Document) != normalizedDocument))
            return CreateFailure<ChargeDebtData>(ErrorCodes.Conflict, "Cliente local incompatível com o cadastro externo.");

        var currentUtcTime = DateTime.UtcNow;
        if (localCustomer is null)
        {
            var localCustomersWithDocument = await customers.FindLocalByDocumentForChargeAsync(
                tenantId,
                normalizedDocument,
                cancellationToken);
            if (localCustomersWithDocument.Count > 0)
                return CreateFailure<ChargeDebtData>(ErrorCodes.Conflict, "Cliente já cadastrado com outro identificador local.");

            await customers.AddForChargeAsync(new(
                customerId,
                tenantId,
                externalCustomer.Name,
                externalCustomer.TaxId,
                normalizedDocument.Length == 11 ? "CPF" : "CNPJ",
                string.IsNullOrWhiteSpace(externalCustomer.Email) ? null : externalCustomer.Email,
                string.IsNullOrWhiteSpace(externalCustomer.Phone) ? null : externalCustomer.Phone,
                true,
                currentUtcTime,
                currentUtcTime), cancellationToken);
        }

        var contractNumber = movement.Contract;
        await contracts.AcquireInboundContractNumberLockAsync(tenantId, contractNumber, cancellationToken);
        await contracts.AcquireContractLockAsync(tenantId, contractNumber, cancellationToken);
        var localContracts = await contracts.FindForChargeAsync(tenantId, contractNumber, cancellationToken);
        if (localContracts.Count > 1 || localContracts.Any(contract =>
                contract.CustomerId != customerId
                || !contract.Active
                || (contract.Plate ?? "") != movement.Plate
                || (contract.Product ?? "") != movement.Product
                || (contract.Fleet ?? "") != movement.Fleet))
            return CreateFailure<ChargeDebtData>(
                ErrorCodes.Conflict,
                "Contrato local ambíguo ou incompatível com a movimentação externa.");

        var contract = localContracts.SingleOrDefault();
        if (contract is null)
        {
            var contractId = Guid.NewGuid();
            await contracts.AddForChargeAsync(new(
                contractId,
                tenantId,
                customerId,
                contractNumber,
                movement.Plate,
                movement.Product,
                movement.Fleet,
                true,
                currentUtcTime,
                currentUtcTime), cancellationToken);
            contract = new ChargeContractRecord(
                contractId,
                tenantId,
                customerId,
                contractNumber,
                movement.Plate,
                movement.Product,
                movement.Fleet,
                true);
        }

        var debt = new ChargeDebtData(
            Guid.NewGuid(),
            tenantId,
            contract.Id,
            categoryId,
            request.Amount,
            request.OccurrenceDate,
            request.DueDate,
            FormatDebtDescription(activeCategory.Name, request.DescriptionDetail, movement.Contract),
            "Open",
            "Yalla Finance",
            currentUtcTime,
            currentUtcTime);
        await debts.AddForChargeAsync(debt, cancellationToken);
        return NotificationResult<ChargeDebtData>.Success(debt);
    }

    private static NotificationResult<DirectChargeGraph> BuildDirectCharge(
        Guid tenantId,
        CreateDirectChargeCommand request,
        FinancialCustomerSummary providerCustomer,
        string costCenterName,
        IReadOnlyList<ChargeCustomerRecord> persistedCustomers,
        IReadOnlyList<ChargeContractRecord> persistedContracts,
        IReadOnlyList<CategoryResponse> persistedCategories,
        DateTime now)
    {
        static NotificationResult<DirectChargeGraph> Failure(string code, string message) =>
            NotificationResult<DirectChargeGraph>.Failure(new NotificationError(code, message));

        var eligibleCustomers = persistedCustomers
            .Where(candidate => candidate.Active
                && candidate.TenantId == tenantId
                && candidate.DocumentType == request.CustomerDocumentType
                && YallaFinance.Domain.Customers.CustomerDocument.Normalize(candidate.Document) == request.CustomerDocument)
            .OrderBy(candidate => candidate.CreatedAt)
            .ThenBy(candidate => candidate.Id)
            .ToArray();
        var linkedCustomerId = persistedContracts.Count == 1
            ? persistedContracts[0].CustomerId
            : (Guid?)null;
        var persistedCustomer = eligibleCustomers.FirstOrDefault(candidate => candidate.Id == linkedCustomerId)
            ?? eligibleCustomers.FirstOrDefault()
            ?? persistedCustomers.FirstOrDefault();
        if (persistedCustomer is not null
            && (!persistedCustomer.Active
                || persistedCustomer.TenantId != tenantId
                || persistedCustomer.DocumentType != request.CustomerDocumentType
                || YallaFinance.Domain.Customers.CustomerDocument.Normalize(persistedCustomer.Document) != request.CustomerDocument))
            return Failure(
                ErrorCodes.BusinessRule,
                "O cliente local está inativo ou seus dados não correspondem ao documento informado.");

        var customerWasCreated = persistedCustomer is null;
        var customerId = persistedCustomer?.Id ?? Guid.NewGuid();
        var customerName = persistedCustomer?.Name ?? providerCustomer.Name.Trim();
        var customerDocument = persistedCustomer?.Document ?? request.CustomerDocument;
        var customerDocumentType = persistedCustomer?.DocumentType ?? request.CustomerDocumentType;
        var customerEmail = persistedCustomer?.Email;
        var customerPhone = persistedCustomer?.Phone;

        if (persistedContracts.Count > 1)
            return Failure(ErrorCodes.Conflict, "O número do contrato identifica mais de um cadastro local.");

        var persistedContract = persistedContracts.SingleOrDefault();
        if (persistedContract is not null
            && (!persistedContract.Active
                || persistedContract.TenantId != tenantId
                || persistedContract.Number != request.Contract.Trim()
                || persistedContract.CustomerId != customerId
                || (!string.IsNullOrWhiteSpace(persistedContract.Plate)
                    && NormalizePlate(persistedContract.Plate) != request.Plate)
                || !Compatible(persistedContract.Product, request.ContractProduct)
                || !Compatible(persistedContract.Fleet, request.ContractFleet)))
            return Failure(
                ErrorCodes.Conflict,
                "O contrato está inativo ou não corresponde ao cliente, placa, produto ou frota informados.");

        var categoryIds = request.Items.Select(item => Guid.Parse(item.CategoryId)).Distinct().ToArray();
        var chargeCategories = persistedCategories
            .Where(category => categoryIds.Contains(Guid.Parse(category.Id)))
            .ToArray();
        if (chargeCategories.Length != categoryIds.Length
            || chargeCategories.Any(category => !category.Active || category.TenantId != tenantId.ToString()))
            return Failure(
                ErrorCodes.Forbidden,
                "Uma ou mais categorias não estão disponíveis para esta empresa.");

        var email = Contact(providerCustomer.Email) ?? Contact(request.CustomerEmail);
        var phone = Contact(providerCustomer.Phone) ?? Contact(request.CustomerPhone);
        if (email is not null)
            customerEmail = email;
        if (phone is not null)
            customerPhone = phone;

        var customer = new ChargeCustomerData(
            customerId,
            tenantId,
            customerName,
            customerDocument,
            customerDocumentType,
            customerEmail,
            customerPhone,
            true,
            persistedCustomer?.CreatedAt ?? now,
            now,
            providerCustomer.Address);
        var contractWasCreated = persistedContract is null;
        var contractId = persistedContract?.Id ?? Guid.NewGuid();
        var contractData = persistedContract is null
            ? new ChargeContractData(
                contractId,
                tenantId,
                customerId,
                request.Contract.Trim(),
                request.Plate,
                Contact(request.ContractProduct),
                Contact(request.ContractFleet),
                true,
                now,
                now)
            : new ChargeContractData(
                contractId,
                tenantId,
                customerId,
                persistedContract.Number,
                string.IsNullOrWhiteSpace(persistedContract.Plate) ? request.Plate : persistedContract.Plate,
                persistedContract.Product ?? Contact(request.ContractProduct),
                persistedContract.Fleet ?? Contact(request.ContractFleet),
                true,
                now,
                now);

        var chargeId = Guid.NewGuid();
        var chargeDebts = new List<ChargeDebtData>(request.Items.Count);
        var chargeItems = new List<ChargeCreationItemData>(request.Items.Count);
        for (var index = 0; index < request.Items.Count; index++)
        {
            var item = request.Items[index];
            var categoryId = Guid.Parse(item.CategoryId);
            var category = chargeCategories.Single(value => value.Id == categoryId.ToString());
            var debtId = Guid.NewGuid();
            chargeDebts.Add(new(
                debtId,
                tenantId,
                contractId,
                categoryId,
                item.Amount,
                item.OccurrenceDate,
                request.DueDate,
                FormatDebtDescription(category.Name, item.DescriptionDetail.Trim(), request.Contract.Trim()),
                "InCollection",
                "Yalla Finance",
                now,
                now));
            chargeItems.Add(new(
                Guid.NewGuid(),
                debtId,
                item.Amount,
                index + 1,
                request.CostCenterReference,
                costCenterName.Trim()));
        }

        var charge = new ChargeCreationData(
            chargeId,
            tenantId,
            request.Items.Sum(item => item.Amount),
            request.DueDate,
            request.CustomerNote,
            ChargeSource.Direct.ToString(),
            request.IdempotencyKey!,
            DirectChargeFingerprint.Hash(tenantId, request),
            now,
            chargeItems);
        return NotificationResult<DirectChargeGraph>.Success(new(
            customer,
            customerWasCreated ? customer : null,
            contractData,
            contractWasCreated ? contractData : null,
            chargeDebts,
            charge));
    }

    private async Task<NotificationResult<string>> FindCostCenterNameAsync(
        Guid tenantId,
        Guid connectionId,
        string costCenterReference,
        CancellationToken cancellationToken)
    {
        int skip = 0;
        int? expectedCount = null;
        string? costCenterName = null;
        var references = new HashSet<string>(StringComparer.Ordinal);
        while (true)
        {
            var page = await financial!.ListCostCentersAsync(new(tenantId, skip, 500), cancellationToken);
            if (!page.Success)
                return NotificationResult<string>.Failure(FinancialCatalogResults.ToNotification(page).Errors);
            if (page.ConnectionId != connectionId
                || page.TotalCount < skip + page.Items.Count
                || expectedCount is not null && expectedCount != page.TotalCount
                || page.Items.Count == 0 && skip < page.TotalCount)
                return CreateFailure<string>(
                    ErrorCodes.ServiceUnavailable,
                    "A conexão ou o catálogo financeiro mudou. Atualize os dados e consulte novamente.");

            expectedCount = page.TotalCount;
            foreach (var costCenter in page.Items)
            {
                if (!references.Add(costCenter.ExternalReference))
                    return CreateFailure<string>(
                        ErrorCodes.ServiceUnavailable,
                        "O catálogo de centros de custo retornou referências duplicadas. Tente novamente.");
                if (costCenter.ExternalReference == costCenterReference)
                    costCenterName = costCenter.Name;
            }

            skip += page.Items.Count;
            if (skip >= page.TotalCount)
                break;
        }

        return string.IsNullOrWhiteSpace(costCenterName)
            ? CreateFailure<string>(ErrorCodes.Validation, "O centro de custo selecionado não está disponível. Atualize os dados.")
            : NotificationResult<string>.Success(costCenterName);
    }

    private async Task<NotificationResult<ChargeDetailsResult>> CompleteCreationAsync(
        string tenantId,
        NotificationResult<(Guid ChargeId, bool Created)> prepared,
        CancellationToken cancellationToken)
    {
        if (prepared.IsFailure)
            return NotificationResult<ChargeDetailsResult>.Failure(prepared.Errors);

        var chargeId = prepared.Value.ChargeId.ToString();
        if (!prepared.Value.Created)
        {
            var replayedCharge = await charges.GetAsync(tenantId, chargeId, CancellationToken.None);
            var enrichedCharge = await processing.EnrichAsync(replayedCharge!, CancellationToken.None);
            if (enrichedCharge.Source == ChargeSource.Direct && enrichedCharge.Processing is null)
                enrichedCharge = enrichedCharge with
                {
                    Processing = new(null, "NotScheduled", ErrorCode: "ChargeSchedulingFailed")
                };
            return NotificationResult<ChargeDetailsResult>.Success(enrichedCharge);
        }

        // The local transaction is committed and disposed before scheduling the integration Job.
        var chargeCreatedNotification = new ChargeCreatedNotification(tenantId, chargeId);
        try
        {
            await publisher.Publish(chargeCreatedNotification, CancellationToken.None);
        }
        catch (Exception)
        {
            // The charge is committed. Do not expose raw errors or report enqueue success.
            chargeCreatedNotification.Processing = new(null, "NotScheduled", ErrorCode: "ChargeSchedulingFailed");
        }

        var persistedCharge = (await charges.GetAsync(
            tenantId,
            chargeCreatedNotification.ChargeId,
            CancellationToken.None))!;
        return NotificationResult<ChargeDetailsResult>.Success(persistedCharge with
        {
            Processing = chargeCreatedNotification.Processing ?? new(
                null,
                "NotScheduled",
                ErrorCode: "ChargeSchedulingFailed")
        });
    }

    private static DateOnly GetTodayInBusinessTimeZone(DateTime utcTime) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(
            new DateTimeOffset(utcTime),
            "America/Sao_Paulo").DateTime);

    private static string FormatDebtDescription(string category, string detail, string contract) =>
        $"{category.ToUpper(CultureInfo.GetCultureInfo("pt-BR"))} | {detail} | {contract}";

    private static string NormalizePlate(string value) => value.Replace("-", "").Trim().ToUpperInvariant();

    private static string? Contact(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool Compatible(string? current, string? requested) => string.IsNullOrWhiteSpace(current)
        || string.IsNullOrWhiteSpace(requested)
        || current.Trim() == requested.Trim();

    private static NotificationResult<(Guid ChargeId, bool Created)> CreateFailure(string code, string message) =>
        NotificationResult<(Guid ChargeId, bool Created)>.Failure(new NotificationError(code, message));

    private static NotificationResult<T> CreateFailure<T>(string code, string message) =>
        NotificationResult<T>.Failure(new NotificationError(code, message));

    private static NotificationResult<ChargeDetailsResult> DirectFailure(string code, string message) =>
        NotificationResult<ChargeDetailsResult>.Failure(new NotificationError(code, message));

    private sealed record DirectChargeGraph(
        ChargeCustomerData Customer,
        ChargeCustomerData? CustomerToAdd,
        ChargeContractData Contract,
        ChargeContractData? ContractToAdd,
        IReadOnlyList<ChargeDebtData> Debts,
        ChargeCreationData Charge);
}
