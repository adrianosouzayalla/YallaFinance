import { useEffect, useRef } from "react";

type PaymentStatusRefreshOptions = {
  /** Refreshes the checkout data from the server. Background refreshes never block the screen. */
  refresh: () => void;
  /** Refresh as soon as the payer returns to the tab, while the payment is still open. */
  refreshWhenVisible: boolean;
  /** Interval for polling while a payment is awaited; null stops polling. */
  pollingIntervalMs: number | null;
};

/** Keeps the checkout up to date in the background: on tab return and while a payment is awaited. */
export function usePaymentStatusRefresh({ refresh, refreshWhenVisible, pollingIntervalMs }: PaymentStatusRefreshOptions) {
  // The latest refresh is read through a ref, so a new function identity does not restart the timers.
  const latestRefresh = useRef(refresh);
  useEffect(() => {
    latestRefresh.current = refresh;
  });

  useEffect(() => {
    if (!refreshWhenVisible) return;
    function refreshOnReturn() {
      if (document.visibilityState === "visible") latestRefresh.current();
    }
    document.addEventListener("visibilitychange", refreshOnReturn);
    return () => document.removeEventListener("visibilitychange", refreshOnReturn);
  }, [refreshWhenVisible]);

  useEffect(() => {
    if (pollingIntervalMs === null) return;
    const timer = window.setInterval(() => {
      if (document.visibilityState === "visible") latestRefresh.current();
    }, pollingIntervalMs);
    return () => window.clearInterval(timer);
  }, [pollingIntervalMs]);
}

/** Polling interval for an open attempt: Pix is confirmed faster than card and boleto. */
export function pollingIntervalFor(instrumentType: string | null | undefined): number {
  return instrumentType === "CreditCard" || instrumentType === "Boleto" ? 15_000 : 2_000;
}
