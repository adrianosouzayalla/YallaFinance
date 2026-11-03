import { describe, expect, it } from "vitest";
import { buildPaymentEntryUrl, parsePaymentChargeIds } from "./paymentEntry";

const one = "11111111-1111-4111-8111-111111111111";
const two = "22222222-2222-4222-8222-222222222222";

describe("payment entry from any origin", () => {
  it("accepts one charge from completion without requiring list state", () => {
    expect(buildPaymentEntryUrl([one])).toBe(`/payments/new?chargeIds=${one}`);
  });
  it("deduplicates a multiple selection and recovers it from the route", () => {
    const url = new URL(buildPaymentEntryUrl([two, one, one]), "https://unit.example");
    expect(parsePaymentChargeIds(url.searchParams.get("chargeIds"))).toEqual([one, two]);
  });
  it("rejects empty or invalid selection instead of entering generation", () => {
    expect(() => buildPaymentEntryUrl([])).toThrow();
    expect(() => parsePaymentChargeIds("invalid")).toThrow();
  });
});
