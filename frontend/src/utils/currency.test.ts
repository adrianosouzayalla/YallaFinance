import { expect, it } from "vitest";
import { formatCurrency, formatCurrencyInput } from "./currency";

it.each([
  [1234.56, "1.234,56"],
  [0.01, "0,01"],
  [0, "0,00"],
  [-10.5, "-10,50"],
  [null, ""],
])("formats currency inputs consistently: %s", (input, expected) => {
  expect(formatCurrencyInput(input)).toBe(expected);
});
it("formats display values with currency and keeps empty values empty", () => {
  expect(formatCurrency(1234.56).replace(/\s/g, " ")).toBe("R$ 1.234,56");
  expect(formatCurrency(null)).toBe("");
});
