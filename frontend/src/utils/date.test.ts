import { expect, it } from "vitest";
import { formatDate, parseDateInput } from "./date";

it.each([
  ["03/10/2026", "2026-10-03"],
  ["29/02/2024", "2024-02-29"],
  ["29/02/2026", null],
  ["31/04/2026", null],
  ["00/10/2026", null],
  ["03/13/2026", null],
  ["03/10", null],
  ["", null],
  ["01/01/0000", null],
])("normalizes a date without accepting impossible or incomplete dates: %s", (input, expected) => {
  expect(parseDateInput(input)).toBe(expected);
});

it.each([
  ["2026-10-03", "03/10/2026"],
  ["2024-02-29", "29/02/2024"],
  [null, ""],
  ["", ""],
  ["2026-02-29", ""],
  ["not-a-date", ""],
])("formats date-only values without timezone conversion: %s", (input, expected) => {
  expect(formatDate(input)).toBe(expected);
});
