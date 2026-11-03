import { cleanup, fireEvent, screen, waitFor, act } from "@testing-library/react";
import { useState } from "react";
import { afterEach, expect, it, vi } from "vitest";
import { renderWithSession } from "@/test/renderWithSession";
import { useCustomerSuggestions } from "./useCustomerSuggestions";
import type { Customer } from "../types/customer.types";

const mocks = vi.hoisted(() => ({ suggestCustomers: vi.fn() }));
vi.mock("../api/customers.client", () => ({ customersApi: mocks }));
afterEach(() => { cleanup(); vi.clearAllMocks(); });

function Search() {
  const [input, setInput] = useState("");
  const query = useCustomerSuggestions(input);
  return <><input aria-label="Search" value={input} onChange={(event) => setInput(event.target.value)} /><output>{query.items.map((item) => item.name).join(",")}</output></>;
}

it("debounces rapid input and cancels the previous request before the new debounce finishes", async () => {
  let finishOld!: (items: Customer[]) => void;
  let oldSignal!: AbortSignal;
  mocks.suggestCustomers.mockImplementation((term: string, options: { signal: AbortSignal }) => {
    if (term === "Sil") {
      oldSignal = options.signal;
      return new Promise<Customer[]>((resolve) => { finishOld = resolve; });
    }
    return Promise.resolve([{ name: "Cliente atual" }]);
  });
  renderWithSession(<Search />);
  const input = screen.getByLabelText("Search");
  fireEvent.change(input, { target: { value: "Sil" } });
  await waitFor(() => expect(mocks.suggestCustomers).toHaveBeenCalledTimes(1));
  fireEvent.change(input, { target: { value: "Sila" } });
  expect(oldSignal.aborted).toBe(true);
  fireEvent.change(input, { target: { value: "Silva" } });
  expect(mocks.suggestCustomers).toHaveBeenCalledTimes(1);
  expect(await screen.findByText("Cliente atual")).toBeVisible();
  expect(mocks.suggestCustomers).toHaveBeenCalledTimes(2);
  expect(mocks.suggestCustomers.mock.calls[1][0]).toBe("Silva");
  await act(async () => finishOld([{ name: "Resposta antiga" } as Customer]));
  expect(screen.queryByText("Resposta antiga")).not.toBeInTheDocument();
});

it("clearing the input cancels its request and hides its suggestions", async () => {
  let signal!: AbortSignal;
  mocks.suggestCustomers.mockImplementation((_term: string, options: { signal: AbortSignal }) => {
    signal = options.signal;
    return new Promise(() => {});
  });
  renderWithSession(<Search />);
  fireEvent.change(screen.getByLabelText("Search"), { target: { value: "João" } });
  await waitFor(() => expect(mocks.suggestCustomers).toHaveBeenCalledTimes(1));
  fireEvent.change(screen.getByLabelText("Search"), { target: { value: "" } });
  expect(signal.aborted).toBe(true);
  expect(screen.getByRole("status")).toHaveTextContent("");
});
