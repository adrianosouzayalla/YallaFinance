"use client";
import { Button, FormControl, InputLabel, Select, Stack, TextField } from "@mui/material";
import type { JobFilters as Filters, JobStatus, JobTypeOption } from "../types/job.types";
import { jobStatusLabels } from "../utils/job-format";
import FilterAltOutlinedIcon from "@mui/icons-material/FilterAltOutlined";
import FilterAltOffOutlinedIcon from "@mui/icons-material/FilterAltOffOutlined";
import { DateField } from "@/components/forms/DateField";
export function JobFilters({
  value,
  onChange,
  onSearch,
  onClear,
  types,
  typesLoading = false,
}: {
  value: Filters;
  onChange(value: Filters): void;
  onSearch(): void;
  onClear(): void;
  types: readonly JobTypeOption[];
  typesLoading?: boolean;
}) {
  return (
    <Stack
      component="form"
      direction="row"
      sx={{
        alignItems: "flex-end",
        columnGap: 1.25,
        flexWrap: "wrap",
        rowGap: 2,
      }}
      onSubmit={(event) => {
        event.preventDefault();
        onSearch();
      }}
    >
      <DateField
        label="Data início"
        size="small"
        required
        value={value.startDate ?? null}
        onChange={(startDate) => onChange({ ...value, startDate: startDate ?? "" })}
        sx={{ width: 165 }}
      />
      <DateField
        label="Data fim"
        size="small"
        required
        value={value.endDate ?? null}
        onChange={(endDate) => onChange({ ...value, endDate: endDate ?? "" })}
        sx={{ width: 165 }}
      />
      <TextField
        label="Key"
        placeholder="Chave completa ou trecho"
        size="small"
        sx={{ width: 150, minWidth: 150 }}
        value={value.key}
        slotProps={{ htmlInput: { maxLength: 250 } }}
        onChange={(e) => onChange({ ...value, key: e.target.value })}
      />
      <FormControl size="small" disabled={typesLoading} sx={{ width: 150, minWidth: 150 }}>
        <InputLabel htmlFor="job-type">Tipo</InputLabel>
        <Select
          id="job-type"
          label="Tipo"
          native
          value={value.type || "__all_types__"}
          onChange={(event) =>
            onChange({
              ...value,
              type: event.target.value === "__all_types__" ? "" : event.target.value,
            })
          }
        >
          <option value="__all_types__">Todos</option>
          {value.type && !types.some((option) => option.type === value.type) && (
            <option value={value.type}>{value.type}</option>
          )}
          {types.map((type) => (
            <option key={type.type} value={type.type}>
              {type.jobTitle || type.type}
            </option>
          ))}
        </Select>
      </FormControl>
      <FormControl size="small" sx={{ minWidth: 128 }}>
        <InputLabel htmlFor="job-status">Status</InputLabel>
        <Select
          id="job-status"
          label="Status"
          native
          value={value.status || "__all_statuses__"}
          onChange={(event) =>
            onChange({
              ...value,
              status:
                event.target.value === "__all_statuses__" ? "" : (event.target.value as JobStatus),
            })
          }
        >
          <option value="__all_statuses__">Todos</option>
          {Object.entries(jobStatusLabels).map(([status, label]) => (
            <option key={status} value={status}>
              {label}
            </option>
          ))}
        </Select>
      </FormControl>
      <Button type="submit" variant="contained" startIcon={<FilterAltOutlinedIcon />}>
        Filtrar
      </Button>
      <Button
        type="button"
        variant="outlined"
        startIcon={<FilterAltOffOutlinedIcon />}
        onClick={onClear}
      >
        Limpar
      </Button>
    </Stack>
  );
}
