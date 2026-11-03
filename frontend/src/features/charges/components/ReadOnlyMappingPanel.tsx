import { Box, Stack, Typography } from "@mui/material";
import { PrototypePanel } from "@/components/ui/PrototypePanel";
import { financeTokens } from "@/theme/designTokens";
import type { FleetMovement } from "@/features/customers/types/customer.types";

interface ReadOnlyMappingPanelProps {
  category: string;
  description: string;
  movement?: FleetMovement;
  variant?: "standalone" | "review";
}

function standardizedDescription(category: string, description: string, contract: string): string {
  return `${category.toLocaleUpperCase("pt-BR")} | ${description} | ${contract}`;
}

export function ReadOnlyMappingPanel({
  category,
  description,
  movement,
  variant = "standalone",
}: ReadOnlyMappingPanelProps) {
  //TODO: Adriano Substituir os textos ilustrativos por uma projeção dos mappings persistidos no banco.
  const fleet = movement?.fleet ?? "";
  const contract = movement?.contract ?? "";
  const mappingRows =
    variant === "review"
      ? [
          ["Centro de custo", movement?.costCenter ?? ""],
          ["Categoria financeira", "a parametrizar (Processos)"],
          ["Descrição", "padrão Yalla Finance"],
        ]
      : [
          ["Lançamento no sistema financeiro", "Será criado nesta cobrança"],
          ["Centro de custo", `${fleet} (frota da placa identificada)`],
          ["Categoria financeira", "definida pela categoria + placa"],
          ["Descrição", standardizedDescription(category, description, contract)],
        ];

  return (
    <PrototypePanel borderStyle="dashed" title="Parametrização financeira (automática, somente leitura)">
      <Stack spacing={0} sx={{ px: 2.5, py: 1 }}>
        {mappingRows.map(([label, value]) => (
          <Box
            key={label}
            sx={{
              display: "grid",
              gap: 2,
              gridTemplateColumns: "minmax(0, 1fr) minmax(0, 1fr)",
              py: 1.25,
            }}
          >
            <Typography color="text.secondary" sx={{ fontSize: financeTokens.typography.secondary }}>
              {label}
            </Typography>
            <Typography sx={{ fontSize: financeTokens.typography.body, fontWeight: 600, textAlign: "right" }}>
              {value}
            </Typography>
          </Box>
        ))}
      </Stack>
    </PrototypePanel>
  );
}
