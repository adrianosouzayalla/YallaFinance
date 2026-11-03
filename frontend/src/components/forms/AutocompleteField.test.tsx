import { cleanup, screen } from "@testing-library/react";
import { useState } from "react";
import userEvent from "@testing-library/user-event";
import { afterEach, expect, it } from "vitest";
import { renderWithSession } from "@/test/renderWithSession";
import { AutocompleteField } from "./AutocompleteField";

afterEach(cleanup);
type Option = { id: string; label: string };
const options = [{ id: "a", label: "Mesmo nome" }, { id: "b", label: "Mesmo nome" }];
function Field() {
  const [value, setValue] = useState<Option | null>(null);
  const [input, setInput] = useState("documento");
  return <>
    <AutocompleteField
      label="Registro" options={options} value={value} inputValue={input}
      getOptionKey={(option) => option.id} getOptionLabel={(option) => option.label}
      onInputChange={setInput}
      onChange={(next) => { setValue(next); setInput(next?.label ?? ""); }}
    />
    <output>{value?.id ?? "none"}</output>
  </>;
}

it("preserves remote results that do not match the label and distinguishes duplicate labels by ID", async () => {
  const user = userEvent.setup();
  renderWithSession(<Field />);
  await user.click(screen.getByRole("combobox", { name: "Registro" }));
  const matches = await screen.findAllByRole("option", { name: "Mesmo nome" });
  expect(matches).toHaveLength(2);
  await user.click(matches[1]);
  expect(screen.getByRole("status")).toHaveTextContent("b");
  expect(screen.getByRole("combobox")).toHaveValue("Mesmo nome");
  await user.click(screen.getByRole("button", { name: "Limpar" }));
  expect(screen.getByRole("status")).toHaveTextContent("none");
  expect(screen.getByRole("combobox")).toHaveValue("");
});
