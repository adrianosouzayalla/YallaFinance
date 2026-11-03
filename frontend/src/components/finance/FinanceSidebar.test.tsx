import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { FinanceSidebar } from "./FinanceSidebar";
vi.mock("@/features/auth/actions", () => ({ logoutAction: vi.fn() }));

vi.mock("next/navigation", () => ({
  usePathname: () => "/home",
  useRouter: () => ({ replace: vi.fn(), refresh: vi.fn() }),
}));
afterEach(cleanup);

describe("FinanceSidebar", () => {
  it("shows direct charges only with their own permission and financial document icon", () => {
    const { rerender } = render(<FinanceSidebar session={{ canLogout: false, functionalityCodes: ["CHARGES_CREATE"] }} />);
    expect(screen.queryByRole("link", { name: "Cobrança Direta" })).not.toBeInTheDocument();
    rerender(<FinanceSidebar session={{ canLogout: false, functionalityCodes: ["DIRECT_CHARGES_CREATE"] }} />);
    expect(screen.getByRole("link", { name: "Cobrança Direta" })).toHaveAttribute("href", "/direct-charges/new");
    expect(screen.getByTestId("RequestQuoteOutlinedIcon")).toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Cobrança avulsa" })).not.toBeInTheDocument();
  });
  it("keeps Home visible to users without functionality grants", () => {
    render(<FinanceSidebar session={{ canLogout: false, functionalityCodes: [] }} />);

    expect(screen.getByRole("link", { name: "Início" })).toHaveAttribute("href", "/home");
    expect(screen.queryByRole("link", { name: "Débitos" })).not.toBeInTheDocument();
  });

  it("contains only the approved destinations", () => {
    render(<FinanceSidebar session={{ canLogout: false, functionalityCodes: ["DEBTS", "DEBTS_CREATE", "CHARGES_CREATE"] }} />);

    expect(screen.getByRole("link", { name: "Início" })).toHaveAttribute("href", "/home");
    expect(screen.getByRole("link", { name: "Débitos" })).toHaveAttribute(
      "href",
      "/debts",
    );
    expect(screen.getByRole("link", { name: "Cobrança avulsa" })).toHaveAttribute("href", "/charges/new");
    expect(screen.getByRole("link", { name: "Débito avulso" })).toHaveAttribute("href", "/debts/new");
    expect(screen.queryByText(/Histórico/)).not.toBeInTheDocument();
    expect(screen.queryByText(/Configurações/)).not.toBeInTheDocument();
  });
});
