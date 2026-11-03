"use client";

import type { PropsWithChildren } from "react";
import { CssBaseline, ThemeProvider } from "@mui/material";
import { AppRouterCacheProvider } from "@mui/material-nextjs/v16-appRouter";
import { theme } from "@/theme/theme";
import { ToastProvider } from "./ToastProvider";
import { QueryProvider } from "./QueryProvider";

export function AppProviders({ children }: PropsWithChildren) {
  return (
    <AppRouterCacheProvider options={{ key: "yallafinance" }}>
      <ThemeProvider theme={theme}>
        <CssBaseline />
        <QueryProvider>
          <ToastProvider>{children}</ToastProvider>
        </QueryProvider>
      </ThemeProvider>
    </AppRouterCacheProvider>
  );
}
