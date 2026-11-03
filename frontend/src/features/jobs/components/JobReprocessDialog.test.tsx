import { cleanup, fireEvent, render, screen, within } from "@testing-library/react";
import { afterEach, expect, it, vi } from "vitest";
import { JobReprocessDialog } from "./JobReprocessDialog";
afterEach(cleanup);
it("can cancel without reprocessing and confirms only an explicit click", () => {
  const onClose = vi.fn(),
    onConfirm = vi.fn();
  render(<JobReprocessDialog open pending={false} onClose={onClose} onConfirm={onConfirm} />);
  const dialog = within(screen.getByRole("dialog", { name: "Reprocessar Job" }));
  expect(dialog.getByText(/O histórico das execuções anteriores será mantido/)).toBeInTheDocument();
  fireEvent.click(dialog.getByRole("button", { name: "Cancelar" }));
  expect(onClose).toHaveBeenCalledOnce();
  expect(onConfirm).not.toHaveBeenCalled();
  fireEvent.click(dialog.getByRole("button", { name: "Reprocessar" }));
  expect(onConfirm).toHaveBeenCalledOnce();
});
it("blocks extra confirmation and closing while sending", () => {
  const onClose = vi.fn(),
    onConfirm = vi.fn();
  render(<JobReprocessDialog open pending onClose={onClose} onConfirm={onConfirm} />);
  const dialog = within(screen.getByRole("dialog"));
  expect(dialog.getByRole("button", { name: "Reprocessar" })).toBeDisabled();
  expect(dialog.getByRole("button", { name: "Cancelar" })).toBeDisabled();
  fireEvent.keyDown(screen.getByRole("dialog"), { key: "Escape", code: "Escape" });
  fireEvent.click(dialog.getByRole("button", { name: "Reprocessar" }));
  expect(onConfirm).not.toHaveBeenCalled();
  expect(onClose).not.toHaveBeenCalled();
});
