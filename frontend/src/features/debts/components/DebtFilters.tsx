import { Button, FormControl, InputLabel, Select, Stack } from "@mui/material";
import { DateField } from "@/components/forms/DateField";
import { debtStatuses, getDebtStatusLabel } from "@/features/debts/debtStatus";
import type { DebtFilters as DebtFiltersValue } from "@/features/debts/types/debt.types";
import type { DebtStatus } from "@/features/debts/debtStatus";
import type { CategoryOption } from "@/features/catalogs/types/catalog.types";

interface DebtFiltersProps {
  disabled?: boolean;
  value: DebtFiltersValue;
  onChange(value: DebtFiltersValue): void;
  onClear(): void;
  onSubmit(): void;
  categories: CategoryOption[];
}

const statuses: Array<DebtStatus | "Todos"> = [...debtStatuses, "Todos"];

const allCategoriesValue = "__all_categories__";

export function DebtFilters({
  disabled = false,
  onChange,
  onClear,
  onSubmit,
  categories,
  value,
}: DebtFiltersProps) {
  return (
    <Stack
      component="form"
      direction="row"
      aria-busy={disabled}
      sx={{
        alignItems: "flex-end",
        columnGap: 1.25,
        flexWrap: "wrap",
        rowGap: 2,
      }}
      onSubmit={(event) => {
        event.preventDefault();
        onSubmit();
      }}
    >
      <FormControl disabled={disabled} size="small" sx={{ minWidth: 150 }}>
        <InputLabel htmlFor="debt-category" shrink>
          Categoria
        </InputLabel>
        <Select
          id="debt-category"
          label="Categoria"
          native
          value={value.categories[0] ?? allCategoriesValue}
          onChange={(event) => {
            const category = event.target.value as string | typeof allCategoriesValue;
            onChange({
              ...value,
              categories: category === allCategoriesValue || category === "" ? [] : [category],
              page: 1,
            });
          }}
        >
          <option value={allCategoriesValue}>Todas</option>
          {categories.map((category) => (
            <option key={category.id} value={category.id}>
              {category.name}
            </option>
          ))}
        </Select>
      </FormControl>

      <FormControl disabled={disabled} size="small" sx={{ minWidth: 128 }}>
        <InputLabel htmlFor="debt-status">Status</InputLabel>
        <Select
          id="debt-status"
          label="Status"
          native
          value={value.status}
          onChange={(event) =>
            onChange({ ...value, page: 1, status: event.target.value as DebtStatus | "Todos" })
          }
        >
          {statuses.map((status) => (
            <option key={status} value={status}>
              {status === "Todos" ? "Todos" : getDebtStatusLabel(status)}
            </option>
          ))}
        </Select>
      </FormControl>

      <Stack direction="row" spacing={1} sx={{ alignItems: "center" }}>
        <DateField
          disabled={disabled}
          id="debt-period-start"
          label="Data inicial"
          size="small"
          value={value.startDate}
          max={value.endDate || undefined}
          onChange={(date) => onChange({ ...value, page: 1, startDate: date ?? "" })}
        />
        <DateField
          disabled={disabled}
          id="debt-period-end"
          label="Data final"
          size="small"
          value={value.endDate}
          min={value.startDate || undefined}
          onChange={(date) => onChange({ ...value, page: 1, endDate: date ?? "" })}
        />
      </Stack>
      <Button disabled={disabled} type="submit" variant="contained">
        Filtrar
      </Button>
      <Button disabled={disabled} type="button" variant="outlined" onClick={onClear}>
        Limpar
      </Button>
    </Stack>
  );
}
