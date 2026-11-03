import { cleanup, screen } from "@testing-library/react";
import { afterEach, beforeEach, expect, it, vi } from "vitest";
import { renderWithSession } from "@/test/renderWithSession";
import { ChargeDataBoundary } from "./ChargeDataBoundary";
import type { ChargeDraft } from "../types/charge.types";
const mocks = vi.hoisted(() => ({ params: "", draft: null as ChargeDraft | null, getDebts: vi.fn(), listDebts: vi.fn() }));
vi.mock("next/navigation", () => ({ useSearchParams: () => new URLSearchParams(mocks.params) }));
vi.mock("../context/ChargeDraftProvider", () => ({ useChargeDraft: () => ({ draft: mocks.draft }) }));
vi.mock("@/features/debts/api/debts.client", () => ({ debtsApi: mocks }));
beforeEach(() => { vi.clearAllMocks(); mocks.params = ""; mocks.draft = null; });
afterEach(cleanup);

it("opens standalone review without querying debts even with a previous selected draft", () => {
  mocks.params = "source=standalone";
  mocks.draft = { source: "selected", debtIds: ["old-debt"] } as ChargeDraft;
  renderWithSession(<ChargeDataBoundary><span>Form ready</span></ChargeDataBoundary>);
  expect(screen.getByText("Form ready")).toBeVisible();
  expect(mocks.getDebts).not.toHaveBeenCalled();
  expect(mocks.listDebts).not.toHaveBeenCalled();
});

it("loads only URL selection before opening review", async () => {
  mocks.params = "source=selected&debtIds=debt-100001";
  mocks.getDebts.mockResolvedValue([{ id: "debt-100001" }]);
  renderWithSession(<ChargeDataBoundary><span>Form ready</span></ChargeDataBoundary>);
  expect(await screen.findByText("Form ready")).toBeVisible();
  expect(mocks.getDebts).toHaveBeenCalledWith(["debt-100001"], expect.anything());
  expect(mocks.listDebts).not.toHaveBeenCalled();
});

it("uses draft IDs on confirmation without a URL selection", async () => {
  mocks.draft = { source: "selected", debtIds: ["selected-debt"] } as ChargeDraft;
  mocks.getDebts.mockResolvedValue([{ id: "selected-debt" }]);
  renderWithSession(<ChargeDataBoundary><span>Confirmation ready</span></ChargeDataBoundary>);
  expect(await screen.findByText("Confirmation ready")).toBeVisible();
  expect(mocks.getDebts).toHaveBeenCalledWith(["selected-debt"], expect.anything());
});
