import { describe, expect, it } from "vitest";
import { financeTokens } from "./designTokens";
import { theme } from "./theme";

describe("financeTokens", () => {
  it("uses a compact typography and spacing scale independent of viewport width", () => {
    expect(financeTokens.uiScale).toBe(0.8);
    expect(financeTokens.shell.viewportHeight).toBe("125dvh");
    expect(theme.breakpoints.values).toMatchObject({ sm: 480, md: 720, lg: 960, xl: 1229 });
    expect(financeTokens.typography.body).toBe("0.8125rem");
    expect(financeTokens.typography.pageTitle).toBe("1.25rem");
    expect(financeTokens.typography.summaryMetric).toBe("1.1875rem");
    expect(financeTokens.shell.headerHeight).toBe("3rem");
    expect(financeTokens.density.summaryCardPadding).toBe("0.45rem");
    expect(financeTokens.density.operationalCardPadding).toBe("0.45rem");
    expect(financeTokens.density.operationalCardMinHeight).toBe("9rem");
    expect(theme.spacing(1)).toBe("2.88px");
  });

  it("locks the prototype shell and palette", () => {
    expect(financeTokens.colors.header).toBe("#203864");
    expect(financeTokens.colors.action).toBe("#2D73B3");
    expect(financeTokens.colors.sidebar).toBe("#F3F4F8");
    expect(financeTokens.colors.stepBackground).toBe("#F8F9FD");
    expect(financeTokens.shell.headerHeight).toBe("3rem");
    expect(financeTokens.shell.sidebarWidth).toBe("12.5rem");
    expect(financeTokens.shell.contentPadding).toBe("1.5rem");
    expect(financeTokens.typography.pageTitle).toBe("1.25rem");
    expect(financeTokens.typography.body).toBe("0.8125rem");
    expect(financeTokens.typography.checkoutAmount).toBe("2.5rem");
    expect(financeTokens.density.navigationItemHeight).toBe("2.5625rem");
    expect(financeTokens.density.summaryCardMinHeight).toBe("4.625rem");
    expect(financeTokens.auth.formMaxWidth).toBe("22.5rem");
    expect(financeTokens.auth.logoWidth).toBe("11.25rem");
  });
});
