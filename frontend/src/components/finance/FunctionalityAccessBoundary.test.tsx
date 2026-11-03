import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, expect, it, vi } from "vitest";
import { FunctionalityAccessBoundary } from "./FunctionalityAccessBoundary";

const pathname = vi.hoisted(() => ({ value: "/profiles" }));
vi.mock("next/navigation", () => ({ usePathname: () => pathname.value }));
vi.mock("server-only", () => ({}));
vi.mock("@/features/auth/actions", () => ({ logoutAction: vi.fn() }));
afterEach(cleanup);

it("protects direct charges with its specific permission", () => {
  pathname.value = "/direct-charges/new";
  const { rerender } = render(<FunctionalityAccessBoundary session={{ canLogout: false, functionalityCodes: ["CHARGES_CREATE"] }}>
    <div>Formulário de Cobrança Direta</div>
  </FunctionalityAccessBoundary>);
  expect(screen.queryByText("Formulário de Cobrança Direta")).not.toBeInTheDocument();
  rerender(<FunctionalityAccessBoundary session={{ canLogout: false, functionalityCodes: ["DIRECT_CHARGES_CREATE"] }}>
    <div>Formulário de Cobrança Direta</div>
  </FunctionalityAccessBoundary>);
  expect(screen.getByText("Formulário de Cobrança Direta")).toBeInTheDocument();
});

it("does not render protected route content without its functionality", () => {
  pathname.value = "/profiles";
  render(<FunctionalityAccessBoundary session={{ canLogout: true, functionalityCodes: ["HOME"] }}>
    <div>Dados privados do perfil</div>
  </FunctionalityAccessBoundary>);
  expect(screen.queryByText("Dados privados do perfil")).not.toBeInTheDocument();
  expect(screen.getByText("Você não tem acesso a esta funcionalidade.")).toBeInTheDocument();
});

it("shows Home without dashboard data when the user has no functionality grants", () => {
  pathname.value = "/home";
  render(<FunctionalityAccessBoundary session={{ canLogout: true, functionalityCodes: [] }}>
    <div>Dados protegidos do painel</div>
  </FunctionalityAccessBoundary>);
  expect(screen.queryByText("Dados protegidos do painel")).not.toBeInTheDocument();
  expect(screen.getByRole("heading", { name: "Bem-vindo ao Yalla Finance" })).toBeInTheDocument();
  expect(screen.getByText("A página inicial está disponível para todos. Os indicadores do painel exigem permissão de consulta a débitos.")).toBeInTheDocument();
  expect(screen.getByRole("button", { name: "Sair da sessão" })).toBeInTheDocument();
});

it("shows Home without dashboard data when the user has other grants but not DEBTS", () => {
  pathname.value = "/home";
  render(<FunctionalityAccessBoundary session={{ canLogout: true, functionalityCodes: ["PROFILES"] }}>
    <div>Indicadores do painel</div>
  </FunctionalityAccessBoundary>);
  expect(screen.queryByText("Indicadores do painel")).not.toBeInTheDocument();
  expect(screen.getByRole("heading", { name: "Bem-vindo ao Yalla Finance" })).toBeInTheDocument();
});

it("does not treat a legacy HOME grant as permission to read dashboard data", () => {
  pathname.value = "/home";
  render(<FunctionalityAccessBoundary session={{ canLogout: true, functionalityCodes: ["HOME"] }}>
    <div>Indicadores do painel</div>
  </FunctionalityAccessBoundary>);
  expect(screen.queryByText("Indicadores do painel")).not.toBeInTheDocument();
  expect(screen.getByRole("heading", { name: "Bem-vindo ao Yalla Finance" })).toBeInTheDocument();
});

it("renders Home indicators when debt access is granted", () => {
  pathname.value = "/home";
  render(<FunctionalityAccessBoundary session={{ canLogout: true, functionalityCodes: ["DEBTS"] }}>
    <div>Indicadores do painel</div>
  </FunctionalityAccessBoundary>);
  expect(screen.getByText("Indicadores do painel")).toBeInTheDocument();
});

it("renders protected route content when its functionality is granted", () => {
  pathname.value = "/profiles";
  render(<FunctionalityAccessBoundary session={{ canLogout: true, functionalityCodes: ["PROFILES"] }}>
    <div>Dados do perfil permitidos</div>
  </FunctionalityAccessBoundary>);
  expect(screen.getByText("Dados do perfil permitidos")).toBeInTheDocument();
});
