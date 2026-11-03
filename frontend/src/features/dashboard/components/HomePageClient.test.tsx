import { cleanup, fireEvent, screen, waitFor } from "@testing-library/react";
import { renderWithSession as render } from "@/test/renderWithSession";
import { afterEach, beforeEach, expect, it, vi } from "vitest";
import { HomePageClient } from "@/features/dashboard/components/HomePageClient";

const summary = {
  awaitingCharge: { count: 1, amount: 20 },
  activeCharges: { count: 2, amount: 30 },
  overdue: { count: 1, amount: 10 },
  staleCharges: { count: 0, amount: 0 },
};
const repository = { getDashboardSummary: vi.fn(async () => summary) };
const showToast = vi.fn();
vi.mock("@/features/dashboard/api/dashboard.client", () => ({
  dashboardApi: { getDashboardSummary: () => repository.getDashboardSummary() },
}));
vi.mock("@/providers/ToastProvider", () => ({ useToast: () => ({ showToast }) }));

afterEach(cleanup);

beforeEach(() => {
  repository.getDashboardSummary.mockReset().mockResolvedValue(summary);
  showToast.mockReset();
});

it("links all indicators using technical statuses including overdue debts", async () => {
  render(<HomePageClient />);
  await waitFor(() => expect(repository.getDashboardSummary).toHaveBeenCalled());
  const links = (await screen.findAllByRole("link")).map((link) => link.getAttribute("href"));
  expect(links).toContain("/debts?status=Open&sort=overdue");
  expect(links).toContain("/debts?status=InCollection");
  expect(links).toContain("/debts?status=Open");
  expect(links).toContain("/debts?status=InCollection&sort=stale");
});

it("omits the manual refresh button and allows retry after loading failure", async () => {
  repository.getDashboardSummary.mockRejectedValueOnce(new Error("offline")).mockResolvedValueOnce(summary);
  render(<HomePageClient />);
  expect(await screen.findByRole("alert")).toHaveTextContent("Não foi possível carregar os indicadores.");
  expect(screen.queryByRole("button", { name: "Atualizar" })).not.toBeInTheDocument();
  fireEvent.click(screen.getByRole("button", { name: "Tentar novamente" }));
  await waitFor(() => expect(repository.getDashboardSummary).toHaveBeenCalledTimes(2));
  expect(await screen.findByText("Aguardando cobrança")).toBeInTheDocument();
  expect(showToast).not.toHaveBeenCalled();
});
