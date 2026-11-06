"use client";
import { useEffect, useRef, useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import {
  Alert,
  Box,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  IconButton,
  Stack,
  Tooltip,
  Typography,
} from "@mui/material";
import { PaymentStatusBadge } from "./PaymentStatusBadge";
import CloseIcon from "@mui/icons-material/Close";
import ContentCopyOutlinedIcon from "@mui/icons-material/ContentCopyOutlined";
import OpenInNewOutlinedIcon from "@mui/icons-material/OpenInNewOutlined";
import CancelOutlinedIcon from "@mui/icons-material/CancelOutlined";
import AccountBalanceWalletOutlinedIcon from "@mui/icons-material/AccountBalanceWalletOutlined";
import ReceiptLongOutlinedIcon from "@mui/icons-material/ReceiptLongOutlined";
import { FinancePageTitle } from "@/components/finance/FinancePageTitle";
import { PrototypePanel } from "@/components/ui/PrototypePanel";
import { SummaryCard } from "@/components/ui/SummaryCard";
import { usePaginatedSelection } from "@/hooks/usePaginatedSelection";
import { useSessionContext } from "@/features/auth/context/SessionProvider";
import { useToast } from "@/providers/ToastProvider";
import { financeTokens } from "@/theme/designTokens";
import { formatCurrency } from "@/utils/currency";
import { formatDate, isValidDateOnly } from "@/utils/date";
import { usePaymentCharges } from "../hooks/usePaymentCharges";
import { paymentsApi } from "@/services/api/payments/paymentsApi.client";
import {
  isPaymentRequestAwaitingPayment,
  paymentAttemptStatus,
  paymentChargeStatus,
  paymentEligibilityDetail,
} from "../paymentStatus";
import { paymentDateOnly } from "../paymentDate";
import { PaymentDateFilters } from "./PaymentDateFilters";
import { PaymentChargeTable } from "./PaymentChargeTable";
import { PaymentReceiptDetails } from "./PaymentReceiptDetails";
import type { PaymentCharge, PaymentChargeFilters } from "@/services/api/payments/payment.types";
import { LoadingState } from "@/components/ui/LoadingState";
import { copyTextToClipboard } from "@/utils/clipboard";
import { isChargeReference } from "@/features/charges/chargeReference";

const defaults: PaymentChargeFilters = { startDate: "", endDate: "", page: 1, pageSize: 5 };
export function PaymentChargesPageClient() {
  const session = useSessionContext();
  const { showToast } = useToast();
  const [draft, setDraft] = useState(defaults);
  const [filters, setFilters] = useState(defaults);
  const [error, setError] = useState("");
  const [manualLoad, setManualLoad] = useState(false);
  const [refreshing, setRefreshing] = useState(false);
  const [detailsChargeId, setDetailsChargeId] = useState<string | null>(null);
  const [cancelPaymentRequestId, setCancelPaymentRequestId] = useState<string | null>(null);
  const [copyingLink, setCopyingLink] = useState(false);
  const [cancelingLink, setCancelingLink] = useState(false);
  const copyInFlight = useRef(false);
  const cancelInFlight = useRef(false);
  const queryClient = useQueryClient();
  const query = usePaymentCharges(filters);
  if (manualLoad && !query.isFetching) setManualLoad(false);
  const { selectedRows, setSelectedRows, clearSelection } = usePaginatedSelection<PaymentCharge>({
    scopeKey: session.contextId,
  });
  const data = query.data ?? {
    items: [],
    totalItems: 0,
    totalAmount: 0,
    awaitingPaymentCount: 0,
    expiredPaymentCount: 0,
    availablePaymentCount: 0,
    ...filters,
  };
  const nextRequestExpiry = data.items.reduce<number | null>((nearest, row) => {
    const expiresAt = row.activePaymentRequest?.expiresAt;
    if (!expiresAt) return nearest;
    const timestamp = Date.parse(expiresAt);
    return Number.isFinite(timestamp) && (nearest === null || timestamp < nearest)
      ? timestamp
      : nearest;
  }, null);

  useEffect(() => {
    if (nextRequestExpiry === null || query.isPlaceholderData) return;
    const timer = window.setTimeout(
      () => {
        void Promise.all([
          queryClient.invalidateQueries({
            queryKey: ["payment-charges", session.tenantId, session.contextId],
          }),
          queryClient.invalidateQueries({
            queryKey: ["payment-preparation", session.tenantId, session.contextId],
          }),
        ]);
      },
      Math.max(0, nextRequestExpiry - Date.now()) + 1000,
    );
    return () => window.clearTimeout(timer);
  }, [
    nextRequestExpiry,
    query.isPlaceholderData,
    queryClient,
    session.contextId,
    session.tenantId,
  ]);
  const details = data.items.find((row) => row.id === detailsChargeId) ?? null;
  const detailsStatus = details ? paymentChargeStatus(details) : null;
  const detailsAttempt =
    details?.activity?.openRiskAttempt ?? details?.activity?.latestAttempt ?? null;
  const boletoPdfUrl = safeExternalUrl(detailsAttempt?.boletoPdfUrl);
  async function copyLink(requestId: string) {
    if (copyInFlight.current) return;
    copyInFlight.current = true;
    setCopyingLink(true);
    try {
      const link = await paymentsApi.link(requestId, { contextId: session.contextId });
      if (!link.url) {
        showToast({
          severity: "warning",
          message:
            "Configure o endereço do checkout nos parâmetros desta empresa para copiar o link.",
        });
        return;
      }
      const result = await copyTextToClipboard(link.url);
      showToast({
        severity: result.ok ? "success" : "error",
        message: result.ok
          ? "Link copiado."
          : "Não foi possível copiar o link. Selecione e copie manualmente.",
      });
    } catch {
      showToast({ severity: "error", message: "Não foi possível copiar o link. Tente novamente." });
    } finally {
      copyInFlight.current = false;
      setCopyingLink(false);
    }
  }
  async function openPaymentLink(requestId: string) {
    if (copyInFlight.current) return;
    const paymentWindow = window.open("about:blank", "_blank");
    if (!paymentWindow) {
      showToast({
        severity: "warning",
        message: "Permita pop-ups para abrir o link de pagamento em outra aba.",
      });
      return;
    }
    paymentWindow.opener = null;
    copyInFlight.current = true;
    setCopyingLink(true);
    try {
      const link = await paymentsApi.link(requestId, { contextId: session.contextId });
      if (!link.url) {
        paymentWindow.close();
        showToast({
          severity: "warning",
          message:
            "Configure o endereço do checkout nos parâmetros desta empresa para abrir o link.",
        });
        return;
      }
      paymentWindow.location.href = link.url;
    } catch {
      paymentWindow.close();
      showToast({
        severity: "error",
        message: "Não foi possível abrir o link de pagamento. Tente novamente.",
      });
    } finally {
      copyInFlight.current = false;
      setCopyingLink(false);
    }
  }
  async function cancelPaymentLink() {
    if (!cancelPaymentRequestId || cancelInFlight.current) return;
    cancelInFlight.current = true;
    setCancelingLink(true);
    try {
      await paymentsApi.cancel(cancelPaymentRequestId, { contextId: session.contextId });
      setCancelPaymentRequestId(null);
      showToast({ severity: "success", message: "Link de pagamento cancelado." });
      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: ["payment-charges", session.tenantId, session.contextId],
        }),
        queryClient.invalidateQueries({
          queryKey: ["payment-preparation", session.tenantId, session.contextId],
        }),
      ]);
    } catch {
      showToast({ severity: "error", message: "Não foi possível cancelar o link de pagamento." });
    } finally {
      cancelInFlight.current = false;
      setCancelingLink(false);
    }
  }
  async function filter() {
    const reference = draft.reference?.trim() || undefined;
    if (reference && !isChargeReference(reference)) {
      setError("Informe a cobrança no formato empresa-número, como 10-123.");
      return;
    }
    if (
      (draft.startDate && !isValidDateOnly(draft.startDate)) ||
      (draft.endDate && !isValidDateOnly(draft.endDate)) ||
      (draft.startDate && draft.endDate && draft.startDate > draft.endDate)
    ) {
      setError("Confira o período informado.");
      return;
    }
    const nextFilters = {
      ...draft,
      reference,
      sortBy: filters.sortBy,
      sortDirection: filters.sortDirection,
      page: 1,
    };
    const filtersChanged = Object.keys(nextFilters).some(
      (key) =>
        nextFilters[key as keyof PaymentChargeFilters] !==
        filters[key as keyof PaymentChargeFilters],
    );
    setError("");
    clearSelection();
    setManualLoad(filtersChanged);
    if (filtersChanged) setFilters(nextFilters);
    else {
      setRefreshing(true);
      try {
        await query.refetch();
      } finally {
        setRefreshing(false);
      }
    }
  }
  function selectPaymentStatus(paymentStatus: "awaitingPayment" | "expired" | "available") {
    const nextStatus = filters.paymentStatus === paymentStatus ? undefined : paymentStatus;
    clearSelection();
    setManualLoad(true);
    setDraft((current) => ({ ...current, paymentStatus: nextStatus }));
    setFilters((current) => ({ ...current, paymentStatus: nextStatus, page: 1 }));
  }
  return (
    <>
      <LoadingState
        mode="blocking"
        open={query.isPending || manualLoad || refreshing || copyingLink || cancelingLink}
        message={
          cancelingLink
            ? "Cancelando link de pagamento..."
            : copyingLink
              ? "Processando solicitação..."
              : manualLoad || refreshing
                ? "Atualizando cobranças..."
                : "Carregando cobranças..."
        }
      />
      <Stack spacing={3}>
        <Stack direction="row" sx={{ alignItems: "center", justifyContent: "space-between" }}>
          <Box>
            <FinancePageTitle navigationHref="/payments">Pagamentos</FinancePageTitle>
            <Typography color="text.secondary" sx={{ mt: 0.5 }}>
              Selecione cobranças sem recebimento para solicitar pagamento.
            </Typography>
          </Box>
        </Stack>
        <PaymentDateFilters
          value={draft}
          onChange={setDraft}
          disabled={query.isFetching}
          onSubmit={filter}
          onClear={() => {
            setDraft(defaults);
            setManualLoad(true);
            setFilters(defaults);
            clearSelection();
            setError("");
          }}
        />
        <Box
          sx={{
            display: "grid",
            gap: 2,
            gridTemplateColumns: {
              xs: "1fr",
              sm: "repeat(2, minmax(0, 1fr))",
              md: "repeat(3, minmax(0, 1fr))",
            },
          }}
        >
          <SummaryCard
            label="Disponível"
            value={query.data ? String(data.availablePaymentCount) : "—"}
            icon={<AccountBalanceWalletOutlinedIcon />}
            color="success"
            selected={filters.paymentStatus === "available"}
            onClick={() => selectPaymentStatus("available")}
          />
          <SummaryCard
            label="Aguardando pagamento"
            value={query.data ? String(data.awaitingPaymentCount) : "—"}
            icon={<AccountBalanceWalletOutlinedIcon />}
            color="warning"
            selected={filters.paymentStatus === "awaitingPayment"}
            onClick={() => selectPaymentStatus("awaitingPayment")}
          />
          <SummaryCard
            label="Pagamento expirado"
            value={query.data ? String(data.expiredPaymentCount) : "—"}
            icon={<ReceiptLongOutlinedIcon />}
            color="default"
            selected={filters.paymentStatus === "expired"}
            onClick={() => selectPaymentStatus("expired")}
          />
        </Box>
        {error && <Alert severity="error">{error}</Alert>}
        {query.isError ? (
          <Alert
            severity="error"
            action={
              <Button
                onClick={() => {
                  setManualLoad(true);
                  void query.refetch();
                }}
              >
                Tentar novamente
              </Button>
            }
          >
            Não foi possível carregar as cobranças.
          </Alert>
        ) : (
          <PrototypePanel title="Cobranças">
            <PaymentChargeTable
              data={data}
              selected={selectedRows}
              onSelect={setSelectedRows}
              onRowClick={(row) => setDetailsChargeId(row.id)}
              sortBy={filters.sortBy}
              sortDirection={filters.sortDirection}
              onSort={(key) => {
                setManualLoad(true);
                setFilters((current) => ({
                  ...current,
                  sortBy: key,
                  sortDirection:
                    current.sortBy === key && current.sortDirection === "asc" ? "desc" : "asc",
                  page: 1,
                }));
              }}
              onClear={clearSelection}
              loading={false}
              onPage={(page) => {
                setManualLoad(true);
                setFilters((current) => ({ ...current, page }));
              }}
            />
          </PrototypePanel>
        )}
        <Dialog
          open={!!details}
          onClose={() => !copyingLink && !cancelingLink && setDetailsChargeId(null)}
          fullWidth
          maxWidth="sm"
          slotProps={{
            paper: {
              sx: {
                borderRadius: 1.5,
                color: financeTokens.colors.dialogText,
                backgroundColor: financeTokens.colors.canvas,
              },
            },
          }}
        >
          <DialogTitle
            sx={{ fontSize: financeTokens.typography.pageTitle, fontWeight: 700, pr: 7 }}
          >
            Detalhes da cobrança
            <IconButton
              aria-label="Fechar detalhes da cobrança"
              disabled={copyingLink || cancelingLink}
              onClick={() => setDetailsChargeId(null)}
              sx={{
                position: "absolute",
                right: 12,
                top: 12,
                color: financeTokens.colors.iconMuted,
              }}
            >
              <CloseIcon fontSize="small" />
            </IconButton>
          </DialogTitle>
          {details && (
            <>
              <DialogContent sx={{ px: 2.5, pb: 2, "& .MuiTypography-root": { color: "inherit" } }}>
                <Box
                  sx={{
                    bgcolor: financeTokens.colors.summaryBackground,
                    borderRadius: 1.5,
                    p: 2.5,
                    mb: 3,
                    display: "grid",
                    gridTemplateColumns: { xs: "1fr", sm: "1fr 1fr" },
                    gap: 3,
                  }}
                >
                  <Box>
                    <Typography variant="caption" sx={{ color: financeTokens.colors.captionText }}>
                      Valor da cobrança
                    </Typography>
                    <Typography
                      sx={{
                        fontSize: financeTokens.typography.metric,
                        fontWeight: 800,
                        lineHeight: 1.4,
                      }}
                    >
                      {formatCurrency(details.amount)}
                    </Typography>
                  </Box>
                  <Box
                    sx={{
                      borderLeft: { sm: `2px solid ${financeTokens.colors.canvas}` },
                      pl: { sm: 3 },
                    }}
                  >
                    <Typography variant="caption">Status do pagamento</Typography>
                    <Box sx={{ mt: 0.5, mb: 0.75 }}>
                      <PaymentStatusBadge charge={details} />
                    </Box>
                    <Typography variant="caption">
                      {details.eligible
                        ? "Disponível para solicitar pagamento"
                        : detailsStatus?.detail}
                    </Typography>
                  </Box>
                </Box>
                <Stack direction="row" spacing={1} sx={{ alignItems: "center", mb: 1.5 }}>
                  <ReceiptLongOutlinedIcon fontSize="small" color="primary" />
                  <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
                    Dados da cobrança
                  </Typography>
                </Stack>
                <Box
                  sx={{
                    display: "grid",
                    gridTemplateColumns: { xs: "1fr", sm: "1fr 1fr" },
                    gap: 1.5,
                    borderTop: `1px solid ${financeTokens.colors.listDivider}`,
                    pt: 1.5,
                  }}
                >
                  {[
                    ["Cobrança", details.reference],
                    ["Locatário", details.customerName],
                    ["Contrato", details.contract],
                    ["Data da cobrança", formatDate(paymentDateOnly(details.createdAt))],
                    ["Vencimento", formatDate(details.dueDate)],
                  ].map(([label, value]) => (
                    <Box key={label} sx={{ minWidth: 0 }}>
                      <Typography variant="caption" color="text.secondary">
                        {label}
                      </Typography>
                      <Typography
                        variant="body2"
                        title={value}
                        sx={{ fontWeight: 500, overflowWrap: "anywhere" }}
                      >
                        {value}
                      </Typography>
                    </Box>
                  ))}
                </Box>
                <Stack
                  direction="row"
                  spacing={1}
                  sx={{
                    alignItems: "center",
                    mt: 2,
                    mb: 1.5,
                    borderTop: `1px solid ${financeTokens.colors.listDivider}`,
                    pt: 1.5,
                  }}
                >
                  <AccountBalanceWalletOutlinedIcon fontSize="small" color="primary" />
                  <Typography variant="subtitle1" sx={{ fontWeight: 700 }}>
                    Dados do pagamento
                  </Typography>
                </Stack>
                {(details.receivedAmount ?? 0) > 0 && (
                  <PaymentReceiptDetails
                    amount={details.amount}
                    receivedAmount={details.receivedAmount ?? 0}
                    receivedAt={details.receivedAt}
                    settledAt={details.settledAt}
                    settled={details.settled}
                    receipts={details.receipts}
                  />
                )}
                {!details.settled && details.activity?.latestRequest && (
                  <>
                    <Typography variant="subtitle2" sx={{ mt: 3, mb: 1.5 }}>
                      Última solicitação
                    </Typography>
                    <Stack spacing={1.5}>
                      {[
                        ["Identificador", details.activity.latestRequest.id],
                        [
                          "Gerado em",
                          formatDate(paymentDateOnly(details.activity.latestRequest.createdAt)),
                        ],
                        [
                          "Validade",
                          formatDate(paymentDateOnly(details.activity.latestRequest.expiresAt)),
                        ],
                        ...(details.activity.latestRequest.authorizedPaymentMethods?.length
                          ? [
                              [
                                "Formas aceitas",
                                details.activity.latestRequest.authorizedPaymentMethods.join(" · "),
                              ],
                            ]
                          : []),
                        ...(details.activity.latestRequest.expiredAt
                          ? [
                              [
                                "Expirado em",
                                formatDate(
                                  paymentDateOnly(details.activity.latestRequest.expiredAt),
                                ),
                              ],
                            ]
                          : []),
                        ...(details.activity.latestRequest.canceledAt
                          ? [
                              [
                                "Cancelado em",
                                formatDate(
                                  paymentDateOnly(details.activity.latestRequest.canceledAt),
                                ),
                              ],
                            ]
                          : []),
                        ...(details.activity.latestRequest.completedAt
                          ? [
                              [
                                "Concluído em",
                                formatDate(
                                  paymentDateOnly(details.activity.latestRequest.completedAt),
                                ),
                              ],
                            ]
                          : []),
                        ...(details.activity.latestRequest.completionReason
                          ? [
                              [
                                "Motivo da conclusão",
                                details.activity.latestRequest.completionReason,
                              ],
                            ]
                          : []),
                      ].map(([label, value]) => (
                        <Box
                          key={label}
                          sx={{
                            display: "grid",
                            gridTemplateColumns: "minmax(130px, 1fr) 2fr",
                            gap: 2,
                          }}
                        >
                          <Typography variant="body2" color="text.secondary">
                            {label}
                          </Typography>
                          <Typography
                            variant="body2"
                            sx={{ fontWeight: 600, overflowWrap: "anywhere" }}
                          >
                            {value}
                          </Typography>
                        </Box>
                      ))}
                    </Stack>
                  </>
                )}
                {!details.settled && detailsAttempt && (
                  <>
                    <Typography variant="subtitle2" sx={{ mt: 3, mb: 1.5 }}>
                      {details.activity?.openRiskAttempt
                        ? "Instrumento financeiro que bloqueia nova solicitação"
                        : "Última tentativa de pagamento"}
                    </Typography>
                    <Stack spacing={1.5}>
                      {[
                        ["Status", paymentAttemptStatus(detailsAttempt.status)],
                        ["Provedor", detailsAttempt.provider],
                        ["Forma de pagamento", detailsAttempt.paymentMethodName],
                        ["Iniciada em", formatDate(paymentDateOnly(detailsAttempt.createdAt))],
                        [
                          "Atualizada em",
                          detailsAttempt.updatedAt
                            ? formatDate(paymentDateOnly(detailsAttempt.updatedAt))
                            : "—",
                        ],
                        ...(detailsAttempt.expiresAt
                          ? [
                              [
                                "Validade do instrumento",
                                formatDate(paymentDateOnly(detailsAttempt.expiresAt)),
                              ],
                            ]
                          : []),
                        ...(detailsAttempt.failureCode
                          ? [["Código da falha", detailsAttempt.failureCode]]
                          : []),
                        ...(detailsAttempt.failureMessage
                          ? [["Detalhe da falha", detailsAttempt.failureMessage]]
                          : []),
                        [
                          "Bloqueia nova solicitação",
                          details.activity?.openRiskAttempt
                            ? "Sim, há risco financeiro em aberto"
                            : "Não",
                        ],
                      ].map(([label, value]) => (
                        <Box
                          key={label}
                          sx={{
                            display: "grid",
                            gridTemplateColumns: "minmax(130px, 1fr) 2fr",
                            gap: 2,
                          }}
                        >
                          <Typography variant="body2" color="text.secondary">
                            {label}
                          </Typography>
                          <Typography
                            variant="body2"
                            sx={{ fontWeight: 600, overflowWrap: "anywhere" }}
                          >
                            {value}
                          </Typography>
                        </Box>
                      ))}
                    </Stack>
                    {boletoPdfUrl && (
                      <Box sx={{ display: "flex", justifyContent: "center", mt: 2 }}>
                        <Button
                          component="a"
                          href={boletoPdfUrl}
                          target="_blank"
                          rel="noopener noreferrer"
                          startIcon={<OpenInNewOutlinedIcon />}
                        >
                          Abrir boleto (PDF)
                        </Button>
                      </Box>
                    )}
                  </>
                )}
                {!details.settled && (
                  <Box
                    sx={{
                      display: "grid",
                      gridTemplateColumns: "minmax(130px, 1fr) 2fr",
                      gap: 2,
                      mt: 3,
                    }}
                  >
                    <Typography variant="body2" color="text.secondary">
                      Disponibilidade
                    </Typography>
                    <Typography variant="body2" sx={{ fontWeight: 500 }}>
                      {details.eligible
                        ? "Disponível para solicitar pagamento"
                        : paymentEligibilityDetail(details)}
                    </Typography>
                  </Box>
                )}
                {!details.settled &&
                  details.activePaymentRequest &&
                  isPaymentRequestAwaitingPayment(
                    details.activePaymentRequest.status,
                    details.activePaymentRequest.expiresAt,
                  ) && (
                    <Box sx={{ display: "flex", justifyContent: "center", gap: 1, mt: 3 }}>
                      <Tooltip title="Copiar link de pagamento">
                        <span>
                          <IconButton
                            size="small"
                            color="primary"
                            aria-label="Copiar link de pagamento"
                            disabled={copyingLink || cancelingLink}
                            onClick={() => void copyLink(details.activePaymentRequest!.id)}
                          >
                            <ContentCopyOutlinedIcon fontSize="small" />
                          </IconButton>
                        </span>
                      </Tooltip>
                      <Tooltip title="Abrir link em outra aba">
                        <span>
                          <IconButton
                            size="small"
                            color="info"
                            aria-label="Abrir link de pagamento em outra aba"
                            disabled={copyingLink || cancelingLink}
                            onClick={() => void openPaymentLink(details.activePaymentRequest!.id)}
                          >
                            <OpenInNewOutlinedIcon fontSize="small" />
                          </IconButton>
                        </span>
                      </Tooltip>
                      <Tooltip title="Cancelar link de pagamento">
                        <span>
                          <IconButton
                            size="small"
                            color="error"
                            aria-label="Cancelar link de pagamento"
                            disabled={copyingLink || cancelingLink}
                            onClick={() =>
                              setCancelPaymentRequestId(details.activePaymentRequest!.id)
                            }
                          >
                            <CancelOutlinedIcon fontSize="small" />
                          </IconButton>
                        </span>
                      </Tooltip>
                    </Box>
                  )}
              </DialogContent>
              <DialogActions
                sx={{
                  borderTop: `1px solid ${financeTokens.colors.listDivider}`,
                  mx: 2.5,
                  py: 1.5,
                  px: 0,
                }}
              >
                <Button
                  disabled={copyingLink || cancelingLink}
                  onClick={() => setDetailsChargeId(null)}
                >
                  Fechar
                </Button>
              </DialogActions>
            </>
          )}
        </Dialog>
        <Dialog
          open={!!cancelPaymentRequestId}
          onClose={() => !cancelingLink && setCancelPaymentRequestId(null)}
        >
          <DialogTitle>Cancelar link de pagamento?</DialogTitle>
          <DialogContent>
            O link deixará de aceitar novos pagamentos. Um instrumento financeiro já emitido não
            será cancelado.
          </DialogContent>
          <DialogActions>
            <Button disabled={cancelingLink} onClick={() => setCancelPaymentRequestId(null)}>
              Voltar
            </Button>
            <Button
              color="error"
              startIcon={<CancelOutlinedIcon />}
              disabled={cancelingLink}
              onClick={() => void cancelPaymentLink()}
            >
              Cancelar link
            </Button>
          </DialogActions>
        </Dialog>
      </Stack>
    </>
  );
}

function safeExternalUrl(value?: string | null) {
  if (!value) return null;
  try {
    const url = new URL(value);
    return url.protocol === "https:" && !url.username && !url.password ? url.href : null;
  } catch {
    return null;
  }
}
