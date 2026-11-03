export function clearBrowserSessionData(): void {
  try {
    for (let index = window.sessionStorage.length - 1; index >= 0; index--) {
      const key = window.sessionStorage.key(index);
      if (key?.startsWith("yalla-finance.charges") || key?.startsWith("payment-intent:") || key?.startsWith("payment-failures:") || key === "yalla-finance.context")
        window.sessionStorage.removeItem(key);
    }
  } catch {
    /* Browser storage may be unavailable; the server session is still revoked. */
  }
}
