"use client";
import { useRef, useState } from "react";
import { useSearchParams } from "next/navigation";
import Link from "next/link";
import { Alert, Button, Dialog, DialogActions, DialogContent, DialogTitle, Divider, IconButton, Stack, Tooltip } from "@mui/material";
import { PaymentStatusBadge } from "./PaymentStatusBadge";
import ArrowBackOutlinedIcon from "@mui/icons-material/ArrowBackOutlined";
import { useQueryClient } from "@tanstack/react-query";
import { FinancePageTitle } from "@/components/finance/FinancePageTitle";
import { PrototypePanel } from "@/components/ui/PrototypePanel";
import { LoadingState } from "@/components/ui/LoadingState";
import { SelectablePaginatedTable, type SelectableTableColumn } from "@/components/ui/SelectablePaginatedTable";
import { useSessionContext } from "@/features/auth/context/SessionProvider";
import { useToast } from "@/providers/ToastProvider";
import { financeTokens } from "@/theme/designTokens";
import { formatCurrency } from "@/utils/currency";
import { formatDate } from "@/utils/date";
import { usePaymentGenerationResults } from "../hooks/usePaymentGenerationResults";
import { parsePaymentChargeIds, buildPaymentEntryUrl } from "../paymentEntry";
import { isPaymentRequestAwaitingPayment, paymentRequestChargeStatus } from "../paymentStatus";
import { paymentsApi } from "../api/payments.client";
import { copyTextToClipboard } from "@/utils/clipboard";
import { paymentDateOnly } from "../paymentDate";
import type { PaymentRequest, PaymentCharge } from "../types/payment.types";
import OpenInNewOutlinedIcon from "@mui/icons-material/OpenInNewOutlined";
import ContentCopyOutlinedIcon from "@mui/icons-material/ContentCopyOutlined";
import CancelOutlinedIcon from "@mui/icons-material/CancelOutlined";

