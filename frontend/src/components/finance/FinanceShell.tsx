"use client";

import type { PropsWithChildren } from "react";
import { useState } from "react";
import { Box, Drawer, useMediaQuery } from "@mui/material";
import { useTheme } from "@mui/material/styles";
import { financeTokens } from "@/theme/designTokens";
import { FinanceHeader } from "./FinanceHeader";
import { FinanceSidebar } from "./FinanceSidebar";
import type { FinanceSession } from "./FinanceSessionSummary";
import { FunctionalityAccessBoundary } from "./FunctionalityAccessBoundary";

export function FinanceShell({ children, session }: PropsWithChildren<{ session?: FinanceSession }>) {
  const theme = useTheme();
  const isMobile = useMediaQuery(theme.breakpoints.down("md"), { noSsr: true });
  const [mobileNavigationOpen, setMobileNavigationOpen] = useState(false);
  const closeMobileNavigation = () => setMobileNavigationOpen(false);

  return (
    <Box sx={{ display: "grid", gridTemplateRows: `${financeTokens.shell.headerHeight} minmax(0, 1fr)`, height: financeTokens.shell.viewportHeight, overflow: "hidden" }}>
      <FinanceHeader onMenuClick={isMobile ? () => setMobileNavigationOpen(true) : undefined} isMobileNavigationOpen={mobileNavigationOpen} />
      <Box sx={{ display: "grid", gridTemplateColumns: { md: `${financeTokens.shell.sidebarWidth} minmax(0, 1fr)`, xs: "minmax(0, 1fr)" }, minHeight: 0, overflow: "hidden" }}>
        {isMobile ? (
          <Drawer ModalProps={{ keepMounted: true }} onClose={closeMobileNavigation} open={mobileNavigationOpen}>
            <Box sx={{ height: financeTokens.shell.viewportHeight, width: financeTokens.shell.sidebarWidth }}><FinanceSidebar onNavigate={closeMobileNavigation} session={session} /></Box>
          </Drawer>
        ) : <FinanceSidebar session={session} />}
        <Box component="main" sx={{ backgroundColor: financeTokens.colors.canvas, minWidth: 0, minHeight: 0, overflow: "auto", overscrollBehavior: "contain", p: financeTokens.shell.contentPadding }}>
          <FunctionalityAccessBoundary session={session}>{children}</FunctionalityAccessBoundary>
        </Box>
      </Box>
    </Box>
  );
}
