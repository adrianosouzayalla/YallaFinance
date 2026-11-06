import type { Dispatch, SetStateAction } from "react";
import { Box, Button, MenuItem, Stack, TextField } from "@mui/material";
import AddOutlinedIcon from "@mui/icons-material/AddOutlined";
import ArrowBackOutlinedIcon from "@mui/icons-material/ArrowBackOutlined";
import CloseOutlinedIcon from "@mui/icons-material/CloseOutlined";
import FactCheckOutlinedIcon from "@mui/icons-material/FactCheckOutlined";
import SaveOutlinedIcon from "@mui/icons-material/SaveOutlined";
import { PrototypePanel } from "@/components/ui/PrototypePanel";
import { CurrencyField } from "@/components/forms/CurrencyField";
import { DateField } from "@/components/forms/DateField";
import type { FinancialCustomer, CategoryOption } from "@/services/api/catalogs/catalog.types";
import type {
  createDirectDebtEditor,
  DirectChargeBasics,
  DirectDebtDraft,
} from "./directChargeDraft";
import { buttonRow, chargeGrid, fieldGap, fieldGrid, panelInset } from "@/theme/layout";
import { renderDirectChargeFooter } from "./directChargeFooter";
import { DirectChargeSummaryPanel } from "./DirectChargeSummaryPanel";
import { DirectChargeDebtPanel } from "./DirectChargeDebtPanel";

type DirectDebtEditor = ReturnType<typeof createDirectDebtEditor>;

interface DirectChargeDebtsStepProps {
  categories: CategoryOption[];
  editor: DirectDebtEditor;
  setEditor: Dispatch<SetStateAction<DirectDebtEditor>>;
  updateEditor: (field: keyof DirectDebtEditor, value: string) => void;
  saveDebt: () => void;
  canSaveDebt: boolean;
  editingId: number | null;
  editorHasInput: boolean;
  cancelDebtEditing: () => void;
  debts: DirectDebtDraft[];
  basics: DirectChargeBasics;
  customer: FinancialCustomer | null;
  total: string;
  page: number;
  setPage: Dispatch<SetStateAction<number>>;
  editDebt: (debt: DirectDebtDraft) => void;
  removeDebt: (debt: DirectDebtDraft) => void;
  canReviewCharge: boolean;
  onReview: () => void;
  onBack: () => void;
}

export function DirectChargeDebtsStep({
  categories,
  editor,
  setEditor,
  updateEditor,
  saveDebt,
  canSaveDebt,
  editingId,
  editorHasInput,
  cancelDebtEditing,
  debts,
  basics,
  customer,
  total,
  page,
  setPage,
  editDebt,
  removeDebt,
  canReviewCharge,
  onReview,
  onBack,
}: DirectChargeDebtsStepProps) {
  return (
    <Stack spacing={fieldGap}>
      <Stack spacing={fieldGap}>
        <DirectChargeSummaryPanel customerName={customer?.name} basics={basics} />
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
                      const category = categories.find((item) => item.id === event.target.value);
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
                      startIcon={editingId === null ? <AddOutlinedIcon /> : <SaveOutlinedIcon />}
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
        <DirectChargeDebtPanel
          title="Débitos incluídos na cobrança"
          showDescription
          editable
          debts={debts}
          contract={basics.contract}
          total={total}
          page={page}
          onPageChange={setPage}
          onEdit={editDebt}
          onRemove={removeDebt}
        />
        {renderDirectChargeFooter(
          <>
            <Button
              variant="outlined"
              startIcon={<ArrowBackOutlinedIcon />}
              onClick={() => {
                onBack();
              }}
            >
              Voltar
            </Button>
            <Button
              variant="contained"
              startIcon={<FactCheckOutlinedIcon />}
              onClick={onReview}
              disabled={!canReviewCharge}
            >
              Revisar cobrança
            </Button>
          </>,
        )}
      </Stack>
    </Stack>
  );
}
