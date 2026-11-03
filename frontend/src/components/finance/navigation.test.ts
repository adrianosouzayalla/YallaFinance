import { describe, expect, it } from "vitest";
import { financeNavigation, getActiveNavigationItem, getBreadcrumbItems, getRequiredFunctionality } from "./navigation";

describe("finance navigation", () => {
  it("requires the system settings permission for settings and its child routes", () => {
    for (const route of ["/settings", "/settings/connections"])
      expect(getRequiredFunctionality(route)).toBe("SYSTEM_SETTINGS");
    expect(getActiveNavigationItem("/settings/connections").href).toBe("/settings");
    expect(financeNavigation.find((item) => item.href === "/settings")?.functionalityCode).toBe("SYSTEM_SETTINGS");
  });
  it("keeps direct charges in their own route and permission", () => {
    expect(getRequiredFunctionality("/direct-charges/new")).toBe("DIRECT_CHARGES_CREATE");
    expect(getActiveNavigationItem("/direct-charges/new").href).toBe("/direct-charges/new");
    expect(getActiveNavigationItem("/direct-charges/new").icon).toBe("direct-charges");
    expect(getBreadcrumbItems("/direct-charges/new")).toEqual([{ label: "Início", href: "/home" }, { label: "Cobrança Direta" }]);
    for (const route of ["/charges/new", "/charges/review", "/charges/confirmation", "/charges/success"])
      expect(getRequiredFunctionality(route)).toBe("CHARGES_CREATE");
  });
  it("maps child routes to their parent navigation item", () => {
    expect(getActiveNavigationItem("/debts/new").href).toBe("/debts/new");
    expect(getActiveNavigationItem("/jobs").href).toBe("/jobs");
    expect(getActiveNavigationItem("/jobs/123").href).toBe("/jobs");
    expect(getActiveNavigationItem("/debts/123").href).toBe("/debts");
    expect(getActiveNavigationItem("/charges/review").href).toBe("/charges/new");
  });

  it("builds breadcrumbs with only routable ancestors", () => {
    expect(getBreadcrumbItems("/home")).toEqual([{ label: "Início" }, { label: "Visão geral" }]);
    expect(getBreadcrumbItems("/debts")).toEqual([
      { label: "Início", href: "/home" },
      { label: "Débitos" },
    ]);
  });

  it("does not model Home as a permission-protected route", () => {
    expect(getRequiredFunctionality("/home")).toBeUndefined();
    expect(getRequiredFunctionality("/")).toBeUndefined();
    expect(financeNavigation.find((item) => item.href === "/home")).not.toHaveProperty("functionalityCode");
  });

  it("maps protected routes to their required functionality", () => {
    expect(getRequiredFunctionality("/debts/123")).toBe("DEBTS");
    expect(getRequiredFunctionality("/debts/new")).toBe("DEBTS_CREATE");
    expect(getRequiredFunctionality("/charges/review")).toBe("CHARGES_CREATE");
    expect(getRequiredFunctionality("/profiles/123")).toBe("PROFILES");
    expect(getRequiredFunctionality("/change-password")).toBeUndefined();
  });
});
