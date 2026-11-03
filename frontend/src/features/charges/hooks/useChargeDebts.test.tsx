import { cleanup, screen } from "@testing-library/react";
import { afterEach, expect, it, vi } from "vitest";
import { renderWithSession } from "@/test/renderWithSession";
import { useChargeDebts } from "./useChargeDebts";

const mocks = vi.hoisted(() => ({ getDebts: vi.fn(), listDebts: vi.fn() }));
vi.mock("@/features/debts/api/debts.client", () => ({ debtsApi: mocks }));
afterEach(() => { cleanup(); vi.clearAllMocks(); });

function Selection({ ids }: { ids: string[] }) {
  const query = useChargeDebts(ids);
  return <output>{query.data?.items.map((debt) => debt.id).join(",") ?? "loading"}</output>;
}

it("fetches only selected IDs regardless of catalog size and deduplicates them", async () => {
  mocks.getDebts.mockResolvedValue([{ id: "debt-100001" }]);
  mocks.listDebts.mockResolvedValue({ items: [], totalItems: 100001, pageSize: 1000 });
  renderWithSession(<Selection ids={["debt-100001", "debt-100001"]} />);
  expect(await screen.findByText("debt-100001")).toBeVisible();
  expect(mocks.getDebts).toHaveBeenCalledWith(["debt-100001"], expect.objectContaining({ contextId: "session-test" }));
  expect(mocks.listDebts).not.toHaveBeenCalled();
});

it("does not request debts when there is no selected debt", () => {
  mocks.getDebts.mockResolvedValue([]);
  mocks.listDebts.mockResolvedValue({ items: [], totalItems: 0, pageSize: 1000 });
  renderWithSession(<Selection ids={[]} />);
  expect(mocks.getDebts).not.toHaveBeenCalled();
  expect(mocks.listDebts).not.toHaveBeenCalled();
});
