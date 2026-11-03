"use client";

import { useCallback, useEffect, useRef, useState, type ReactNode } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import {
  Box,
  Button,
  IconButton,
  MenuItem,
  Stack,
  TextField,
  Tooltip,
  Typography,
} from "@mui/material";
import AddOutlinedIcon from "@mui/icons-material/AddOutlined";
import ArrowBackOutlinedIcon from "@mui/icons-material/ArrowBackOutlined";
import CloseOutlinedIcon from "@mui/icons-material/CloseOutlined";
import DeleteOutlinedIcon from "@mui/icons-material/DeleteOutlined";
import EditOutlinedIcon from "@mui/icons-material/EditOutlined";
import FactCheckOutlinedIcon from "@mui/icons-material/FactCheckOutlined";
import PersonSearchOutlinedIcon from "@mui/icons-material/PersonSearchOutlined";
import PlaylistAddOutlinedIcon from "@mui/icons-material/PlaylistAddOutlined";
import SaveOutlinedIcon from "@mui/icons-material/SaveOutlined";
import SearchOutlinedIcon from "@mui/icons-material/SearchOutlined";
import TaskAltOutlinedIcon from "@mui/icons-material/TaskAltOutlined";
import RefreshOutlinedIcon from "@mui/icons-material/RefreshOutlined";
import { useToast } from "@/providers/ToastProvider";
import { FinancePageTitle } from "@/components/finance/FinancePageTitle";
import { CurrencyField } from "@/components/forms/CurrencyField";
import { DateField } from "@/components/forms/DateField";
import { TaxIdField } from "@/components/forms/TaxIdField";
import { PlateField } from "@/components/forms/PlateField";
import { PrototypePanel } from "@/components/ui/PrototypePanel";
import {
  SelectablePaginatedTable,
  type SelectableTableColumn,
} from "@/components/ui/SelectablePaginatedTable";
import { ChargeStepper } from "@/features/charges/components/ChargeStepper";
import { ChargeResult } from "@/features/charges/components/ChargeResult";
import { chargesApi } from "@/features/charges/api/charges.client";
import type { DirectChargeReceipt } from "@/features/charges/types/charge.types";
import { useSessionContext } from "@/features/auth/context/SessionProvider";
import {
  useSubmitDirectCharge,
  useRefreshDirectChargeStatus,
} from "@/features/charges/hooks/useCreateCharge";
import { LoadingState } from "@/components/ui/LoadingState";
import { ApiError } from "@/services/api/api-error";
import { financeTokens } from "@/theme/designTokens";
import { formatDate } from "@/utils/date";
import { formatPlate } from "@/utils/plate";
import { createSecureUuidV4 } from "@/utils/secureUuid";
import {
  buildDirectChargeRequest,
  composeDirectDebtDescription,
  createDirectChargeBasics,
  createDirectDebtEditor,
  formatDirectAmount,
  getDirectChargeTotal,
  validateDirectBasics,
  validateDirectDebt,
  type DirectChargeBasics,
  type DirectDebtDraft,
} from "./directChargeDraft";
import { useCategories } from "@/features/catalogs/hooks/useCategories";
import {
  useFinancialCostCenters,
  useFinancialCustomerLookup,
} from "@/features/catalogs/hooks/useFinancialCatalog";
import type { FinancialCustomer } from "@/features/catalogs/types/catalog.types";

const stepLabels = ["Cliente", "Débitos", "Revisão", "Confirmação"];
const debtsPerPage = 3;
const fieldGap = "1rem";
const panelInset = "1.25rem";
const actionGap = "0.75rem";
const stepIcons = [
  <PersonSearchOutlinedIcon key="customer" fontSize="small" />,
  <PlaylistAddOutlinedIcon key="debts" fontSize="small" />,
  <FactCheckOutlinedIcon key="review" fontSize="small" />,
  <TaskAltOutlinedIcon key="confirmation" fontSize="small" />,
];

// Single form as requested, retaining the existing compact fields and spacing.
const chargeGrid = {
  display: "grid",
  gap: fieldGap,
  gridTemplateColumns: "minmax(0, 1fr)",
  alignItems: "start",
} as const;

// Existing form spacing, with fields filling the available container width.
const fieldGrid = {
  display: "grid",
  gap: fieldGap,
  width: "100%",
  gridTemplateColumns: {
    xs: "minmax(0, 1fr)",
    md: "repeat(3, minmax(0, 1fr))",
  },
} as const;

const buttonRow = {
  display: "flex",
  flexWrap: "wrap",
  gap: actionGap,
  justifyContent: "flex-end",
} as const;

