const uuid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

export function parsePaymentChargeIds(value: string | null): string[] {
  const ids = [
    ...new Set(
      (value ?? "")
        .split(",")
        .filter(Boolean)
        .map((id) => id.toLowerCase()),
    ),
  ].sort();
  if (
    !ids.length ||
    ids.length > 100 ||
    ids.some((id) => !uuid.test(id) || id === "00000000-0000-0000-0000-000000000000")
  ) {
    throw new Error("Selecione de 1 a 100 cobranças válidas.");
  }
  return ids;
}

/** Every origin uses this route; the generation page always reloads authoritative data. */
export function buildPaymentEntryUrl(chargeIds: readonly string[]): string {
  return `/payments/new?chargeIds=${parsePaymentChargeIds(chargeIds.join(",")).join(",")}`;
}
