import { expect, it } from "vitest";
import { isChargeReference } from "./chargeReference";

it.each(["1-1", "10-123", "9223372036854775807-9223372036854775807"])("accepts exact bigint reference %s", (value) => {
  expect(isChargeReference(value)).toBe(true);
});

it.each(["", "123", "0-1", "1-0", "01-1", "1-01", "1-1 ", "+1-1", "1--1", "1-1-1",
  "1-9223372036854775808", "9223372036854775808-1", "1-1e3"])("rejects invalid reference %s", (value) => {
  expect(isChargeReference(value)).toBe(false);
});
