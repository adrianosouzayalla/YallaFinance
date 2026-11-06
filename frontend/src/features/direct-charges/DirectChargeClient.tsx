"use client";

import { useDirectChargeForm } from "./useDirectChargeForm";
import { Box, Button, Stack, Typography } from "@mui/material";
import RefreshOutlinedIcon from "@mui/icons-material/RefreshOutlined";
import { FinancePageTitle } from "@/components/finance/FinancePageTitle";
import { ChargeStepper } from "@/features/charges/components/ChargeStepper";
import { useSessionContext } from "@/features/auth/context/SessionProvider";
import { LoadingState } from "@/components/ui/LoadingState";
import { financeTokens } from "@/theme/designTokens";
import { stepIcons, stepLabels } from "./directChargeLayout";
import { buttonRow, fieldGap } from "@/theme/layout";
import { DirectChargeCustomerStep } from "./DirectChargeCustomerStep";
import { DirectChargeDebtsStep } from "./DirectChargeDebtsStep";
import { DirectChargeReviewStep } from "./DirectChargeReviewStep";
import { DirectChargeResultStep } from "./DirectChargeResultStep";

export function DirectChargeClient() {
  const { tenantId, contextId } = useSessionContext();
  return <DirectChargeForm key={JSON.stringify([tenantId, contextId])} />;
}

function DirectChargeForm() {
  const {
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
    consultCustomer,
    continueToDebts,
    customer,
    debts,
    editDebt,
    editingId,
    editor,
    editorHasInput,
    lookup,
    page,
    received,
    refreshChargeStatus,
    refreshing,
    removeDebt,
    retryCatalogs,
    reviewCharge,
    saveDebt,
    setBasics,
    setEditor,
    setPage,
    setStep,
    startNewDirectCharge,
    statusRefresh,
    step,
    submission,
    submitDirectCharge,
    total,
    updateBasics,
    updateEditor,
  } = useDirectChargeForm();
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
          <DirectChargeCustomerStep
            basics={basics}
            setBasics={setBasics}
            updateBasics={updateBasics}
            consultCustomer={consultCustomer}
            lookupPending={lookup.isPending}
            customer={customer}
            centers={centers}
            clearReceipt={clearReceipt}
            canContinueToDebts={canContinueToDebts}
            continueToDebts={continueToDebts}
          />
        )}

        {step === 1 && (
          <DirectChargeDebtsStep
            categories={categories}
            editor={editor}
            setEditor={setEditor}
            updateEditor={updateEditor}
            saveDebt={saveDebt}
            canSaveDebt={canSaveDebt}
            editingId={editingId}
            editorHasInput={editorHasInput}
            cancelDebtEditing={cancelDebtEditing}
            debts={debts}
            basics={basics}
            customer={customer}
            total={total}
            page={page}
            setPage={setPage}
            editDebt={editDebt}
            removeDebt={removeDebt}
            canReviewCharge={canReviewCharge}
            onReview={reviewCharge}
            onBack={() => setStep(0)}
          />
        )}

        {step === 2 && (
          <DirectChargeReviewStep
            basics={basics}
            customer={customer}
            debts={debts}
            total={total}
            page={page}
            setPage={setPage}
            editDebt={editDebt}
            removeDebt={removeDebt}
            canReviewCharge={canReviewCharge}
            submissionPending={submission.isPending}
            onBack={() => setStep(1)}
            onSubmit={() => void submitDirectCharge()}
          />
        )}
        {step === 3 && received && (
          <DirectChargeResultStep
            received={received}
            categories={categories}
            customer={customer}
            basics={basics}
            statusRefreshPending={statusRefresh.isPending}
            onRefresh={() => void refreshChargeStatus()}
            onNew={startNewDirectCharge}
          />
        )}
      </Stack>
    </>
  );
}
