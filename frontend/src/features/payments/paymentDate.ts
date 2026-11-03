import { isValidDateOnly } from "@/utils/date";

/** Timestamps from the database are UTC; date filters and display use the operational timezone. */
export function paymentDateOnly(timestamp: string): string {
  const utc = /(?:Z|[+-]\d{2}:\d{2})$/.test(timestamp) ? timestamp : `${timestamp}Z`;
  const parts = new Intl.DateTimeFormat("en-CA", { timeZone: "America/Sao_Paulo", year: "numeric", month: "2-digit", day: "2-digit" }).formatToParts(new Date(utc));
  const part = (name: string) => parts.find((value) => value.type === name)?.value;
  return `${part("year")}-${part("month")}-${part("day")}`;
}

export function paymentExpirationAt(date: string): string {
  return new Date(`${date}T23:59:59.999-03:00`).toISOString();
}

export function paymentExpirationError(date: string, maximumLinkDays: number, now = new Date(), dueDates: readonly string[] = []): string | null {
  if (!date) return "Informe a validade do link.";
  if (!isValidDateOnly(date)) return "Informe uma validade válida.";
  if (dueDates.some((dueDate) => !isValidDateOnly(dueDate))) return "Confira o vencimento das cobranças.";
  if (dueDates.some((dueDate) => date > dueDate)) return "A validade do link não pode ultrapassar o vencimento das cobranças.";
  const deadline = new Date(paymentExpirationAt(date)).getTime();
  if (deadline <= now.getTime()) return "Informe uma validade futura.";
  if (deadline > now.getTime() + maximumLinkDays * 24 * 60 * 60 * 1000)
    return `A validade deve respeitar o limite de ${maximumLinkDays} dias desta empresa.`;
  return null;
}

export function paymentMaximumDate(maximumLinkDays: number, now = new Date(), dueDates: readonly string[] = []): string {
  const dayMilliseconds = 24 * 60 * 60 * 1000;
  const limit = new Date(now.getTime() + maximumLinkDays * dayMilliseconds);
  const lastDay = paymentDateOnly(limit.toISOString());
  const tenantLimit = new Date(paymentExpirationAt(lastDay)).getTime() <= limit.getTime()
    ? lastDay : paymentDateOnly(new Date(limit.getTime() - dayMilliseconds).toISOString());
  return dueDates.reduce((maximum, dueDate) => dueDate < maximum ? dueDate : maximum, tenantLimit);
}
