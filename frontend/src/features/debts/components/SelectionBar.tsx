import Link from "next/link";
import { Alert, Box, Button, Stack, Typography } from "@mui/material";
import { financeTokens } from "@/theme/designTokens";

interface SelectionBarProps {
  count: number;
  reviewUrl?: string;
  total: string;
  tenantName?: string;
  validationMessage?: string;
  onClear?(): void;
}

export function SelectionBar({
  count,
  reviewUrl,
  tenantName,
  total,
  validationMessage,
  onClear,
}: SelectionBarProps) {
  if (count === 0) {
    return null;
  }

  const action = reviewUrl ? (
    <Button component={Link} href={reviewUrl} variant="contained">
      Cobrar selecionados
    </Button>
  ) : (
    <Button disabled variant="contained">
      Cobrar selecionados
    </Button>
  );

  return (
    <Box
      aria-live="polite"
      sx={{ borderTop: "1px solid", borderColor: "divider", px: 2.5, py: 1.5 }}
    >
      <Stack
        direction="row"
        spacing={2}
        sx={{ alignItems: "center", justifyContent: "space-between" }}
      >
        <Stack direction="row" spacing={1} sx={{ alignItems: "baseline" }}>
          <Typography sx={{ fontSize: financeTokens.typography.body, fontWeight: 700 }}>
            {count} {count === 1 ? "débito selecionado" : "débitos selecionados"}
          </Typography>
          <Typography sx={{ fontSize: financeTokens.typography.secondary }}>
            Total: {total}
          </Typography>
          {tenantName ? (
            <Typography sx={{ fontSize: financeTokens.typography.secondary }}>
              Locatário: {tenantName}
            </Typography>
          ) : null}
        </Stack>
        <Stack direction="row" spacing={1}>
          {onClear ? <Button onClick={onClear}>Limpar seleção</Button> : null}
          {action}
        </Stack>
      </Stack>
      {validationMessage ? (
        <Alert severity="warning" sx={{ mt: 1.25 }}>
          {validationMessage}
        </Alert>
      ) : null}
    </Box>
  );
}
