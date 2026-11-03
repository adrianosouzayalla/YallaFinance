const currencyFormatter = new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" });
const inputFormatter = new Intl.NumberFormat("pt-BR", {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

export function formatCurrency(value: number | null | undefined): string {
  return value == null || !Number.isFinite(value) ? "" : currencyFormatter.format(value);
}

export function formatCurrencyInput(value: number | null | undefined): string {
  return value == null || !Number.isFinite(value) ? "" : inputFormatter.format(value);
}

/** Decimal text avoids Number rounding for the full PostgreSQL numeric(18,2) range. */
export function decimalToCents(value: string): string {
  if (!/^-?\d+(\.\d{1,2})?$/.test(value)) return "";
  const [whole, fraction = ""] = value.split(".");
  return `${whole}${fraction.padEnd(2, "0")}`;
}

export function centsToDecimal(value: string): string | null {
  if (!/^-?\d+$/.test(value)) return null;
  const negative = value.startsWith("-");
  const digits = value
    .replace(/^-/, "")
    .replace(/^0+(?=\d)/, "")
    .padStart(3, "0");
  return `${negative ? "-" : ""}${digits.slice(0, -2)}.${digits.slice(-2)}`;
}

export function formatExactCurrencyInput(cents: string): string {
  const decimal = centsToDecimal(cents);
  if (decimal === null) return cents === "-" ? "-" : "";
  const [whole, fraction] = decimal.split(".");
  return `${whole.replace(/\B(?=(\d{3})+(?!\d))/g, ".")},${fraction}`;
}
