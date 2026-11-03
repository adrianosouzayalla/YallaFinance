"use client";

import { useLayoutEffect, useRef, useState } from "react";
import Link from "next/link";
import { Box, IconButton, Tooltip, Typography } from "@mui/material";
import OpenInNewOutlinedIcon from "@mui/icons-material/OpenInNewOutlined";
import { SuccessStatusIcon } from "@/components/ui/SuccessStatusIcon";
import { PrototypePanel } from "@/components/ui/PrototypePanel";
import { formatCurrency } from "@/utils/currency";
import { formatDate } from "@/utils/date";
import { financeTokens } from "@/theme/designTokens";
import type { ChargeItem } from "../types/charge.types";

export type ChargePostingItem = ChargeItem & { formattedAmount?: string };

const maxVisiblePostings = 4;

export function ChargeItemPostings({
  charge,
  categories,
}: {
  charge: { items?: readonly ChargePostingItem[] };
  categories: readonly { id: string; name: string }[];
}) {
  const items = charge.items ?? [];
  const categoryNames = new Map(categories.map((category) => [category.id, category.name]));
  const listRef = useRef<HTMLOListElement>(null);
  const [listMaxHeight, setListMaxHeight] = useState<number>();
  const scrollable = items.length > maxVisiblePostings;

  useLayoutEffect(() => {
    const list = listRef.current;
    if (!list || !scrollable) return;

    const visibleItems = Array.from(list.children).slice(0, maxVisiblePostings);
    const lastVisibleItem = visibleItems.at(-1) as HTMLElement | undefined;
    if (!lastVisibleItem) return;

    const updateHeight = () => {
      const bottomPadding = parseFloat(getComputedStyle(list).paddingBottom) || 0;
      setListMaxHeight(lastVisibleItem.offsetTop + lastVisibleItem.offsetHeight + bottomPadding);
    };
    updateHeight();

    const observer = new ResizeObserver(updateHeight);
    observer.observe(list);
    visibleItems.forEach((item) => observer.observe(item));
    return () => observer.disconnect();
  }, [charge.items, scrollable]);

  return (
    <PrototypePanel
      title="Lançamentos por item"
      action={
        <Typography variant="body2" color="text.secondary">
          {items.length} lançamento{items.length === 1 ? "" : "s"}
        </Typography>
      }
    >
      <Box
        component="ol"
        ref={listRef}
        aria-label="Lançamentos por item"
        tabIndex={scrollable ? 0 : undefined}
        sx={{
          position: "relative",
          boxSizing: "border-box",
          width: "100%",
          listStyle: "none",
          m: 0,
          p: 1,
          display: "grid",
          gap: 1,
          gridTemplateColumns: "minmax(0, 1fr)",
          gridAutoRows: "max-content",
          alignContent: "start",
          maxHeight: scrollable ? listMaxHeight : undefined,
          overflowY: scrollable ? "scroll" : "auto",
          overflowX: "hidden",
          scrollbarGutter: scrollable ? "stable" : "auto",
          "&:focus-visible": {
            outline: "2px solid",
            outlineColor: "primary.main",
            outlineOffset: -2,
          },
        }}
      >
        {items.map((item) => {
          const confirmed = Boolean(item.providerExternalId);
          const detailRows = [
            [["Descrição", item.debt?.description || "Descrição não disponível"]],
            [
              ["Contrato", item.debt?.contract || "—"],
              ["Placa", item.debt?.plate || "—"],
              [
                "Ocorrência",
                item.debt?.occurrenceDate ? formatDate(item.debt.occurrenceDate) : "—",
              ],
            ],
            [
              ...(item.debt?.fleet ? [["Frota", item.debt.fleet]] : []),
              ...(item.costCenterName ? [["Centro de custo", item.costCenterName]] : []),
              ["Vencimento", item.debt?.dueDate ? formatDate(item.debt.dueDate) : "—"],
            ],
          ];
          return (
            <Box
              component="li"
              key={item.id}
              sx={{
                border: "1px solid",
                borderColor: "divider",
                borderRadius: 2,
                p: 1,
                minWidth: 0,
                width: "100%",
                boxSizing: "border-box",
                display: "flex",
                flexDirection: "column",
                overflow: "hidden",
              }}
            >
              <Box
                sx={{
                  mx: -1,
                  mt: -1,
                  px: 1,
                  py: 0.5,
                  backgroundColor: "action.hover",
                  display: "flex",
                  alignItems: "center",
                  gap: 1.5,
                  whiteSpace: "nowrap",
                  overflowX: "auto",
                }}
              >
                <Typography variant="caption" color="text.secondary">
                  {item.ordinal}
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  <Box component="span" sx={{ fontWeight: 700 }}>
                    Produto:
                  </Box>{" "}
                  {item.debt?.product || "—"}
                </Typography>
                <Typography
                  component="h3"
                  sx={{ fontSize: financeTokens.typography.body, fontWeight: 400 }}
                >
                  <Box component="span" sx={{ fontWeight: 700 }}>
                    Categoria:
                  </Box>{" "}
                  {categoryNames.get(item.categoryId) ?? "Categoria não disponível"}
                </Typography>
                {item.debtId ? (
                  <Tooltip title="Abrir débito">
                    <IconButton
                      component={Link}
                      href={`/debts/${encodeURIComponent(item.debtId)}`}
                      size="small"
                      aria-label="Abrir débito"
                      color="primary"
                      sx={{ p: 0.5, flexShrink: 0 }}
                    >
                      <OpenInNewOutlinedIcon fontSize="small" />
                    </IconButton>
                  </Tooltip>
                ) : null}
                {confirmed ? (
                  <Tooltip title="Lançamento criado no sistema financeiro">
                    <Box
                      component="span"
                      tabIndex={0}
                      aria-label="Lançamento criado no sistema financeiro"
                      sx={{ display: "inline-flex", color: "success.main", flexShrink: 0 }}
                    >
                      <SuccessStatusIcon fontSize="small" />
                    </Box>
                  </Tooltip>
                ) : null}

                <Typography
                  sx={{ ml: "auto", fontWeight: 700, fontSize: financeTokens.typography.body }}
                >
                  {item.formattedAmount ?? formatCurrency(item.amount)}
                </Typography>
              </Box>
              <Box
                sx={{
                  display: "flex",
                  flexDirection: "column",
                  gap: 0.25,
                  pt: 0.5,
                  minWidth: 0,
                }}
              >
                {detailRows.map((fields) => (
                  <Box
                    key={fields[0][0]}
                    sx={{
                      display: "grid",
                      gridTemplateColumns: {
                        xs: "minmax(0, 1fr)",
                        sm: `repeat(${fields.length}, minmax(0, 1fr))`,
                      },
                      alignItems: "baseline",
                      columnGap: 2,
                      rowGap: 0.25,
                      minWidth: 0,
                    }}
                  >
                    {fields.map(([field, value]) => (
                      <Box
                        key={field}
                        sx={{
                          display: "inline-flex",
                          alignItems: "baseline",
                          gap: 0.75,
                          minWidth: 0,
                          maxWidth: "100%",
                          width: "100%",
                        }}
                      >
                        <Typography
                          variant="caption"
                          color="text.secondary"
                          sx={{ fontWeight: 700, flexShrink: 0 }}
                        >
                          {field}:
                        </Typography>
                        <Typography
                          variant="caption"
                          title={field === "Descrição" ? value : undefined}
                          sx={{
                            fontWeight: 400,
                            minWidth: 0,
                            ...(field === "Descrição"
                              ? {
                                  flex: 1,
                                  whiteSpace: "nowrap",
                                  overflow: "hidden",
                                  textOverflow: "ellipsis",
                                }
                              : { overflowWrap: "anywhere" }),
                          }}
                        >
                          {value}
                        </Typography>
                      </Box>
                    ))}
                  </Box>
                ))}
              </Box>
            </Box>
          );
        })}
      </Box>
    </PrototypePanel>
  );
}
