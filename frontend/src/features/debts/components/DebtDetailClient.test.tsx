import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, expect, it, vi } from "vitest";
import { DebtDetailClient } from "./DebtDetailClient";

const mocks = vi.hoisted(() => ({
  debt: {
    id: "debt-a",
    amount: 20,
    occurrenceDate: "2026-10-01",
    tenantName: "Cliente",
    contract: "CT",
    product: "TRUST",
    plate: "ABC",
    fleet: "Frota",
    category: "Multa",
    status: "InCollection",
    description: "Descrição",
    source: "Yalla Digital",
    providerExternalId: null as string | null,
    chargeId: null as string | null,
  },
}));
vi.mock("@/features/debts/hooks/useDebt", () => ({
  useDebt: () => ({ data: mocks.debt, isError: false }),
}));
vi.mock("@/providers/ToastProvider", () => ({ useToast: () => ({ showToast: vi.fn() }) }));
afterEach(() => {
  cleanup();
  mocks.debt.providerExternalId = null;
  mocks.debt.chargeId = null;
});

it("shows the confirmed provider reference and links to the grouped charge", () => {
  mocks.debt.providerExternalId = "provider-posting-123";
  mocks.debt.chargeId = "charge-a";
  render(<DebtDetailClient debtId="debt-a" />);
  expect(screen.getByText("Integra\u00e7\u00e3o financeira")).toBeInTheDocument();
  expect(screen.getByText("provider-posting-123")).toBeInTheDocument();
  expect(screen.getByRole("link", { name: "Ver cobrança vinculada" })).toHaveAttribute(
    "href",
    "/charges/success?chargeId=charge-a",
  );
});

it("shows an honest pending message without inventing a charge link", () => {
  render(<DebtDetailClient debtId="debt-a" />);
  expect(screen.getByText("Sem refer\u00eancia externa confirmada")).toBeInTheDocument();
  expect(screen.queryByRole("link", { name: "Ver cobrança vinculada" })).not.toBeInTheDocument();
  expect(screen.queryByText("A parametrizar")).not.toBeInTheDocument();
});
