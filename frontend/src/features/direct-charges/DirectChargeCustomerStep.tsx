import type { Dispatch, SetStateAction } from "react";
import Link from "next/link";
import { Box, Button, MenuItem, Stack, TextField } from "@mui/material";
import CloseOutlinedIcon from "@mui/icons-material/CloseOutlined";
import PlaylistAddOutlinedIcon from "@mui/icons-material/PlaylistAddOutlined";
import SearchOutlinedIcon from "@mui/icons-material/SearchOutlined";
import { PrototypePanel } from "@/components/ui/PrototypePanel";
import { TaxIdField } from "@/components/forms/TaxIdField";
import { PlateField } from "@/components/forms/PlateField";
import { DateField } from "@/components/forms/DateField";
import type { FinancialCustomer } from "@/services/api/catalogs/catalog.types";
import type { DirectChargeBasics } from "./directChargeDraft";
import { actionGap, fieldGap, panelInset, chargeGrid, fieldGrid } from "@/theme/layout";
import { renderDirectChargeFooter } from "./directChargeFooter";

interface DirectChargeCustomerStepProps {
  basics: DirectChargeBasics;
  setBasics: Dispatch<SetStateAction<DirectChargeBasics>>;
  updateBasics: (field: keyof DirectChargeBasics, value: string) => void;
  consultCustomer: (automatic?: boolean) => Promise<void>;
  lookupPending: boolean;
  customer: FinancialCustomer | null;
  centers: Array<{ externalReference: string; name: string }>;
  clearReceipt: () => void;
  canContinueToDebts: boolean;
  continueToDebts: () => void;
}

export function DirectChargeCustomerStep({
  basics,
  setBasics,
  updateBasics,
  consultCustomer,
  lookupPending,
  customer,
  centers,
  clearReceipt,
  canContinueToDebts,
  continueToDebts,
}: DirectChargeCustomerStepProps) {
  return (
    <Box
      component="form"
      noValidate
      onSubmit={(event) => {
        event.preventDefault();
        continueToDebts();
      }}
      sx={chargeGrid}
    >
      <PrototypePanel title="Dados da cobrança" headerPaddingX={panelInset}>
        <Stack spacing={fieldGap} sx={{ p: panelInset }}>
          <Box
            sx={{
              display: "flex",
              gap: actionGap,
              alignItems: "center",
              width: "100%",
              flexWrap: "wrap",
            }}
          >
            <TaxIdField
              label="CPF/CNPJ"
              required
              size="small"
              fullWidth
              value={basics.document}
              onChange={(value) => updateBasics("document", value)}
              onBlur={() => void consultCustomer(true)}
              sx={{ width: 240, maxWidth: "100%" }}
            />
            <Button
              variant="contained"
              type="button"
              startIcon={<SearchOutlinedIcon />}
              onClick={() => void consultCustomer()}
              disabled={!/^(\d{11}|\d{14})$/.test(basics.document) || lookupPending}
            >
              Consultar
            </Button>
          </Box>
          <Box
            sx={{
              ...fieldGrid,
              gridTemplateColumns: { xs: "minmax(0, 1fr)", sm: "repeat(3, minmax(0, 1fr))" },
            }}
          >
            <TextField label="Nome" size="small" fullWidth disabled value={basics.customerName} />
            <TextField
              label="Telefone"
              placeholder="Opcional"
              size="small"
              fullWidth
              disabled={!customer || Boolean(customer.phone?.trim())}
              value={basics.customerPhone}
              onChange={(event) => {
                if (!customer?.phone?.trim()) updateBasics("customerPhone", event.target.value);
              }}
            />
            <TextField
              label="E-mail"
              placeholder="Opcional"
              size="small"
              fullWidth
              disabled={!customer || Boolean(customer.email?.trim())}
              value={basics.customerEmail}
              onChange={(event) => {
                if (!customer?.email?.trim()) updateBasics("customerEmail", event.target.value);
              }}
            />
          </Box>
          <Box
            sx={{
              ...fieldGrid,
              gridTemplateColumns: {
                xs: "minmax(0, 1fr)",
                sm: "repeat(2, minmax(0, 1fr))",
                md: "repeat(4, minmax(0, 1fr))",
              },
            }}
          >
            <TextField
              label="Contrato"
              required
              size="small"
              fullWidth
              value={basics.contract}
              onChange={(event) => updateBasics("contract", event.target.value)}
            />
            <PlateField
              label="Placa"
              required
              size="small"
              fullWidth
              value={basics.plate}
              onChange={(value) => updateBasics("plate", value)}
            />
            <DateField
              label="Vencimento"
              required
              size="small"
              fullWidth
              value={basics.dueDate}
              onChange={(value) => updateBasics("dueDate", value ?? "")}
            />
            <TextField
              select
              label="Centro de custo"
              required
              size="small"
              fullWidth
              value={basics.costCenterReference}
              onChange={(event) => {
                const center = centers.find(
                  (item) => item.externalReference === event.target.value,
                );
                clearReceipt();
                setBasics((current) => ({
                  ...current,
                  costCenterReference: center?.externalReference ?? "",
                  costCenter: center?.name ?? "",
                }));
              }}
            >
              {centers.map((center) => (
                <MenuItem key={center.externalReference} value={center.externalReference}>
                  {center.name}
                </MenuItem>
              ))}
            </TextField>
          </Box>
          <TextField
            label="Observação ao cliente"
            placeholder="Opcional"
            size="small"
            fullWidth
            multiline
            minRows={3}
            value={basics.note}
            onChange={(event) => updateBasics("note", event.target.value)}
          />
        </Stack>
      </PrototypePanel>
      {renderDirectChargeFooter(
        <>
          <Button
            component={Link}
            href="/home"
            variant="outlined"
            startIcon={<CloseOutlinedIcon />}
          >
            Cancelar
          </Button>
          <Button
            type="submit"
            variant="contained"
            startIcon={<PlaylistAddOutlinedIcon />}
            disabled={!canContinueToDebts}
          >
            Adicionar débitos
          </Button>
        </>,
      )}
    </Box>
  );
}
