import { expect, it } from "vitest";
import { buildPaymentBatchInput } from "./paymentAttempt";

const ids = ["11111111-1111-4111-8111-111111111111"];
const form = { paymentMethodIds: ["22222222-2222-4222-8222-222222222222"], expiresDate: "2030-01-01", note: "note" };

it("keeps individual keys for identical retries and creates new keys for edited content", () => {
  const first = buildPaymentBatchInput(ids, form);
  expect(buildPaymentBatchInput(ids, form, first)).toEqual(first);
  const edited = buildPaymentBatchInput(ids, { ...form, note: "changed" }, first);
  expect(edited.note).toBe("changed");
  expect(edited.items[0].idempotencyKey).not.toBe(first.items[0].idempotencyKey);
});
