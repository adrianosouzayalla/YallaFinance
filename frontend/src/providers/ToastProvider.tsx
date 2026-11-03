"use client";

import { Alert, type AlertColor, Snackbar } from "@mui/material";
import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useState,
  type PropsWithChildren,
  type ReactNode,
} from "react";
import { financeTokens } from "@/theme/designTokens";

const DEFAULT_TOAST_DURATION = 4_500;

export interface ToastOptions {
  message: ReactNode;
  severity?: AlertColor;
  duration?: number;
}

interface ToastContextValue {
  showToast(options: ToastOptions): void;
  closeToast(): void;
}

interface ToastState extends ToastOptions {
  id: number;
}

const ToastContext = createContext<ToastContextValue | undefined>(undefined);

export function ToastProvider({ children }: PropsWithChildren) {
  const [toast, setToast] = useState<ToastState | null>(null);

  const closeToast = useCallback(() => {
    setToast(null);
  }, []);

  const showToast = useCallback((options: ToastOptions) => {
    setToast((current) => ({
      ...options,
      id: (current?.id ?? 0) + 1,
    }));
  }, []);

  const contextValue = useMemo<ToastContextValue>(
    () => ({ closeToast, showToast }),
    [closeToast, showToast],
  );

  return (
    <ToastContext.Provider value={contextValue}>
      {children}
      <Snackbar
        anchorOrigin={{ horizontal: "right", vertical: "bottom" }}
        autoHideDuration={toast?.duration ?? DEFAULT_TOAST_DURATION}
        key={toast?.id}
        open={toast !== null}
        onClose={closeToast}
      >
        {toast ? (
          <Alert
            severity={toast.severity ?? "info"}
            variant="filled"
            onClose={closeToast}
            sx={{
              borderRadius: `${financeTokens.radius.control}px`,
              boxShadow: "0 12px 28px rgba(7, 41, 101, 0.18)",
              minWidth: { sm: 360 },
              width: "100%",
            }}
          >
            {toast.message}
          </Alert>
        ) : undefined}
      </Snackbar>
    </ToastContext.Provider>
  );
}

export function useToast(): ToastContextValue {
  const context = useContext(ToastContext);

  if (context === undefined) {
    throw new Error("useToast must be used within a ToastProvider");
  }

  return context;
}
