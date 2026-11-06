import { Box, IconButton, Stack, Tooltip, Typography } from "@mui/material";
import DeleteOutlinedIcon from "@mui/icons-material/DeleteOutlined";
import EditOutlinedIcon from "@mui/icons-material/EditOutlined";
import { PrototypePanel } from "@/components/ui/PrototypePanel";
import {
  SelectablePaginatedTable,
  type SelectableTableColumn,
} from "@/components/ui/SelectablePaginatedTable";
import { financeTokens } from "@/theme/designTokens";
import { formatDate } from "@/utils/date";
import {
  composeDirectDebtDescription,
  formatDirectAmount,
  type DirectDebtDraft,
} from "./directChargeDraft";
import { debtsPerPage } from "./directChargeLayout";
import { fieldGap, panelInset } from "@/theme/layout";

interface DirectChargeDebtPanelProps {
  title: string;
  showDescription?: boolean;
  editable?: boolean;
  debts: DirectDebtDraft[];
  contract: string;
  total: string;
  page: number;
  onPageChange: (page: number) => void;
  onEdit: (debt: DirectDebtDraft) => void;
  onRemove: (debt: DirectDebtDraft) => void;
}

export function DirectChargeDebtPanel({
  title,
  showDescription = false,
  editable = false,
  debts,
  contract,
  total,
  page,
  onPageChange,
  onEdit,
  onRemove,
}: DirectChargeDebtPanelProps) {
  function renderTable(showDescription: boolean, editable: boolean) {
    const columns: SelectableTableColumn<DirectDebtDraft>[] = showDescription
      ? [
          { id: "category", header: "Categoria", render: (debt) => debt.category },
          {
            id: "description",
            header: "Descrição",
            render: (debt) => (
              <Box component="span" sx={{ overflowWrap: "anywhere" }}>
                {composeDirectDebtDescription(debt, contract)}
              </Box>
            ),
            getSortValue: (debt) => composeDirectDebtDescription(debt, contract),
          },
        ]
      : [
          { id: "category", header: "Categoria", render: (debt) => debt.category },
          {
            id: "detail",
            header: "Detalhe",
            render: (debt) => (
              <Box component="span" sx={{ overflowWrap: "anywhere" }}>
                {debt.detail}
              </Box>
            ),
            getSortValue: (debt) => debt.detail,
          },
        ];
    columns.push(
      {
        id: "occurrenceDate",
        header: "Ocorrência",
        render: (debt) => (
          <Box component="span" sx={{ whiteSpace: "nowrap" }}>
            {formatDate(debt.occurrenceDate)}
          </Box>
        ),
        getSortValue: (debt) => debt.occurrenceDate,
      },
      {
        id: "amount",
        header: "Valor",
        align: "right",
        render: (debt) => (
          <Box component="span" sx={{ whiteSpace: "nowrap" }}>
            {formatDirectAmount(debt.amount)}
          </Box>
        ),
        getSortValue: (debt) => Number(debt.amount),
      },
    );
    if (editable)
      columns.push({
        id: "actions",
        header: "Ações",
        sortable: false,
        align: "right",
        render: (debt) => (
          <Stack direction="row" spacing={0.5} sx={{ justifyContent: "flex-end" }}>
            <Tooltip title="Editar débito">
              <IconButton
                aria-label={`Editar débito ${debt.id}`}
                color="primary"
                size="small"
                onClick={() => onEdit(debt)}
              >
                <EditOutlinedIcon fontSize="small" />
              </IconButton>
            </Tooltip>
            <Tooltip title="Remover débito">
              <IconButton
                aria-label={`Remover débito ${debt.id}`}
                color="error"
                size="small"
                onClick={() => onRemove(debt)}
              >
                <DeleteOutlinedIcon fontSize="small" />
              </IconButton>
            </Tooltip>
          </Stack>
        ),
      });
    return (
      <SelectablePaginatedTable
        rows={debts}
        columns={columns}
        getRowId={(debt) => debt.id}
        showSelection={false}
        selectedRows={[]}
        onSelectionChange={() => {}}
        paginationMode="client"
        tableLabel="Débitos da Cobrança Direta"
        headerSx={{ backgroundColor: financeTokens.colors.tableHeader }}
        emptyContent="Nenhum débito adicionado."
        pagination={{
          page: Math.min(page, Math.max(1, Math.ceil(debts.length / debtsPerPage))),
          pageSize: debtsPerPage,
          totalItems: debts.length,
          onPageChange: onPageChange,
          label: "Paginação de débitos",
        }}
      />
    );
  }

  function renderDebtPanel(title: string, showDescription = false, editable = false) {
    return (
      <PrototypePanel
        title={title}
        headerPaddingX={panelInset}
        action={
          <Typography color="text.secondary" sx={{ fontSize: financeTokens.typography.secondary }}>
            {debts.length} {debts.length === 1 ? "débito" : "débitos"}
          </Typography>
        }
      >
        {renderTable(showDescription, editable)}
        <Box
          sx={{
            display: "flex",
            justifyContent: "space-between",
            gap: fieldGap,
            px: panelInset,
            py: fieldGap,
          }}
        >
          <Typography component="p" variant="h2">
            Total a cobrar
          </Typography>
          <Typography
            component="p"
            variant="h2"
            sx={{ color: financeTokens.colors.header, whiteSpace: "nowrap" }}
          >
            {formatDirectAmount(total)}
          </Typography>
        </Box>
      </PrototypePanel>
    );
  }
  return renderDebtPanel(title, showDescription, editable);
}
