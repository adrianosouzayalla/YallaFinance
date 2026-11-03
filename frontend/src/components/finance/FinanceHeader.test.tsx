import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import { FinanceHeader } from "./FinanceHeader";

vi.mock("next/navigation", () => ({ usePathname: () => "/debts/123" }));

describe("FinanceHeader", () => {
  it("renders the brand and route breadcrumb without a profile menu", () => {
    render(<FinanceHeader onMenuClick={vi.fn()} />);

    expect(screen.getByAltText("Yalla Finance")).toBeInTheDocument();
    expect(screen.getByText("Início")).toBeInTheDocument();
    expect(screen.getByRole("navigation", { name: "Breadcrumb" })).toHaveTextContent("Débitos");
    expect(screen.getByRole("button", { name: "Abrir menu" })).toBeInTheDocument();
    expect(screen.queryByText("Usuário logado (SSO Yalla)")).not.toBeInTheDocument();
  });
});
