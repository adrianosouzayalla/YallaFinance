import type { Control, FieldErrors, UseFormRegister, UseFormSetValue } from "react-hook-form";
import { Controller } from "react-hook-form";
import {
  Alert,
  Box,
  Button,
  FormControl,
  FormHelperText,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  TextField,
  Typography,
} from "@mui/material";
import type { Dispatch, SetStateAction } from "react";
import type { Customer } from "@/services/api/customers/customer.types";
import type { CustomerContract } from "@/services/api/customers/customerContract.types";
import type { useCustomerSuggestions } from "@/features/customers/hooks/useCustomerSuggestions";
import type { useCustomerContracts } from "@/features/customers/hooks/useCustomerContracts";
import type { useFleetMovements } from "@/features/customers/hooks/useFleetMovements";
import type { useCategories } from "@/features/catalogs/hooks/useCategories";
import { AutocompleteField } from "@/components/forms/AutocompleteField";
import { CurrencyField } from "@/components/forms/CurrencyField";
import { DateField } from "@/components/forms/DateField";
import { PrototypePanel } from "@/components/ui/PrototypePanel";
import type { ManualDebtForm } from "@/services/api/debts/manualDebt.types";

export function customerLabel(customer: Customer) {
  const digits = customer.taxId.replace(/\D/g, "");
  return `${customer.name} (${digits.length === 14 ? "CNPJ" : "CPF"} •••${digits.slice(-4)})`;
}

interface ManualDebtFieldsProps {
  control: Control<ManualDebtForm>;
  register: UseFormRegister<ManualDebtForm>;
  errors: FieldErrors<ManualDebtForm>;
  setValue: UseFormSetValue<ManualDebtForm>;
  locked: boolean;
  customer: Customer | null;
  search: string;
  setSearch: Dispatch<SetStateAction<string>>;
  setCustomer: Dispatch<SetStateAction<Customer | null>>;
  suggestions: ReturnType<typeof useCustomerSuggestions>;
  onCustomerChange: (value: Customer | null) => void;
  customerId: string;
  validOccurrence: boolean;
  contracts: ReturnType<typeof useCustomerContracts>;
  movements: ReturnType<typeof useFleetMovements>;
  contractItems: CustomerContract[];
  selectedContract: CustomerContract | undefined;
  categories: ReturnType<typeof useCategories>;
}