export function DirectChargeClient() {
  const { tenantId, contextId } = useSessionContext();
  return <DirectChargeForm key={JSON.stringify([tenantId, contextId])} />;
}

function DirectChargeForm() {
  const router = useRouter();
  const { showToast } = useToast();
  const notifyError = useCallback(
    (message: string) => {
      if (message) showToast({ message, severity: "error" });
    },
    [showToast],
  );
  const submission = useSubmitDirectCharge();
  const statusRefresh = useRefreshDirectChargeStatus();
  const statusController = useRef<AbortController | null>(null);
  const statusInFlight = useRef(false);
  const categoriesQuery = useCategories();
  const centersQuery = useFinancialCostCenters();
  const lookup = useFinancialCustomerLookup();
  const categories = (categoriesQuery.data ?? []).filter((category) => category.active);
  const centers = centersQuery.data?.items ?? [];
  const [customer, setCustomer] = useState<FinancialCustomer | null>(null);
  const [customerConnectionId, setCustomerConnectionId] = useState("");
  const [refreshing, setRefreshing] = useState(false);
  const lookupController = useRef<AbortController | null>(null);
  const lookupVersion = useRef(0);
  const lookupInFlight = useRef(false);
  const [received, setReceived] = useState<DirectChargeReceipt | null>(null);
  const inFlight = useRef(false);
  const mounted = useRef(true);
  const attempt = useRef<{ snapshot: string; idempotencyKey: string } | null>(null);
  const pendingController = useRef<AbortController | null>(null);
  useEffect(() => {
    mounted.current = true;
    return () => {
      mounted.current = false;
      pendingController.current?.abort();
      lookupController.current?.abort();
      statusController.current?.abort();
    };
  }, []);
  const [basics, setBasics] = useState<DirectChargeBasics>(createDirectChargeBasics);
  const [debts, setDebts] = useState<DirectDebtDraft[]>([]);
  const [editor, setEditor] = useState(createDirectDebtEditor);
  const [step, setStep] = useState(0);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [page, setPage] = useState(1);
  const nextItemId = useRef(1);
  const total = getDirectChargeTotal(debts);
  const connectionMismatch = Boolean(
    customer && customerConnectionId !== centersQuery.data?.connectionId,
  );
  const customerFound = Boolean(customer) && !connectionMismatch;
  const catalogError = categoriesQuery.isError
    ? `Não foi possível carregar as categorias.${categoriesQuery.error instanceof ApiError ? ` ${categoriesQuery.error.message}` : ""}`
    : centersQuery.isError
      ? `Não foi possível carregar os centros de custo.${centersQuery.error instanceof ApiError ? ` ${centersQuery.error.message}` : ""}`
      : categoriesQuery.isSuccess && !categories.length
        ? "Nenhuma categoria ativa disponível."
        : centersQuery.isSuccess && !centers.length
          ? "Nenhum centro de custo disponível."
          : connectionMismatch
            ? "A conexão financeira mudou. Atualize os dados e consulte o cliente novamente."
            : "";
  const validCategory = (id: string) => categories.some((category) => category.id === id);
  const editorHasInput = Object.values(editor).some((value) => value.trim() !== "");
  const canContinueToDebts =
    categoriesQuery.isSuccess &&
    centersQuery.isSuccess &&
    !catalogError &&
    validateDirectBasics(basics, customerFound) === "" &&
    centers.some((center) => center.externalReference === basics.costCenterReference);
  const canSaveDebt =
    canContinueToDebts && validCategory(editor.categoryId) && validateDirectDebt(editor) === "";
  const canReviewCharge =
    canContinueToDebts &&
    !editorHasInput &&
    editingId === null &&
    debts.length > 0 &&
    debts.every((debt) => validCategory(debt.categoryId) && validateDirectDebt(debt) === "");

  const lastCatalogNotice = useRef("");
  useEffect(() => {
    const notice = catalogError
      ? `${catalogError}:${categoriesQuery.errorUpdatedAt}:${centersQuery.errorUpdatedAt}`
      : "";
    if (notice && notice !== lastCatalogNotice.current) notifyError(catalogError);
    lastCatalogNotice.current = notice;
  }, [catalogError, categoriesQuery.errorUpdatedAt, centersQuery.errorUpdatedAt, notifyError]);

  async function consultCustomer(automatic = false) {
    if (automatic && customerFound) return;
    if (lookupInFlight.current || !/^(\d{11}|\d{14})$/.test(basics.document)) return;
    lookupInFlight.current = true;
    const version = ++lookupVersion.current;
    const controller = new AbortController();
    lookupController.current = controller;
    setCustomer(null);
    try {
      const result = await lookup.mutateAsync({
        document: basics.document,
        signal: controller.signal,
      });
      if (!mounted.current || version !== lookupVersion.current) return;
      if (!result.items.length) notifyError("Cliente não encontrado no sistema financeiro.");
      else if (result.items.length !== 1)
        notifyError(
          "Mais de um cliente encontrado para o documento. Verifique o cadastro no sistema financeiro.",
        );
      else {
        setCustomer(result.items[0]);
        setCustomerConnectionId(result.connectionId);
        setBasics((current) => ({
          ...current,
          customerName: result.items[0].name,
          customerEmail: result.items[0].email?.trim() ?? "",
          customerPhone: result.items[0].phone?.trim() ?? "",
        }));
      }
    } catch (failure) {
      if (mounted.current && version === lookupVersion.current)
        notifyError(
          failure instanceof ApiError
            ? failure.message
            : "Não foi possível consultar o cliente. Tente novamente.",
        );
    } finally {
      lookupInFlight.current = false;
      lookupController.current = null;
    }
  }

  async function retryCatalogs() {
    setRefreshing(true);
    if (step !== 3) {
      setCustomer(null);
      setCustomerConnectionId("");
      clearReceipt();
      setBasics((current) => ({
        ...current,
        costCenter: "",
        costCenterReference: "",
        customerName: "",
        customerEmail: "",
        customerPhone: "",
      }));
    }
    try {
      await Promise.allSettled([categoriesQuery.refetch(), centersQuery.refetch()]);
    } finally {
      if (mounted.current) setRefreshing(false);
    }
  }

  async function refreshChargeStatus() {
    if (!received || statusInFlight.current) return;
    statusInFlight.current = true;
    const controller = new AbortController();
    statusController.current = controller;
    try {
      const status = await statusRefresh.mutateAsync({
        chargeId: received.id,
        signal: controller.signal,
      });
      if (mounted.current) {
        setReceived(status);
        showToast({ message: "Situação da cobrança atualizada.", severity: "success" });
      }
    } catch (failure) {
      if (mounted.current)
        notifyError(
          failure instanceof ApiError
            ? failure.message
            : "Não foi possível atualizar a situação da cobrança. Tente novamente.",
        );
    } finally {
      statusInFlight.current = false;
      statusController.current = null;
    }
  }

  function clearReceipt() {
    setReceived(null);
  }

  async function submitDirectCharge() {
    if (inFlight.current || received) return;
    if (!canReviewCharge) {
      notifyError("Confira os dados e conclua a edição dos débitos antes de enviar.");
      return;
    }
    inFlight.current = true;
    try {
      const payload = buildDirectChargeRequest(basics, debts);
      const snapshot = JSON.stringify(payload);
      const idempotencyKey =
        attempt.current?.snapshot === snapshot
          ? attempt.current.idempotencyKey
          : createSecureUuidV4();
      attempt.current = { snapshot, idempotencyKey };
      const controller = new AbortController();
      pendingController.current = controller;
      const receipt = await submission.mutateAsync({
        payload,
        idempotencyKey,
        signal: controller.signal,
      });
      if (mounted.current) {
        setReceived(receipt);
        setStep(3);
        router.replace(`/direct-charges/success?chargeId=${encodeURIComponent(receipt.id)}`);
      }
    } catch (failure) {
      if (mounted.current)
        notifyError(
          failure instanceof ApiError
            ? failure.message
            : "Não foi possível enviar a solicitação. Tente novamente.",
        );
    } finally {
      inFlight.current = false;
      pendingController.current = null;
    }
  }

  function updateBasics(field: keyof DirectChargeBasics, value: string) {
    clearReceipt();
    setBasics((current) => ({ ...current, [field]: value }));
    if (field === "document") {
      setBasics((current) => ({
        ...current,
        customerName: "",
        customerEmail: "",
        customerPhone: "",
      }));
      setCustomer(null);
      setCustomerConnectionId("");
      lookupVersion.current++;
      lookupController.current?.abort();
    }
  }

  function updateEditor(field: keyof typeof editor, value: string) {
    setEditor((current) => ({ ...current, [field]: value }));
  }

  function continueToDebts() {
    const validation = validateDirectBasics(basics, customerFound);
    notifyError(validation);
    if (!validation && canContinueToDebts) setStep(1);
  }

  function saveDebt() {
    const validation = validateDirectDebt(editor);
    notifyError(validation);
    if (validation || !canSaveDebt) return;
    clearReceipt();
    if (editingId !== null) {
      setDebts((current) =>
        current.map((debt) => (debt.id === editingId ? { ...editor, id: editingId } : debt)),
      );
    } else {
      const id = nextItemId.current++;
      setDebts((current) => [...current, { ...editor, id }]);
    }
    cancelDebtEditing();
  }

  function cancelDebtEditing() {
    setEditingId(null);
    setEditor(createDirectDebtEditor());
  }

  function editDebt(debt: DirectDebtDraft) {
    setEditor({
      categoryId: debt.categoryId,
      category: debt.category,
      occurrenceDate: debt.occurrenceDate,
      amount: debt.amount,
      detail: debt.detail,
    });
    setEditingId(debt.id);
  }

  function removeDebt(debt: DirectDebtDraft) {
    clearReceipt();
    setDebts((current) => current.filter((item) => item.id !== debt.id));
    if (editingId === debt.id) cancelDebtEditing();
  }

  function reviewCharge() {
    if (editingId !== null) return notifyError("Salve ou cancele a edição antes de revisar.");
    if (debts.length === 0) return notifyError("Adicione pelo menos um débito para revisar.");
    const validation =
      validateDirectBasics(basics, customerFound) ||
      (!canReviewCharge
        ? "Confira os dados e adicione ou limpe o débito em edição antes de revisar."
        : "") ||
      debts.map(validateDirectDebt).find(Boolean) ||
      "";
    notifyError(validation);
    if (!validation) {
      setPage(1);
      setStep(2);
    }
  }

  function startNewDirectCharge() {
    attempt.current = null;
    clearReceipt();
    setBasics(createDirectChargeBasics());
    setDebts([]);
    cancelDebtEditing();
    setCustomer(null);
    setCustomerConnectionId("");
    setPage(1);
    nextItemId.current = 1;
    setStep(0);
  }

  function renderTable(showDescription: boolean, editable: boolean) {
    const columns: SelectableTableColumn<DirectDebtDraft>[] = showDescription
      ? [
          { id: "category", header: "Categoria", render: (debt) => debt.category },
          {
            id: "description",
            header: "Descrição",
            render: (debt) => (
              <Box component="span" sx={{ overflowWrap: "anywhere" }}>
                {composeDirectDebtDescription(debt, basics.contract)}
              </Box>
            ),
            getSortValue: (debt) => composeDirectDebtDescription(debt, basics.contract),
          },
        ]
      : [
          { id: "category", header: "Categoria", render: (debt) => debt.category },
          {
            id: "detail",
            header: "Detalhe",
            render: (debt) => (
              <Box component="span" sx={{ overflowWrap: "anywhere" }}>
                {debt.detail}
              </Box>
            ),
            getSortValue: (debt) => debt.detail,
          },
        ];
    columns.push(
      {
        id: "occurrenceDate",
        header: "Ocorrência",
        render: (debt) => (
          <Box component="span" sx={{ whiteSpace: "nowrap" }}>
            {formatDate(debt.occurrenceDate)}
          </Box>
        ),
        getSortValue: (debt) => debt.occurrenceDate,
      },
      {
        id: "amount",
        header: "Valor",
        align: "right",
        render: (debt) => (
          <Box component="span" sx={{ whiteSpace: "nowrap" }}>
            {formatDirectAmount(debt.amount)}
          </Box>
        ),
        getSortValue: (debt) => Number(debt.amount),
      },
    );
    if (editable)
      columns.push({
        id: "actions",
        header: "Ações",
        sortable: false,
        align: "right",
        render: (debt) => (
          <Stack direction="row" spacing={0.5} sx={{ justifyContent: "flex-end" }}>
            <Tooltip title="Editar débito">
              <IconButton
                aria-label={`Editar débito ${debt.id}`}
                color="primary"
                size="small"
                onClick={() => editDebt(debt)}
              >
                <EditOutlinedIcon fontSize="small" />
              </IconButton>
            </Tooltip>
            <Tooltip title="Remover débito">
              <IconButton
                aria-label={`Remover débito ${debt.id}`}
                color="error"
                size="small"
                onClick={() => removeDebt(debt)}
              >
                <DeleteOutlinedIcon fontSize="small" />
              </IconButton>
            </Tooltip>
          </Stack>
        ),
      });
    return (
      <SelectablePaginatedTable
        rows={debts}
        columns={columns}
        getRowId={(debt) => debt.id}
        showSelection={false}
        selectedRows={[]}
        onSelectionChange={() => {}}
        paginationMode="client"
        tableLabel="Débitos da Cobrança Direta"
        headerSx={{ backgroundColor: financeTokens.colors.tableHeader }}
        emptyContent="Nenhum débito adicionado."
        pagination={{
          page: Math.min(page, Math.max(1, Math.ceil(debts.length / debtsPerPage))),
          pageSize: debtsPerPage,
          totalItems: debts.length,
          onPageChange: setPage,
          label: "Paginação de débitos",
        }}
      />
    );
  }

  function renderDebtPanel(title: string, showDescription = false, editable = false) {
    return (
      <PrototypePanel
        title={title}
        headerPaddingX={panelInset}
        action={
          <Typography color="text.secondary" sx={{ fontSize: financeTokens.typography.secondary }}>
            {debts.length} {debts.length === 1 ? "débito" : "débitos"}
          </Typography>
        }
      >
        {renderTable(showDescription, editable)}
        <Box
          sx={{
            display: "flex",
            justifyContent: "space-between",
            gap: fieldGap,
            px: panelInset,
            py: fieldGap,
          }}
        >
          <Typography component="p" variant="h2">
            Total a cobrar
          </Typography>
          <Typography
            component="p"
            variant="h2"
            sx={{ color: financeTokens.colors.header, whiteSpace: "nowrap" }}
          >
            {formatDirectAmount(total)}
          </Typography>
        </Box>
      </PrototypePanel>
    );
  }

  function renderSummary() {
    const summaryRows = [
      ["Cliente", customer?.name ?? "—"],
      ["Contrato", basics.contract],
      ["Placa", formatPlate(basics.plate)],
      ["Vencimento", formatDate(basics.dueDate)],
      ["Centro de custo", basics.costCenter],
      ...(basics.note.trim() ? [["Observação ao cliente", basics.note]] : []),
    ];
    return (
      <PrototypePanel
        title="Dados da cobrança"
        highlighted
        highlightedBackground={financeTokens.colors.stepBackground}
        headerPaddingX={panelInset}
      >
        <Box
          component="dl"
          sx={{
            display: "grid",
            gridTemplateColumns: {
              xs: "minmax(0, 1fr)",
              sm: "repeat(2, minmax(0, 1fr))",
              md: "repeat(5, minmax(0, 1fr))",
            },
            columnGap: fieldGap,
            rowGap: actionGap,
            m: 0,
            px: panelInset,
            py: actionGap,
          }}
        >
          {summaryRows.map(([label, value]) => (
            <Box
              key={label}
              sx={{
                display: "flex",
                flexWrap: "wrap",
                alignItems: "baseline",
                gap: "0.375rem",
                minWidth: 0,
                maxWidth: "100%",
                ...(label === "Observação ao cliente" ? { gridColumn: "1 / -1" } : {}),
              }}
            >
              <Typography component="dt" sx={{ fontWeight: 700, whiteSpace: "nowrap" }}>
                {label}:
              </Typography>
              <Typography component="dd" sx={{ m: 0, minWidth: 0, overflowWrap: "anywhere" }}>
                {value}
              </Typography>
            </Box>
          ))}
        </Box>
      </PrototypePanel>
    );
  }

  function renderFooter(actions: ReactNode, caption = "* Campos obrigatórios.") {
    return (
      <Box
        sx={{
          display: "flex",
          alignItems: "center",
          flexWrap: "wrap",
          gap: fieldGap,
          gridColumn: "1 / -1",
          justifyContent: "space-between",
        }}
      >
        <Typography color="text.secondary" sx={{ fontSize: financeTokens.typography.secondary }}>
          {caption}
        </Typography>
        <Box sx={buttonRow}>{actions}</Box>
      </Box>
    );
  }

  return (
    <>
      <LoadingState
        mode="blocking"
        open={
          submission.isPending ||
          statusRefresh.isPending ||
          lookup.isPending ||
          categoriesQuery.isPending ||
          centersQuery.isPending ||
          refreshing
        }
        message={
          submission.isPending
            ? "Enviando solicitação..."
            : statusRefresh.isPending
              ? "Atualizando situação da cobrança..."
              : lookup.isPending
                ? "Consultando cliente..."
                : "Carregando dados da cobrança..."
        }
      />
      <Stack spacing={fieldGap}>
        <Box>
          <FinancePageTitle navigationHref="/direct-charges/new">Cobrança Direta</FinancePageTitle>
          {step === 0 && (
            <Typography
              color="text.secondary"
              sx={{ fontSize: financeTokens.typography.body, mt: 0.5 }}
            >
              Informe o cliente e adicione os débitos da cobrança.
            </Typography>
          )}
        </Box>
        <ChargeStepper
          activeStep={step}
          steps={stepLabels}
          icons={stepIcons}
          ariaLabel="Etapas da Cobrança Direta"
          responsive
        />

        {catalogError && (
          <Box sx={buttonRow}>
            <Button
              variant="outlined"
              startIcon={<RefreshOutlinedIcon />}
              onClick={() => void retryCatalogs()}
            >
              Tentar novamente
            </Button>
          </Box>
        )}
        {step === 0 && (
          <Box
            component="form"
            noValidate
            onSubmit={(event) => {
              event.preventDefault();
              continueToDebts();
            }}
            sx={chargeGrid}
          >
            <PrototypePanel title="Dados da cobrança" headerPaddingX={panelInset}>
              <Stack spacing={fieldGap} sx={{ p: panelInset }}>
                <Box
                  sx={{
                    display: "flex",
                    gap: actionGap,
                    alignItems: "center",
                    width: "100%",
                    flexWrap: "wrap",
                  }}
                >
                  <TaxIdField
                    label="CPF/CNPJ"
                    required
                    size="small"
                    fullWidth
                    value={basics.document}
                    onChange={(value) => updateBasics("document", value)}
                    onBlur={() => void consultCustomer(true)}
                    sx={{ width: 240, maxWidth: "100%" }}
                  />
                  <Button
                    variant="contained"
                    type="button"
                    startIcon={<SearchOutlinedIcon />}
                    onClick={() => void consultCustomer()}
                    disabled={!/^(\d{11}|\d{14})$/.test(basics.document) || lookup.isPending}
                  >
                    Consultar
                  </Button>
                </Box>
                <Box
                  sx={{
                    ...fieldGrid,
                    gridTemplateColumns: { xs: "minmax(0, 1fr)", sm: "repeat(3, minmax(0, 1fr))" },
                  }}
                >
                  <TextField
                    label="Nome"
                    size="small"
                    fullWidth
                    disabled
                    value={basics.customerName}
                  />
                  <TextField
                    label="Telefone"
                    placeholder="Opcional"
                    size="small"
                    fullWidth
                    disabled={!customer || Boolean(customer.phone?.trim())}
                    value={basics.customerPhone}
                    onChange={(event) => {
                      if (!customer?.phone?.trim())
                        updateBasics("customerPhone", event.target.value);
                    }}
                  />
                  <TextField
                    label="E-mail"
                    placeholder="Opcional"
                    size="small"
                    fullWidth
                    disabled={!customer || Boolean(customer.email?.trim())}
                    value={basics.customerEmail}
                    onChange={(event) => {
                      if (!customer?.email?.trim())
                        updateBasics("customerEmail", event.target.value);
                    }}
                  />
                </Box>
                <Box
                  sx={{
                    ...fieldGrid,
                    gridTemplateColumns: {
                      xs: "minmax(0, 1fr)",
                      sm: "repeat(2, minmax(0, 1fr))",
                      md: "repeat(4, minmax(0, 1fr))",
                    },
                  }}
                >
                  <TextField
                    label="Contrato"
                    required
                    size="small"
                    fullWidth
                    value={basics.contract}
                    onChange={(event) => updateBasics("contract", event.target.value)}
                  />
                  <PlateField
                    label="Placa"
                    required
                    size="small"
                    fullWidth
                    value={basics.plate}
                    onChange={(value) => updateBasics("plate", value)}
                  />
                  <DateField
                    label="Vencimento"
                    required
                    size="small"
                    fullWidth
                    value={basics.dueDate}
                    onChange={(value) => updateBasics("dueDate", value ?? "")}
                  />
                  <TextField
                    select
                    label="Centro de custo"
                    required
                    size="small"
                    fullWidth
                    value={basics.costCenterReference}
                    onChange={(event) => {
                      const center = centers.find(
                        (item) => item.externalReference === event.target.value,
                      );
                      clearReceipt();
                      setBasics((current) => ({
                        ...current,
                        costCenterReference: center?.externalReference ?? "",
                        costCenter: center?.name ?? "",
                      }));
                    }}
                  >
                    {centers.map((center) => (
                      <MenuItem key={center.externalReference} value={center.externalReference}>
                        {center.name}
                      </MenuItem>
                    ))}
                  </TextField>
                </Box>
                <TextField
                  label="Observação ao cliente"
                  placeholder="Opcional"
                  size="small"
                  fullWidth
                  multiline
                  minRows={3}
                  value={basics.note}
                  onChange={(event) => updateBasics("note", event.target.value)}
                />
              </Stack>
            </PrototypePanel>
            {renderFooter(
              <>
                <Button
                  component={Link}
                  href="/home"
                  variant="outlined"
                  startIcon={<CloseOutlinedIcon />}
                >
                  Cancelar
                </Button>
                <Button
                  type="submit"
                  variant="contained"
                  startIcon={<PlaylistAddOutlinedIcon />}
                  disabled={!canContinueToDebts}
                >
                  Adicionar débitos
                </Button>
              </>,
            )}
          </Box>
        )}

        {step === 1 && (
          <Stack spacing={fieldGap}>
            {renderSummary()}
            <Box sx={chargeGrid}>
              <PrototypePanel title="Adicionar débitos" headerPaddingX={panelInset}>
                <Box
                  component="form"
                  noValidate
                  onSubmit={(event) => {
                    event.preventDefault();
                    saveDebt();
                  }}
                >
                  <Stack spacing={fieldGap} sx={{ p: panelInset }}>
                    <Box sx={fieldGrid}>
                      <TextField
                        select
                        label="Categoria Yalla"
                        required
                        size="small"
                        fullWidth
                        value={editor.categoryId}
                        onChange={(event) => {
                          const category = categories.find(
                            (item) => item.id === event.target.value,
                          );
                          setEditor((current) => ({
                            ...current,
                            categoryId: category?.id ?? "",
                            category: category?.name ?? "",
                          }));
                        }}
                      >
                        {categories.map((category) => (
                          <MenuItem key={category.id} value={category.id}>
                            {category.name}
                          </MenuItem>
                        ))}
                      </TextField>
                      <CurrencyField
                        exactDecimal
                        label="Valor"
                        required
                        size="small"
                        fullWidth
                        value={editor.amount}
                        onChange={(value) => updateEditor("amount", value ?? "")}
                      />
                      <DateField
                        label="Ocorrência"
                        required
                        size="small"
                        fullWidth
                        value={editor.occurrenceDate}
                        onChange={(value) => updateEditor("occurrenceDate", value ?? "")}
                      />
                    </Box>
                    <Box
                      sx={{
                        display: "grid",
                        gap: fieldGap,
                        gridTemplateColumns: { xs: "minmax(0, 1fr)", md: "minmax(0, 1fr) auto" },
                        alignItems: "center",
                      }}
                    >
                      <TextField
                        label="Descrição"
                        required
                        size="small"
                        fullWidth
                        value={editor.detail}
                        onChange={(event) => updateEditor("detail", event.target.value)}
                      />
                      <Box sx={buttonRow}>
                        <Button
                          type="submit"
                          variant="contained"
                          startIcon={
                            editingId === null ? <AddOutlinedIcon /> : <SaveOutlinedIcon />
                          }
                          disabled={!canSaveDebt}
                        >
                          {editingId === null ? "Adicionar débito" : "Salvar alteração"}
                        </Button>
                        {(editingId !== null || editorHasInput) && (
                          <Button
                            variant="outlined"
                            startIcon={<CloseOutlinedIcon />}
                            onClick={cancelDebtEditing}
                          >
                            {editingId !== null ? "Cancelar edição" : "Limpar"}
                          </Button>
                        )}
                      </Box>
                    </Box>
                  </Stack>
                </Box>
              </PrototypePanel>
            </Box>
            {renderDebtPanel("Débitos incluídos na cobrança", true, true)}
            {renderFooter(
              <>
                <Button
                  variant="outlined"
                  startIcon={<ArrowBackOutlinedIcon />}
                  onClick={() => {
                    setStep(0);
                  }}
                >
                  Voltar
                </Button>
                <Button
                  variant="contained"
                  startIcon={<FactCheckOutlinedIcon />}
                  onClick={reviewCharge}
                  disabled={!canReviewCharge}
                >
                  Revisar cobrança
                </Button>
              </>,
            )}
          </Stack>
        )}

        {step === 2 && (
          <Stack spacing={fieldGap}>
            <Box sx={chargeGrid}>
              {renderSummary()}
              {renderDebtPanel("Débitos incluídos na cobrança", true)}
            </Box>
            {renderFooter(
              <>
                <Button
                  variant="outlined"
                  startIcon={<ArrowBackOutlinedIcon />}
                  onClick={() => setStep(1)}
                >
                  Voltar aos débitos
                </Button>
                <Button
                  variant="contained"
                  startIcon={<TaskAltOutlinedIcon />}
                  disabled={!canReviewCharge || submission.isPending}
                  onClick={() => void submitDirectCharge()}
                >
                  Gerar cobrança
                </Button>
              </>,
              "Revise os dados antes de gerar a cobrança.",
            )}
          </Stack>
        )}
        {step === 3 && received && (
          <Stack spacing={fieldGap}>
            <ChargeResult
              charge={received}
              categories={categories}
              additionalDetails={[
                ["Cliente", customer?.name ?? "—"],
                ["Contrato", basics.contract],
                ["Placa", formatPlate(basics.plate)],
                ["Centro de custo", basics.costCenter],
                ...(basics.note.trim() ? [["Observação ao cliente", basics.note] as const] : []),
              ]}
              actions={
                <>
                  <Button
                    variant="outlined"
                    startIcon={<RefreshOutlinedIcon />}
                    onClick={() => void refreshChargeStatus()}
                    disabled={statusRefresh.isPending}
                  >
                    Atualizar situação
                  </Button>
                  <Button
                    variant="contained"
                    startIcon={<AddOutlinedIcon />}
                    onClick={startNewDirectCharge}
                  >
                    Nova Cobrança Direta
                  </Button>
                </>
              }
            />
          </Stack>
        )}
      </Stack>
    </>
  );
}

