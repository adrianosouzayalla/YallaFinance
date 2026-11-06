import { useEffect, useState } from "react";

const MAX_TIMEOUT_MS = 2_147_483_647;

/**
 * Current time for the checkout view. It advances exactly when the nearest pending
 * expiration passes, so expired links and Pix codes switch state without polling.
 */
export function useCheckoutClock(expirationTimes: number[]): number {
  const [now, setNow] = useState(() => Date.now());
  const pendingExpirations = expirationTimes.filter((expiresAt) => expiresAt > now);
  const nextExpiration =
    pendingExpirations.length > 0 ? Math.min(...pendingExpirations) : undefined;

  useEffect(() => {
    if (nextExpiration === undefined) return;
    // Re-runs on every tick, so timers longer than the browser limit are rescheduled until they pass.
    const timer = window.setTimeout(
      () => setNow(Date.now()),
      Math.min(nextExpiration - Date.now(), MAX_TIMEOUT_MS),
    );
    return () => window.clearTimeout(timer);
  }, [nextExpiration, now]);

  return now;
}
