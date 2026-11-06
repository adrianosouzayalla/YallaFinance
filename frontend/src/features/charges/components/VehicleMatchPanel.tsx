import {
  Alert,
  Box,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  Typography,
} from "@mui/material";
import { PrototypePanel } from "@/components/ui/PrototypePanel";
import { financeTokens } from "@/theme/designTokens";
import type { FleetMovement } from "@/services/api/customers/customer.types";

interface VehicleMatchPanelProps {
  loading: boolean;
  movements: FleetMovement[];
  onSelect: (movementId: string) => void;
  selectedMovementId?: string;
}

const noVehicleMessage =
  "Nenhum veículo encontrado para este locatário na data informada. Verifique a movimentação de frota no Yalla Digital.";

export function VehicleMatchPanel({
  loading,
  movements,
  onSelect,
  selectedMovementId,
}: VehicleMatchPanelProps) {
  return (
    <>
      <PrototypePanel
        borderStyle="dashed"
        title="Veículo no momento da ocorrência (automático, Yalla Digital)"
      >
        <Box sx={{ px: 2.5, py: 2 }}>
          {loading ? null : movements.length === 0 ? (
            <Alert severity="warning">{noVehicleMessage}</Alert>
          ) : movements.length === 1 ? (
            <VehicleDetails movement={movements[0]} />
          ) : (
            <FormControl fullWidth size="small">
              <InputLabel id="movement-selection-label">Placa do veículo</InputLabel>
              <Select
                labelId="movement-selection-label"
                label="Placa do veículo"
                value={selectedMovementId ?? ""}
                onChange={(event) => onSelect(event.target.value)}
              >
                <MenuItem value="" disabled>
                  Selecione uma placa
                </MenuItem>
                {movements.map((movement) => (
                  <MenuItem key={movement.id} value={movement.id}>
                    {movement.plate} — {movement.contract} ({movement.product})
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
          )}
        </Box>
      </PrototypePanel>
    </>
  );
}

function VehicleDetails({ movement }: { movement: FleetMovement }) {
  const occurrenceDate = movement.occurrenceDate.split("-").reverse().join("/");
  const rows = [
    ["Placa", movement.plate],
    ["Contrato", `${movement.contract} (${movement.product})`],
    ["Frota", movement.fleet],
    ["Fonte", `Movimentação de frota em ${occurrenceDate}`],
  ];

  return (
    <Stack spacing={1}>
      {rows.map(([label, value], index) => (
        <Box
          key={label}
          sx={{ display: "grid", gap: 2, gridTemplateColumns: "minmax(0, 1fr) minmax(0, 1fr)" }}
        >
          <Typography color="text.secondary" sx={{ fontSize: financeTokens.typography.secondary }}>
            {label}
          </Typography>
          <Typography
            sx={{
              fontSize: financeTokens.typography.body,
              fontWeight: index === 0 ? 700 : 500,
              textAlign: "right",
            }}
          >
            {value}
          </Typography>
        </Box>
      ))}
    </Stack>
  );
}
