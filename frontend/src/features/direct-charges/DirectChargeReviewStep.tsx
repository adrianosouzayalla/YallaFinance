import type { Dispatch, SetStateAction } from "react";
import { Box, Button, Stack } from "@mui/material";
import ArrowBackOutlinedIcon from "@mui/icons-material/ArrowBackOutlined";
import TaskAltOutlinedIcon from "@mui/icons-material/TaskAltOutlined";
import type { FinancialCustomer } from "@/services/api/catalogs/catalog.types";
import type { DirectChargeBasics, DirectDebtDraft } from "./directChargeDraft";
import { chargeGrid, fieldGap } from "@/theme/layout";
import { renderDirectChargeFooter } from "./directChargeFooter";
import { DirectChargeSummaryPanel } from "./DirectChargeSummaryPanel";
import { DirectChargeDebtPanel } from "./DirectChargeDebtPanel";

interface DirectChargeReviewStepProps {
  basics: DirectChargeBasics;
  customer: FinancialCustomer | null;
  debts: DirectDebtDraft[];
  total: string;
  page: number;
  setPage: Dispatch<SetStateAction<number>>;
  editDebt: (debt: DirectDebtDraft) => void;
  removeDebt: (debt: DirectDebtDraft) => void;
  canReviewCharge: boolean;
  submissionPending: boolean;
  onBack: () => void;
  onSubmit: () => void;
}

export function DirectChargeReviewStep({
  basics,
  customer,
  debts,
  total,
  page,
  setPage,
  editDebt,
  removeDebt,
  canReviewCharge,
  submissionPending,
  onBack,
  onSubmit,
}: DirectChargeReviewStepProps) {
  return (
    <Stack spacing={fieldGap}>
      <Stack spacing={fieldGap}>
        <Box sx={chargeGrid}>
          <DirectChargeSummaryPanel customerName={customer?.name} basics={basics} />
          <DirectChargeDebtPanel
            title="Débitos incluídos na cobrança"
            showDescription
            debts={debts}
            contract={basics.contract}
            total={total}
            page={page}
            onPageChange={setPage}
            onEdit={editDebt}
            onRemove={removeDebt}
          />
        </Box>
        {renderDirectChargeFooter(
          <>
            <Button variant="outlined" startIcon={<ArrowBackOutlinedIcon />} onClick={onBack}>
              Voltar aos débitos
            </Button>
            <Button
              variant="contained"
              startIcon={<TaskAltOutlinedIcon />}
              disabled={!canReviewCharge || submissionPending}
              onClick={onSubmit}
            >
              Gerar cobrança
            </Button>
          </>,
          "Revise os dados antes de gerar a cobrança.",
        )}
      </Stack>
    </Stack>
  );
}