export function ManualDebtFields({
  control,
  register,
  errors,
  setValue,
  locked,
  customer,
  search,
  setSearch,
  setCustomer,
  suggestions,
  onCustomerChange,
  customerId,
  validOccurrence,
  contracts,
  movements,
  contractItems,
  selectedContract,
  categories,
}: ManualDebtFieldsProps) {
  return (
    <PrototypePanel title="Dados do débito">
      <Stack spacing={2} sx={{ p: 2.5 }}>
        <Box
          sx={{
            display: "grid",
            gap: 2,
            gridTemplateColumns: {
              xs: "1fr",
              md: "minmax(0, 480px) minmax(0, 240px) minmax(0, 300px)",
            },
            width: "100%",
            maxWidth: { md: 1036 },
            alignItems: "start",
          }}
        >
          <AutocompleteField<Customer>
            label="Locatário"
            required
            disabled={locked}
            options={suggestions.items}
            value={customer}
            inputValue={search}
            getOptionKey={(item) => item.customerId}
            getOptionLabel={customerLabel}
            onChange={onCustomerChange}
            onInputChange={(text) => {
              setSearch(text);
              if (customer) {
                setCustomer(null);
                setValue("customerId", "");
                setValue("contractId", "");
              }
            }}
            loading={suggestions.searching}
            error={Boolean(errors.customerId) || suggestions.isError}
            helperText={
              errors.customerId?.message ??
              (suggestions.isError
                ? "Não foi possível consultar os clientes. Tente novamente."
                : customer
                  ? undefined
                  : "Digite pelo menos 3 caracteres do nome ou documento")
            }
            noOptionsText={
              search.trim().length < 3
                ? "Digite pelo menos 3 caracteres"
                : "Nenhum cliente encontrado"
            }
          />
          <Controller
            name="occurrenceDate"
            control={control}
            render={({ field }) => (
              <DateField
                label="Data da ocorrência"
                required
                size="small"
                disabled={locked}
                value={field.value}
                onChange={(value) => {
                  field.onChange(value ?? "");
                  setValue("contractId", "");
                }}
                error={Boolean(errors.occurrenceDate)}
                helperText={errors.occurrenceDate?.message}
              />
            )}
          />
          <Controller
            name="contractId"
            control={control}
            render={({ field }) => (
              <FormControl
                fullWidth
                size="small"
                required
                error={Boolean(errors.contractId)}
                disabled={
                  locked ||
                  !customerId ||
                  !validOccurrence ||
                  contracts.isPending ||
                  movements.isPending ||
                  movements.isError
                }
              >
                <InputLabel id="manual-contract-label">Contrato / veículo</InputLabel>
                <Select {...field} label="Contrato / veículo" labelId="manual-contract-label">
                  <MenuItem value="">Selecione um contrato</MenuItem>
                  {contractItems.map((item) => (
                    <MenuItem key={item.id} value={item.id}>
                      {item.number} — {item.plate ?? "Sem placa"}
                    </MenuItem>
                  ))}
                </Select>
                <FormHelperText>
                  {errors.contractId?.message ??
                    (customerId &&
                    validOccurrence &&
                    !movements.isPending &&
                    !movements.isError &&
                    !contracts.isPending &&
                    contractItems.length === 0 &&
                    !contracts.isError
                      ? "Nenhum contrato disponível para a ocorrência nas páginas carregadas."
                      : undefined)}
                </FormHelperText>
              </FormControl>
            )}
          />
        </Box>
        {movements.isError ? (
          <Alert
            severity="error"
            action={<Button onClick={() => void movements.refetch()}>Tentar novamente</Button>}
          >
            Não foi possível consultar a movimentação na data informada.
          </Alert>
        ) : null}
        {contracts.isError ? (
          <Alert
            severity="error"
            action={<Button onClick={() => void contracts.refetch()}>Tentar novamente</Button>}
          >
            Não foi possível consultar os contratos.
          </Alert>
        ) : null}
        {contracts.hasNextPage ? (
          <Button
            disabled={contracts.isFetchingNextPage || locked}
            onClick={() => void contracts.fetchNextPage()}
          >
            Carregar mais contratos
          </Button>
        ) : null}
        {selectedContract ? (
          <Typography color="text.secondary" variant="body2">
            Produto: {selectedContract.product ?? "—"} · Frota: {selectedContract.fleet ?? "—"}
          </Typography>
        ) : null}
        {categories.isError ? (
          <Alert
            severity="error"
            action={<Button onClick={() => void categories.refetch()}>Tentar novamente</Button>}
          >
            Não foi possível consultar as categorias.
          </Alert>
        ) : null}
        <Box
          sx={{
            display: "grid",
            gap: 2,
            gridTemplateColumns: {
              xs: "1fr",
              md: "minmax(0, 480px) minmax(0, 240px) minmax(0, 300px)",
            },
            width: "100%",
            maxWidth: { md: 1036 },
          }}
        >
          <Controller
            name="categoryId"
            control={control}
            render={({ field }) => (
              <FormControl
                fullWidth
                size="small"
                required
                error={Boolean(errors.categoryId)}
                disabled={locked || categories.isPending}
              >
                <InputLabel id="manual-category-label">Categoria</InputLabel>
                <Select {...field} label="Categoria" labelId="manual-category-label">
                  <MenuItem value="">Selecione uma categoria</MenuItem>
                  {(categories.data ?? []).map((item) => (
                    <MenuItem key={item.id} value={item.id}>
                      {item.name}
                    </MenuItem>
                  ))}
                </Select>
                <FormHelperText>{errors.categoryId?.message}</FormHelperText>
              </FormControl>
            )}
          />
          <Controller
            name="amount"
            control={control}
            render={({ field }) => (
              <CurrencyField
                exactDecimal
                label="Valor"
                required
                size="small"
                disabled={locked}
                value={field.value || null}
                onChange={(value) => field.onChange(value ?? "")}
                error={Boolean(errors.amount)}
                helperText={errors.amount?.message}
              />
            )}
          />
          <Controller
            name="dueDate"
            control={control}
            render={({ field }) => (
              <DateField
                label="Vencimento"
                required
                size="small"
                disabled={locked}
                value={field.value}
                onChange={(value) => field.onChange(value ?? "")}
                error={Boolean(errors.dueDate)}
                helperText={errors.dueDate?.message}
              />
            )}
          />
        </Box>
        <TextField
          {...register("description")}
          sx={{ width: "100%", maxWidth: { md: 1036 } }}
          label="Descrição"
          required
          size="small"
          multiline
          minRows={2}
          disabled={locked}
          error={Boolean(errors.description)}
          helperText={errors.description?.message}
        />
        <Typography color="text.secondary" variant="body2">
          Status inicial: Em aberto
        </Typography>
      </Stack>
    </PrototypePanel>
  );
}
