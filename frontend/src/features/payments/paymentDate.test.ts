import { expect, it } from "vitest";
import { paymentDateOnly, paymentExpirationError, paymentMaximumDate } from "./paymentDate";

it("shows creation and validity in the same operational day as the date filter", () => {
  expect(paymentDateOnly("2026-10-10T02:59:59Z")).toBe("2026-10-09");
  expect(paymentDateOnly("2026-10-10T03:00:00")).toBe("2026-10-10");
});

it("limits a batch to the earliest due date and still enforces the tenant maximum", () => {
  const now = new Date("2026-10-15T15:00:00Z");
  expect(paymentMaximumDate(30, now, ["2026-10-20", "2026-10-18"])).toBe("2026-10-18");
  expect(paymentMaximumDate(2, now, ["2026-10-20"])).toBe("2026-10-16");
  expect(paymentExpirationError("2026-10-19", 30, now, ["2026-10-18"])).toContain("vencimento");
  expect(paymentExpirationError("2026-10-18", 30, now, ["2026-10-18"])).toBeNull();
  expect(paymentExpirationError("2026-10-15", 30, now, ["2026-10-14"])).toContain("vencimento");
});
