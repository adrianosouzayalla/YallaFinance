import type { Dispatch, SetStateAction } from "react";
import {
  Box,
  FormControl,
  FormHelperText,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import { PrototypePanel } from "@/components/ui/PrototypePanel";
import { CurrencyField } from "@/components/forms/CurrencyField";
import { DateField } from "@/components/forms/DateField";
import { AutocompleteField } from "@/components/forms/AutocompleteField";
import type { Customer } from "@/services/api/customers/customer.types";
import type { DebtCategory } from "@/services/api/debts/debt.types";
import type { CategoryOption } from "@/services/api/catalogs/catalog.types";
import type { StandaloneChargeDraft } from "@/services/api/charges/charge.types";
import type { useStandaloneChargeForm } from "@/features/charges/hooks/useStandaloneChargeForm";
import { financeTokens } from "@/theme/designTokens";

export function maskTaxId(taxId: string): string {
  const digits = taxId.replace(/\D/g, "");
  return `***.${digits.slice(3, 6)}.${digits.slice(6, 9)}-**`;
}

interface NewChargeDebtPanelProps {
  customers: Customer[];
  customersSearching: boolean;
  customersFailed: boolean;
  selectedCustomer: Customer | null;
  setSelectedCustomer: Dispatch<SetStateAction<Customer | null>>;
  customerInput: string;
  setCustomerInput: Dispatch<SetStateAction<string>>;
  setSelectedMovementId: (id: string | undefined) => void;
  draft: StandaloneChargeDraft;
  updateDraft: (nextDraft: StandaloneChargeDraft) => void;
  errors: ReturnType<typeof useStandaloneChargeForm>["errors"];
  categories: CategoryOption[];
  amountValue: number | null;
  setAmountValue: Dispatch<SetStateAction<number | null>>;
}

export function NewChargeDebtPanel({
  customers,
  customersSearching,
  customersFailed,
  selectedCustomer,
  setSelectedCustomer,
  customerInput,
  setCustomerInput,
  setSelectedMovementId,
  draft,
  updateDraft,
  errors,
  categories,
  amountValue,
  setAmountValue,
}: NewChargeDebtPanelProps) {
  return (
    <PrototypePanel title="Débito a cobrar">
      <Stack spacing={2} sx={{ p: 2.5 }}>
        <AutocompleteField<Customer>
          label="Locatário"
          options={customers}
          value={selectedCustomer}
          inputValue={customerInput}
          getOptionKey={(customer) => customer.customerId}
          getOptionLabel={(customer) => `${customer.name} (${maskTaxId(customer.taxId)})`}
          onChange={(customer) => {
            setSelectedCustomer(customer);
            setCustomerInput(customer ? `${customer.name} (${maskTaxId(customer.taxId)})` : "");
            setSelectedMovementId(undefined);
            updateDraft({
              ...draft,
              customerId: customer?.customerId ?? "",
              selectedMovementId: undefined,
            });
          }}
          onInputChange={(text) => {
            setCustomerInput(text);
            if (selectedCustomer) {
              setSelectedCustomer(null);
              setSelectedMovementId(undefined);
              updateDraft({ ...draft, customerId: "", selectedMovementId: undefined });
            }
          }}
          loading={customersSearching}
          error={Boolean(errors.customerId) || customersFailed}
          helperText={
            errors.customerId ??
            (customersFailed
              ? "Não foi possível buscar os locatários. Tente novamente."
              : selectedCustomer
                ? undefined
                : "Digite pelo menos 3 caracteres do nome ou documento")
          }
          placeholder="Nome ou CPF/CNPJ"
          noOptionsText={
            customerInput.trim().length < 3
              ? "Digite pelo menos 3 caracteres"
              : "Nenhum locatário encontrado"
          }
        />
        <FormControl fullWidth size="small">
          <InputLabel id="category-label">Categoria</InputLabel>
          <Select
            label="Categoria"
            labelId="category-label"
            value={draft.categoryId ?? ""}
            onChange={(event) => {
              const selected = categories.find((item) => item.id === event.target.value);
              updateDraft({
                ...draft,
                category: (selected?.name ?? "") as DebtCategory,
                categoryId: selected?.id,
              });
            }}
          >
            <MenuItem value="">Selecione uma categoria</MenuItem>
            {categories.map((category) => (
              <MenuItem key={category.id} value={category.id}>
                {category.name}
              </MenuItem>
            ))}
          </Select>
          {errors.categoryId ? <FormHelperText error>{errors.categoryId}</FormHelperText> : null}
        </FormControl>
        <Box
          sx={{
            display: "grid",
            gap: 2,
            gridTemplateColumns: "minmax(0, 1fr) minmax(0, 1fr)",
          }}
        >
          <CurrencyField
            error={errors.amount !== undefined}
            helperText={errors.amount}
            label="Valor"
            size="small"
            value={amountValue}
            onChange={(value) => {
              setAmountValue(value);
              updateDraft({ ...draft, amount: value ?? 0 });
            }}
          />
          <DateField
            error={errors.occurrenceDate !== undefined}
            helperText={errors.occurrenceDate}
            label="Data da ocorrência"
            size="small"
            value={draft.occurrenceDate}
            onChange={(value) => updateDraft({ ...draft, occurrenceDate: value ?? "" })}
          />
        </Box>
        <TextField
          error={errors.descriptionDetail !== undefined}
          helperText={errors.descriptionDetail}
          label="Detalhe da descrição"
          required
          size="small"
          value={draft.descriptionDetail}
          onChange={(event) => updateDraft({ ...draft, descriptionDetail: event.target.value })}
          onBlur={() => {
            if (!draft.descriptionDetail.trim()) {
              updateDraft({ ...draft, descriptionDetail: "Cobrança avulsa" });
            }
          }}
        />
        <Box>
          <Typography
            color="text.secondary"
            sx={{ fontSize: financeTokens.typography.secondary, mb: 0.75 }}
          >
            Anexo / evidência
          </Typography>
          {/*TODO: Adriano Implementar o upload e a persistência de anexos/evidências; o campo atual é apenas visual.*/}
          <Box
            sx={{
              border: "1px dashed",
              borderColor: financeTokens.colors.border,
              borderRadius: `${financeTokens.radius.control}px`,
              color: financeTokens.colors.mutedText,
              px: 2,
              py: 1.5,
            }}
          >
            Arraste fotos, laudo ou auto de infração
          </Box>
        </Box>
      </Stack>
    </PrototypePanel>
  );
}
