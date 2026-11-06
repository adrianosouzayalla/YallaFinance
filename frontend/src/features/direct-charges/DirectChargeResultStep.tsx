import { Button, Stack } from "@mui/material";
import AddOutlinedIcon from "@mui/icons-material/AddOutlined";
import RefreshOutlinedIcon from "@mui/icons-material/RefreshOutlined";
import { ChargeResult } from "@/features/charges/components/ChargeResult";
import { formatPlate } from "@/utils/plate";
import type { CategoryOption, FinancialCustomer } from "@/services/api/catalogs/catalog.types";
import type { DirectChargeReceipt } from "@/services/api/charges/charge.types";
import type { DirectChargeBasics } from "./directChargeDraft";
import { fieldGap } from "@/theme/layout";

interface DirectChargeResultStepProps {
  received: DirectChargeReceipt;
  categories: CategoryOption[];
  customer: FinancialCustomer | null;
  basics: DirectChargeBasics;
  statusRefreshPending: boolean;
  onRefresh: () => void;
  onNew: () => void;
}

export function DirectChargeResultStep({
  received,
  categories,
  customer,
  basics,
  statusRefreshPending,
  onRefresh,
  onNew,
}: DirectChargeResultStepProps) {
  return (
    <Stack spacing={fieldGap}>
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
                onClick={onRefresh}
                disabled={statusRefreshPending}
              >
                Atualizar situação
              </Button>
              <Button variant="contained" startIcon={<AddOutlinedIcon />} onClick={onNew}>
                Nova Cobrança Direta
              </Button>
            </>
          }
        />
      </Stack>
    </Stack>
  );
}