export function DirectChargeSuccessClient({ chargeId }: { chargeId: string | null }) {
  const { tenantId, contextId } = useSessionContext();
  const { showToast } = useToast();
  const categoriesQuery = useCategories();
  const [refreshing, setRefreshing] = useState(false);
  const validId = Boolean(
    chargeId && /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(chargeId),
  );
  const chargeQuery = useQuery({
    queryKey: ["charge", tenantId, contextId, chargeId],
    enabled: validId,
    staleTime: 60_000,
    retry: false,
    queryFn: ({ signal }) => chargesApi.refreshDirectChargeStatus(chargeId!, { contextId, signal }),
  });
  useEffect(() => {
    if (!validId || chargeQuery.isError) {
      showToast({
        severity: "error",
        message: !validId
          ? "Identificador da cobrança inválido."
          : chargeQuery.error instanceof ApiError
            ? chargeQuery.error.message
            : "Não foi possível carregar a cobrança. Tente novamente.",
      });
    }
  }, [validId, chargeQuery.isError, chargeQuery.error, showToast]);

  async function refreshCharge() {
    if (refreshing) return;
    setRefreshing(true);
    try {
      const result = await chargeQuery.refetch();
      if (!result.isError)
        showToast({ message: "Situação da cobrança atualizada.", severity: "success" });
    } finally {
      setRefreshing(false);
    }
  }

  const charge = chargeQuery.data;
  const debt = charge?.items?.[0]?.debt;
  return (
    <>
      <LoadingState
        mode="blocking"
        open={(validId && chargeQuery.isPending) || categoriesQuery.isPending || refreshing}
        message="Carregando cobrança..."
      />
      <Stack spacing={fieldGap}>
        <FinancePageTitle navigationHref="/direct-charges/new">Cobrança Direta</FinancePageTitle>
        <ChargeStepper
          activeStep={3}
          steps={stepLabels}
          icons={stepIcons}
          ariaLabel="Etapas da Cobrança Direta"
          responsive
        />
        {charge && (
          <ChargeResult
            charge={charge}
            categories={categoriesQuery.data ?? []}
            additionalDetails={[
              ["Cliente", charge.customerName || "—"],
              ["Contrato", debt?.contract ?? "—"],
              ["Placa", debt?.plate ? formatPlate(debt.plate) : "—"],
              ["Centro de custo", charge.items?.[0]?.costCenterName ?? "—"],
              ...(charge.payment.customerNote?.trim()
                ? [["Observação ao cliente", charge.payment.customerNote] as const]
                : []),
            ]}
            actions={
              <>
                <Button
                  variant="outlined"
                  startIcon={<RefreshOutlinedIcon />}
                  onClick={() => void refreshCharge()}
                  disabled={refreshing}
                >
                  Atualizar situação
                </Button>
                <Button
                  component={Link}
                  href="/direct-charges/new"
                  variant="contained"
                  startIcon={<AddOutlinedIcon />}
                >
                  Nova Cobrança Direta
                </Button>
              </>
            }
          />
        )}
        {!charge && !chargeQuery.isFetching && (
          <Box sx={buttonRow}>
            {validId && (
              <Button variant="outlined" onClick={() => void refreshCharge()}>
                Tentar novamente
              </Button>
            )}
            <Button component={Link} href="/direct-charges/new" variant="contained">
              Nova Cobrança Direta
            </Button>
          </Box>
        )}
      </Stack>
    </>
  );
}
