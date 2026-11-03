import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { useState } from "react";
import { Controller, useForm, useWatch } from "react-hook-form";
import { afterEach, expect, it, vi } from "vitest";
import { CurrencyField } from "./CurrencyField";
import { DateField } from "./DateField";

afterEach(cleanup);

function MoneyForm() {
  const form = useForm<{ amount: number | null }>({ defaultValues: { amount: null } });
  const amount = useWatch({ control: form.control, name: "amount" });
  return (
    <>
      <Controller
        name="amount"
        control={form.control}
        render={({ field: { ref, ...field } }) => (
          <CurrencyField {...field} inputRef={ref} label="Valor" />
        )}
      />
      <output aria-label="Valor normalizado">{amount === null ? "vazio" : String(amount)}</output>
      <button onClick={() => form.reset({ amount: 20.5 })}>Restaurar</button>
    </>
  );
}

it("masks currency typing and keeps cents numeric with React Hook Form", async () => {
  const user = userEvent.setup();
  render(<MoneyForm />);
  const input = screen.getByLabelText("Valor");
  await user.type(input, "1");
  expect(input).toHaveValue("0,01");
  await user.type(input, "2");
  expect(input).toHaveValue("0,12");
  await user.type(input, "3");
  expect(input).toHaveValue("1,23");
  await user.keyboard("{Backspace}");
  expect(input).toHaveValue("0,12");
  await user.type(input, "3");
  await user.type(input, "456");
  expect(input).toHaveValue("1.234,56");
  expect(screen.getByLabelText("Valor normalizado")).toHaveTextContent("1234.56");
  await user.clear(input);
  expect(input).toHaveValue("");
  expect(screen.getByLabelText("Valor normalizado")).toHaveTextContent("vazio");
  fireEvent.click(screen.getByRole("button", { name: "Restaurar" }));
  expect(input).toHaveValue("20,50");
});

it("replaces the initial zero and shifts digit-only pasted values into cents", async () => {
  const user = userEvent.setup();
  function InitialZero() {
    const [amount, setAmount] = useState<number | null>(0);
    return <CurrencyField label="Valor" value={amount} onChange={setAmount} />;
  }
  render(<InitialZero />);
  const input = screen.getByLabelText("Valor");
  await user.type(input, "123");
  expect(input).toHaveValue("1,23");
  await user.clear(input);
  await user.paste("123456");
  expect(input).toHaveValue("1.234,56");
  await user.clear(input);
  await user.type(input, "0");
  expect(input).toHaveValue("0,00");
});

it("supports pasted currency, editing cents, negative values and disabled fields", async () => {
  const user = userEvent.setup();
  render(<MoneyForm />);
  const input = screen.getByLabelText("Valor");
  await user.click(input);
  await user.paste("1.234,56");
  expect(screen.getByLabelText("Valor normalizado")).toHaveTextContent("1234.56");
  await user.keyboard("{End}{Backspace}7");
  expect(screen.getByLabelText("Valor normalizado")).toHaveTextContent("1234.57");
  await user.clear(input);
  await user.type(input, "-1050");
  expect(screen.getByLabelText("Valor normalizado")).toHaveTextContent("-10.5");
});

function DateForm({ mode = "native" }: { mode?: "native" | "masked" }) {
  const [date, setDate] = useState<string | null>(null);
  return (
    <>
      <DateField
        label="Data"
        mode={mode}
        value={date}
        onChange={setDate}
        min="2020-01-01"
        max="2030-12-31"
      />
      <output aria-label="Data normalizada">{date ?? "vazia"}</output>
      <button onClick={() => setDate("2026-10-03")}>Restaurar</button>
    </>
  );
}

it("preserves native calendar, ISO values and date boundaries", () => {
  render(<DateForm />);
  const input = screen.getByLabelText("Data");
  expect(input).toHaveAttribute("type", "date");
  expect(input).toHaveAttribute("min", "2020-01-01");
  expect(input).toHaveAttribute("max", "2030-12-31");
  fireEvent.change(input, { target: { value: "2026-10-03" } });
  expect(screen.getByLabelText("Data normalizada")).toHaveTextContent("2026-10-03");
  fireEvent.change(input, { target: { value: "" } });
  expect(screen.getByLabelText("Data normalizada")).toHaveTextContent("vazia");
});

it("keeps the outline open around an empty native date label before focus and after blur", () => {
  const view = render(<DateForm />);
  const input = screen.getByLabelText("Data");
  const legend = view.container.querySelector("legend");
  expect(legend).not.toBeNull();
  expect(legend).toHaveStyle({ maxWidth: "100%" });
  fireEvent.focus(input);
  expect(legend).toHaveStyle({ maxWidth: "100%" });
  fireEvent.blur(input);
  expect(legend).toHaveStyle({ maxWidth: "100%" });
});

it("preserves boundaries supplied through native input slots and allows explicit overrides", () => {
  const props = {
    label: "Data",
    value: "2026-10-03",
    onChange: () => {},
    slotProps: { htmlInput: () => ({ min: "2026-01-01", max: "2027-12-31" }) },
  };
  const view = render(<DateField {...props} />);
  expect(screen.getByLabelText("Data")).toHaveAttribute("min", "2026-01-01");
  expect(screen.getByLabelText("Data")).toHaveAttribute("max", "2027-12-31");
  view.rerender(<DateField {...props} min="2025-01-01" />);
  expect(screen.getByLabelText("Data")).toHaveAttribute("min", "2025-01-01");
  expect(screen.getByLabelText("Data")).toHaveAttribute("max", "2027-12-31");
});

it("masks dates, retains partial typing and never emits an impossible ISO date", async () => {
  const user = userEvent.setup();
  render(<DateForm mode="masked" />);
  const input = screen.getByLabelText("Data");
  await user.type(input, "0310");
  expect(screen.getByLabelText("Data normalizada")).toHaveTextContent("vazia");
  await user.type(input, "2026");
  expect(input).toHaveValue("03/10/2026");
  expect(screen.getByLabelText("Data normalizada")).toHaveTextContent("2026-10-03");
  await user.clear(input);
  await user.type(input, "31022026");
  expect(input).toHaveValue("31/02/2026");
  expect(screen.getByLabelText("Data normalizada")).toHaveTextContent("vazia");
  expect(input).toHaveAttribute("aria-invalid", "true");
  fireEvent.click(screen.getByRole("button", { name: "Restaurar" }));
  expect(input).toHaveValue("03/10/2026");
});

it("preserves generic field errors, required state, disabled state and input refs", () => {
  const change = vi.fn();
  const inputRef = { current: null as HTMLInputElement | null };
  render(
    <CurrencyField
      label="Valor"
      name="amount"
      value={10}
      onChange={change}
      inputRef={inputRef}
      disabled
      required
      error
      helperText="Erro da tela"
    />,
  );
  expect(screen.getByLabelText(/Valor/)).toBeDisabled();
  expect(screen.getByLabelText(/Valor/)).toBeRequired();
  expect(screen.getByText("Erro da tela")).toBeVisible();
  expect(inputRef.current).toBe(screen.getByLabelText(/Valor/));
  expect(change).not.toHaveBeenCalled();
});
