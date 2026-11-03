import { expect, it } from "vitest";
import { formatJobDate, formatJobDuration, jobErrorMessage, jobStatusLabels } from "./job-format";
it("formats durations without fabricating absent dates", () => {
  expect(formatJobDate(null)).toBe("—");
  expect(formatJobDate("bad")).toBe("—");
  expect(formatJobDuration(null)).toBe("—");
  expect(formatJobDuration(42)).toBe("42s");
  expect(formatJobDuration(65)).toBe("1min 5s");
  expect(formatJobDuration(3665)).toBe("1h 1min");
  expect(formatJobDuration(-1)).toBe("0s");
  expect(Object.keys(jobStatusLabels)).toEqual(["Pending", "Processing", "Success", "Failed"]);
});

it("uses provider-neutral messages while preserving the retry guidance", () => {
  const indeterminate = jobErrorMessage("ChargeSettlementIndeterminate");
  const failed = jobErrorMessage("ChargeSettlementFailed");

  expect(indeterminate).toContain("sistema financeiro externo");
  expect(indeterminate).toContain("confira os recebimentos antes de tentar novamente");
  expect(failed).toContain("sistema financeiro externo");
  expect(indeterminate).not.toContain("Nibo");
  expect(failed).not.toContain("Nibo");
});
