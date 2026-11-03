import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, expect, it, vi } from "vitest";
import { CurrencyField } from "./CurrencyField";
afterEach(cleanup);
it("preserves every cent in the numeric(18,2) range in exact decimal mode", () => {
  const changed = vi.fn();
  render(
    <CurrencyField exactDecimal label="Valor exato" value="90071992547409.91" onChange={changed} />,
  );
  expect(screen.getByRole("textbox")).toHaveValue("90.071.992.547.409,91");
  fireEvent.change(screen.getByRole("textbox"), { target: { value: "9.999.999.999.999.999,99" } });
  expect(changed).toHaveBeenCalledWith("9999999999999999.99");
});
