import { useState } from "react";
import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, describe, expect, it, vi } from "vitest";
import { TaxIdField } from "./TaxIdField";

afterEach(cleanup);

function ControlledField() {
  const [value, setValue] = useState("");
  return (
    <>
      <TaxIdField value={value} onChange={setValue} />
      <output aria-label="Documento normalizado">{value}</output>
    </>
  );
}

describe("TaxIdField", () => {
  it.each([
    ["12345678901", "123.456.789-01", "12345678901"],
    ["123.456.789-01", "123.456.789-01", "12345678901"],
    ["01234567890", "012.345.678-90", "01234567890"],
    ["12345678000190", "12.345.678/0001-90", "12345678000190"],
    ["12.345.678/0001-90", "12.345.678/0001-90", "12345678000190"],
  ])("formats pasted document %s and retains only digits", async (pasted, formatted, digits) => {
    const user = userEvent.setup();
    render(<ControlledField />);
    const input = screen.getByRole("textbox", { name: "CPF/CNPJ" });
    await user.click(input);
    await user.paste(pasted);
    expect(input).toHaveValue(formatted);
    expect(screen.getByLabelText("Documento normalizado")).toHaveTextContent(digits);
    expect(input).toHaveAttribute("inputmode", "numeric");
  });

  it("changes between CPF and CNPJ masks without losing digits and allows clearing", () => {
    render(<ControlledField />);
    const input = screen.getByRole("textbox", { name: "CPF/CNPJ" });
    fireEvent.change(input, { target: { value: "12345678901" } });
    expect(input).toHaveValue("123.456.789-01");
    fireEvent.change(input, { target: { value: "123456789012" } });
    expect(input).toHaveValue("12.345.678/9012");
    expect(screen.getByLabelText("Documento normalizado")).toHaveTextContent("123456789012");
    fireEvent.change(input, { target: { value: "12345678901" } });
    expect(input).toHaveValue("123.456.789-01");
    fireEvent.change(input, { target: { value: "" } });
    expect(input).toHaveValue("");
    expect(screen.getByLabelText("Documento normalizado")).toBeEmptyDOMElement();
  });

  it("preserves controlled values and does not emit changes merely from rendering", () => {
    const change = vi.fn();
    const view = render(
      <TaxIdField
        value="123.456.789-01"
        onChange={change}
        required
        error
        helperText="Confira o documento."
      />,
    );
    const input = screen.getByRole("textbox", { name: /CPF\/CNPJ/ });
    expect(input).toHaveValue("123.456.789-01");
    expect(input).toBeRequired();
    expect(input).toHaveAttribute("aria-invalid", "true");
    expect(screen.getByText("Confira o documento.")).toBeInTheDocument();
    view.rerender(<TaxIdField value="12345678000190" onChange={change} />);
    expect(input).toHaveValue("12.345.678/0001-90");
    expect(change).not.toHaveBeenCalled();
    view.rerender(<TaxIdField value={null} onChange={change} />);
    expect(input).toHaveValue("");
    expect(change).not.toHaveBeenCalled();
  });

  it("keeps the document intact when typing and deleting through the mask transition", async () => {
    const user = userEvent.setup();
    render(<ControlledField />);
    const input = screen.getByRole("textbox", { name: "CPF/CNPJ" });
    await user.type(input, "12345678000190");
    expect(input).toHaveValue("12.345.678/0001-90");
    expect(screen.getByLabelText("Documento normalizado")).toHaveTextContent("12345678000190");
    await user.keyboard("{Backspace}{Backspace}{Backspace}");
    expect(input).toHaveValue("123.456.780-00");
    expect(screen.getByLabelText("Documento normalizado")).toHaveTextContent("12345678000");
  }, 15000);

  it("supports a fixed CPF mode and rejects excess digits without truncating the document", () => {
    const change = vi.fn();
    render(<TaxIdField documentType="cpf" value="12345678901" onChange={change} />);
    const input = screen.getByRole("textbox", { name: "CPF" });
    fireEvent.change(input, { target: { value: "123456789012" } });
    expect(input).toHaveValue("123.456.789-01");
    expect(change).not.toHaveBeenCalled();
  });
});