type ResultRow = PaymentRequest & { charge: PaymentCharge };
export function PaymentGenerationResultClient() {
  const search = useSearchParams(); const { tenantId, contextId } = useSessionContext(); const cache = useQueryClient();
  const { showToast } = useToast();
  let ids: string[] = []; try { ids = parsePaymentChargeIds(search.get("requestIds")); } catch {}
  const query = usePaymentGenerationResults(ids); const [page, setPage] = useState(1);
  const [cancelId, setCancelId] = useState<string | null>(null); const [busy, setBusy] = useState(false);
  const actionInFlight = useRef(false);
  let failures: { chargeId: string; message: string }[] = [];
  try { failures = JSON.parse(sessionStorage.getItem(`payment-failures:${contextId}:${search.get("requestIds")}`) ?? "[]"); } catch {}
  async function copy(id: string) {
    if (actionInFlight.current) return;
    actionInFlight.current = true; setBusy(true);
    try {
      const link = await paymentsApi.link(id, { contextId });
      if (!link.url) {
        showToast({ severity: "warning", message: "Configure o endereço do checkout nos parâmetros desta empresa para copiar o link." });
        return;
      }
      const result = await copyTextToClipboard(
        link.url,
      );
      showToast({ severity: result.ok ? "success" : "error", message: result.ok
        ? "Link copiado."
        : "Não foi possível copiar o link. Selecione e copie manualmente." });
    }
    catch { showToast({ severity: "error", message: "Não foi possível copiar o link. Tente novamente." }); }
    finally { actionInFlight.current = false; setBusy(false); }
  }
  async function openLink(id: string) {
    if (actionInFlight.current) return;
    const paymentWindow = window.open("about:blank", "_blank");
    if (!paymentWindow) {
      showToast({ severity: "warning", message: "Permita pop-ups para abrir o link de pagamento em outra aba." });
      return;
    }
    paymentWindow.opener = null;
    actionInFlight.current = true; setBusy(true);
    try {
      const link = await paymentsApi.link(id, { contextId });
      if (!link.url) {
        paymentWindow.close();
        showToast({ severity: "warning", message: "Configure o endereço do checkout nos parâmetros desta empresa para abrir o link." });
        return;
      }
      paymentWindow.location.href = link.url;
    }
    catch {
      paymentWindow.close();
      showToast({ severity: "error", message: "Não foi possível abrir o link de pagamento. Tente novamente." });
    }
    finally { actionInFlight.current = false; setBusy(false); }
  }
  async function cancel() {
    if (!cancelId || actionInFlight.current) return; actionInFlight.current = true; setBusy(true);
    try {
      const canceled = await paymentsApi.cancel(cancelId, { contextId });
      await cache.cancelQueries({ queryKey: ["payment-results", tenantId, contextId, ids], exact: true });
      cache.setQueryData<ResultRow[]>(["payment-results", tenantId, contextId, ids], (rows) =>
        rows?.map((row) => row.id === canceled.id ? { ...row, ...canceled } : row));
      setCancelId(null);
      await Promise.all([
        cache.invalidateQueries({ queryKey: ["payment-charges", tenantId, contextId] }),
        cache.invalidateQueries({ queryKey: ["payment-preparation", tenantId, contextId] }),
      ]);
    }
    catch { showToast({ severity: "error", message: "Não foi possível cancelar o link." }); }
    finally { actionInFlight.current = false; setBusy(false); }
  }
  const columns: SelectableTableColumn<ResultRow>[] = [
    { id: "charge", header: "Cobrança", getSortValue: (row) => row.charge.reference, render: (row) => row.charge.reference },
    { id: "customer", header: "Locatário", getSortValue: (row) => row.charge.customerName, render: (row) => row.charge.customerName },
    { id: "contract", header: "Contrato", getSortValue: (row) => row.charge.contract, render: (row) => row.charge.contract },
    { id: "amount", header: "Valor", align: "right", getSortValue: (row) => row.charge.amount, render: (row) => formatCurrency(row.charge.amount) },
    { id: "created", header: "Data de geração", getSortValue: (row) => new Date(row.createdAt), render: (row) => formatDate(paymentDateOnly(row.createdAt)) },
    { id: "expiry", header: "Validade", getSortValue: (row) => new Date(row.expiresAt), render: (row) => formatDate(paymentDateOnly(row.expiresAt)) },
    { id: "status", header: "Status do pagamento", getSortValue: (row) => paymentRequestChargeStatus(row, row.charge).label,
      render: (row) => <PaymentStatusBadge charge={row.charge} request={row} /> },
    { id: "actions", header: "Ações", sortable: false, render: (row) => <Stack direction="row" spacing={0.5}>
      {isPaymentRequestAwaitingPayment(row.status, row.expiresAt) && <Tooltip title="Copiar link"><span><IconButton size="small" color="primary" aria-label="Copiar link" disabled={busy} onClick={() => void copy(row.id)}><ContentCopyOutlinedIcon fontSize="small" /></IconButton></span></Tooltip>}
      {isPaymentRequestAwaitingPayment(row.status, row.expiresAt) && <Tooltip title="Abrir link em outra aba"><span><IconButton size="small" color="info" aria-label="Abrir link em outra aba" disabled={busy} onClick={() => void openLink(row.id)}><OpenInNewOutlinedIcon fontSize="small" /></IconButton></span></Tooltip>}
      {isPaymentRequestAwaitingPayment(row.status, row.expiresAt) && <Tooltip title="Cancelar link"><span><IconButton size="small" color="error" aria-label="Cancelar link" disabled={busy} onClick={() => setCancelId(row.id)}><CancelOutlinedIcon fontSize="small" /></IconButton></span></Tooltip>}
    </Stack> },
  ];
  if (!ids.length) return <Alert severity="warning" action={<Button component={Link} href="/payments" variant="outlined" startIcon={<ArrowBackOutlinedIcon />}>Voltar para consulta de pagamentos</Button>}>Nenhuma solicitação informada.</Alert>;
  if (query.isFetching && !query.data) return <LoadingState mode="blocking" message="Recuperando solicitações..." />;
  if (query.isError) return <Alert severity="error" action={<Button onClick={() => void query.refetch()}>Tentar novamente</Button>}>Não foi possível recuperar as solicitações.</Alert>;
  if (!query.data) return <LoadingState mode="blocking" message="Recuperando solicitações..." />;
  return <>
  <LoadingState mode="blocking" open={busy} message="Processando solicitação..." />
  <Stack spacing={3}>
    <FinancePageTitle navigationHref="/payments">Solicitações de pagamento geradas</FinancePageTitle>
    <Alert severity="success">{query.data.length} {query.data.length === 1 ? "solicitação gerada" : "solicitações geradas"}. Os recebimentos serão registrados separadamente.</Alert>
    {failures.length > 0 && <Alert severity="warning" action={<Button component={Link} href={buildPaymentEntryUrl(failures.map((row) => row.chargeId))}>Revisar pendentes</Button>}>
      {failures.length} {failures.length === 1 ? "cobrança não gerada" : "cobranças não geradas"}. Os sucessos foram preservados.
    </Alert>}
    <PrototypePanel title="Solicitações"><SelectablePaginatedTable rows={query.data} paginationMode="client"
      columns={columns} showSelection={false} getRowId={(row) => row.id} selectedRows={[]} onSelectionChange={() => {}}
      tableLabel="Solicitações geradas" headerSx={{ backgroundColor: financeTokens.colors.tableHeader }}
      pagination={{ page, pageSize: 5, totalItems: query.data.length, onPageChange: setPage }} /></PrototypePanel>
    <Divider />
    <Stack direction="row" sx={{ justifyContent: "flex-end" }}><Button component={Link} href="/payments" variant="outlined" startIcon={<ArrowBackOutlinedIcon />}>Voltar para consulta de pagamentos</Button></Stack>
    <Dialog open={!!cancelId} onClose={() => !busy && setCancelId(null)}>
      <DialogTitle>Cancelar link?</DialogTitle><DialogContent>Cancelar o link não cancela um Pix já emitido.</DialogContent>
      <DialogActions><Button disabled={busy} onClick={() => setCancelId(null)}>Voltar</Button><Button color="error" startIcon={<CancelOutlinedIcon />} disabled={busy} onClick={() => void cancel()}>Cancelar link</Button></DialogActions>
    </Dialog>
  </Stack>
  </>;
}
