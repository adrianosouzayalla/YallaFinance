"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { Box, List, ListItem, ListItemButton, ListItemIcon, ListItemText } from "@mui/material";
import { financeTokens } from "@/theme/designTokens";
import { FinanceSessionSummary, type FinanceSession } from "./FinanceSessionSummary";
import { financeNavigation, getActiveNavigationItem } from "./navigation";

type FinanceSidebarProps = { session?: FinanceSession; onNavigate?: () => void };

export function FinanceSidebar({ session, onNavigate }: FinanceSidebarProps) {
  const pathname = usePathname();
  const activeItem = getActiveNavigationItem(pathname);
  return (
    <Box
      component="aside"
      sx={{
        backgroundColor: financeTokens.colors.sidebar,
        display: "flex",
        flexDirection: "column",
        height: "100%",
        minHeight: 0,
        overflow: "hidden",
        width: "100%",
      }}
    >
      <Box
        component="nav"
        aria-label="Navegação principal"
        sx={{ flex: 1, minHeight: 0, overflowY: "auto", py: 1 }}
      >
        <List disablePadding>
          {financeNavigation
            .filter(
              (destination) =>
                destination.alwaysVisible ||
                (destination.functionalityCode !== undefined &&
                  session?.functionalityCodes?.includes(destination.functionalityCode)),
            )
            .map((destination) => {
              const active = activeItem.href === destination.href;
              const Icon = destination.Icon;
              return (
                <ListItem disablePadding key={destination.href}>
                  <ListItemButton
                    aria-current={active ? "page" : undefined}
                    component={Link}
                    href={destination.href}
                    onClick={onNavigate}
                    selected={active}
                    sx={{
                      borderLeft: `4px solid ${active ? financeTokens.colors.header : "transparent"}`,
                      minHeight: financeTokens.density.navigationItemHeight,
                      px: `${financeTokens.density.navigationItemHorizontalPadding}px`,
                      py: `${financeTokens.density.navigationItemVerticalPadding}px`,
                      "&.Mui-selected": {
                        backgroundColor: financeTokens.colors.activeNavigation,
                        color: financeTokens.colors.header,
                      },
                      "&.Mui-selected:hover": {
                        backgroundColor: financeTokens.colors.activeNavigation,
                      },
                    }}
                  >
                    <ListItemIcon sx={{ color: "inherit", minWidth: 34 }}>
                      <Icon fontSize="small" />
                    </ListItemIcon>
                    <ListItemText
                      primary={destination.label}
                      slotProps={{
                        primary: {
                          sx: {
                            fontSize: financeTokens.typography.navigationLabel,
                            fontWeight: active ? 700 : 500,
                          },
                        },
                      }}
                    />
                  </ListItemButton>
                </ListItem>
              );
            })}
        </List>
      </Box>
      <FinanceSessionSummary session={session} />
    </Box>
  );
}
