"use client";

import { Box, IconButton, Typography } from "@mui/material";
import MenuOutlinedIcon from "@mui/icons-material/MenuOutlined";
import { usePathname } from "next/navigation";
import { financeTokens } from "@/theme/designTokens";
import { getBreadcrumbItems } from "./navigation";

type FinanceHeaderProps = { onMenuClick?: () => void; isMobileNavigationOpen?: boolean };

export function FinanceHeader({ onMenuClick, isMobileNavigationOpen = false }: FinanceHeaderProps) {
  const breadcrumbs = getBreadcrumbItems(usePathname());
  return (
    <Box
      component="header"
      sx={{
        alignItems: "center",
        backgroundColor: financeTokens.colors.header,
        color: financeTokens.colors.canvas,
        display: { md: "grid", xs: "flex" },
        gap: { xs: 1, sm: 1.5 },
        gridTemplateColumns: { md: `${financeTokens.shell.sidebarWidth} minmax(0, 1fr)` },
        height: financeTokens.shell.headerHeight,
        px: { xs: 1, md: 0 },
      }}
    >
      {onMenuClick && (
        <IconButton
          aria-expanded={isMobileNavigationOpen}
          aria-label="Abrir menu"
          onClick={onMenuClick}
          sx={{ color: "inherit", display: { md: "none" } }}
        >
          <MenuOutlinedIcon />
        </IconButton>
      )}
      <Box
        component="span"
        sx={{
          alignItems: "center",
          borderRight: { md: `1px solid ${financeTokens.overlays.headerDivider}` },
          display: "flex",
          flexShrink: 0,
          height: "100%",
          px: { md: 1.5 },
        }}
      >
        <Box
          component="img"
          src="/assets/brand/yallafinance-logo-dark.svg"
          alt="Yalla Finance"
          sx={{
            display: "block",
            height: "auto",
            maxHeight: 26,
            width: "clamp(104px, 9.2vw, 124px)",
          }}
        />
      </Box>
      <Box
        component="nav"
        aria-label="Breadcrumb"
        sx={{
          alignItems: "center",
          display: "flex",
          minWidth: 0,
          overflow: "hidden",
          px: { md: 1.5 },
        }}
      >
        {breadcrumbs.map((item, index) => (
          <Box
            key={`${item.label}-${index}`}
            sx={{ alignItems: "center", display: "flex", minWidth: 0 }}
          >
            {index > 0 && (
              <Typography
                aria-hidden="true"
                sx={{ color: financeTokens.overlays.headerBreadcrumb, mx: 1 }}
              >
                /
              </Typography>
            )}
            {item.href ? (
              <Typography
                component="a"
                href={item.href}
                sx={{
                  color: "inherit",
                  fontSize: financeTokens.typography.navigationLabel,
                  textDecoration: "none",
                }}
              >
                {item.label}
              </Typography>
            ) : (
              <Typography
                noWrap
                sx={{ fontSize: financeTokens.typography.navigationLabel, fontWeight: 700 }}
              >
                {item.label}
              </Typography>
            )}
          </Box>
        ))}
      </Box>
    </Box>
  );
}
