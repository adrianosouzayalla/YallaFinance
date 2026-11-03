import { expect, it } from "vitest";
import { paymentRequestSchema } from "./payment-request.schema";

it("requires payment methods and valid expiration independently of charge method", () => {
  const value = { paymentMethodIds: ["11111111-1111-4111-8111-111111111111"], expiresDate: "2030-02-28", note: "" };
  expect(paymentRequestSchema.safeParse(value).success).toBe(true);
  expect(paymentRequestSchema.safeParse({ ...value, paymentMethodIds: [] }).success).toBe(false);
  expect(paymentRequestSchema.safeParse({ ...value, paymentMethodIds: [value.paymentMethodIds[0], value.paymentMethodIds[0]] }).success).toBe(false);
  expect(paymentRequestSchema.safeParse({ ...value, paymentMethodIds: ["00000000-0000-0000-0000-000000000000"] }).success).toBe(false);
  expect(paymentRequestSchema.safeParse({ ...value, paymentMethodIds: [...value.paymentMethodIds, "22222222-2222-4222-8222-222222222222"] }).success).toBe(true);
  expect(paymentRequestSchema.safeParse({ ...value, expiresDate: "2030-02-30" }).success).toBe(false);
  expect(paymentRequestSchema.safeParse({ ...value, note: "x".repeat(2001) }).success).toBe(false);
});
