import { useState } from "react";
import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, expect, it } from "vitest";
import { PlateField } from "./PlateField";
import userEvent from "@testing-library/user-event";
import { isValidPlate } from "@/utils/plate";

afterEach(cleanup);
function Field() {
  const [value, setValue] = useState("");
  return (
    <>
      <PlateField value={value} onChange={setValue} required size="small" />
      <output>{value}</output>
    </>
  );
}
it.each([
  ["abc1234", "ABC-1234", "ABC1234"],
  ["abc-1234", "ABC-1234", "ABC1234"],
  ["abc1d23", "ABC1D23", "ABC1D23"],
])("normalizes pasted plate %s", (input, formatted, raw) => {
  render(<Field />);
  fireEvent.change(screen.getByLabelText(/Placa/), { target: { value: input } });
  expect(screen.getByLabelText(/Placa/)).toHaveValue(formatted);
  expect(screen.getByRole("status")).toHaveTextContent(raw);
});
it("supports the old-to-Mercosul transition and clearing", () => {
  render(<Field />);
  const input = screen.getByLabelText(/Placa/);
  fireEvent.change(input, { target: { value: "abc1" } });
  expect(input).toHaveValue("ABC-1");
  fireEvent.change(input, { target: { value: "ABC-1D" } });
  expect(input).toHaveValue("ABC1D");
  fireEvent.change(input, { target: { value: "" } });
  expect(input).toHaveValue("");
});
it("rejects extra characters and preserves field props", () => {
  render(<Field />);
  const input = screen.getByLabelText(/Placa/);
  fireEvent.change(input, { target: { value: "ABC1234" } });
  fireEvent.change(input, { target: { value: "ABC12345" } });
  expect(input).toHaveValue("ABC-1234");
  expect(input).toBeRequired();
});
it.each(["ABC1234", "ABC1D23"])(
  "allows typing, backspace and replacement for %s",
  async (plate) => {
    const user = userEvent.setup();
    render(<Field />);
    const input = screen.getByLabelText(/Placa/);
    await user.type(input, plate.toLowerCase());
    expect(screen.getByRole("status")).toHaveTextContent(plate);
    await user.keyboard("{Backspace}");
    expect(screen.getByRole("status")).toHaveTextContent(plate.slice(0, -1));
    await user.keyboard(plate.at(-1)!);
    expect(screen.getByRole("status")).toHaveTextContent(plate);
  },
);
it.each([
  ["ABC1234", true],
  ["ABC1D23", true],
  ["AB12345", false],
  ["ABC12D3", false],
  ["ABC1", false],
])("validates normalized plate %s", (plate, valid) => {
  expect(isValidPlate(plate as string)).toBe(valid);
});
