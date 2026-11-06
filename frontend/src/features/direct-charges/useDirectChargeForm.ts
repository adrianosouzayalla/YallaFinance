import { useCallback, useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { useToast } from "@/providers/ToastProvider";
import type { DirectChargeReceipt } from "@/services/api/charges/charge.types";
import {
  useSubmitDirectCharge,
  useRefreshDirectChargeStatus,
} from "@/features/charges/hooks/useCreateCharge";
import { ApiError } from "@/services/api/apiError";
import { createSecureUuidV4 } from "@/utils/secureUuid";
import {
  buildDirectChargeRequest,
  createDirectChargeBasics,
  createDirectDebtEditor,
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
import type { FinancialCustomer } from "@/services/api/catalogs/catalog.types";

export function useDirectChargeForm() {
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

  return {
    attempt,
    basics,
    canContinueToDebts,
    canReviewCharge,
    canSaveDebt,
    cancelDebtEditing,
    catalogError,
    categories,
    categoriesQuery,
    centers,
    centersQuery,
    clearReceipt,
    connectionMismatch,
    consultCustomer,
    continueToDebts,
    customer,
    customerConnectionId,
    customerFound,
    debts,
    editDebt,
    editingId,
    editor,
    editorHasInput,
    inFlight,
    lastCatalogNotice,
    lookup,
    lookupController,
    lookupInFlight,
    lookupVersion,
    mounted,
    nextItemId,
    notifyError,
    page,
    pendingController,
    received,
    refreshChargeStatus,
    refreshing,
    removeDebt,
    retryCatalogs,
    reviewCharge,
    router,
    saveDebt,
    setBasics,
    setCustomer,
    setCustomerConnectionId,
    setDebts,
    setEditingId,
    setEditor,
    setPage,
    setReceived,
    setRefreshing,
    setStep,
    showToast,
    startNewDirectCharge,
    statusController,
    statusInFlight,
    statusRefresh,
    step,
    submission,
    submitDirectCharge,
    total,
    updateBasics,
    updateEditor,
    validCategory,
  };
}
