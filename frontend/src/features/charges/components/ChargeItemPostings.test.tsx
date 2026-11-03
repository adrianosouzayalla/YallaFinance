import { act, cleanup, render, screen, within } from "@testing-library/react";
import { ThemeProvider } from "@mui/material/styles";
import { afterEach, beforeEach, expect, it, vi } from "vitest";
import { theme } from "@/theme/theme";
import type { Charge } from "../types/charge.types";
import { ChargeItemPostings } from "./ChargeItemPostings";

const observers = new Set<TestResizeObserver>();
class TestResizeObserver implements ResizeObserver {
  readonly targets = new Set<Element>();
  constructor(readonly callback: ResizeObserverCallback) {
    observers.add(this);
  }
  observe(target: Element) {
    this.targets.add(target);
  }
  unobserve(target: Element) {
    this.targets.delete(target);
  }
  disconnect() {
    this.targets.clear();
    observers.delete(this);
  }
}

let layout: { top: number; height: number }[];
function itemIndex(element: HTMLElement) {
  if (element.tagName !== "LI") return -1;
  return Array.from(element.parentElement?.children ?? []).indexOf(element);
}

beforeEach(() => {
  layout = [
    { top: 4, height: 80 },
    { top: 88, height: 104 },
    { top: 196, height: 96 },
    { top: 296, height: 88 },
    { top: 388, height: 120 },
  ];
  vi.spyOn(HTMLElement.prototype, "offsetTop", "get").mockImplementation(function (
    this: HTMLElement,
  ) {
    return layout[itemIndex(this)]?.top ?? 0;
  });
  vi.spyOn(HTMLElement.prototype, "offsetHeight", "get").mockImplementation(function (
    this: HTMLElement,
  ) {
    return layout[itemIndex(this)]?.height ?? 0;
  });
  vi.stubGlobal("ResizeObserver", TestResizeObserver);
});
afterEach(() => {
  cleanup();
  observers.clear();
  vi.restoreAllMocks();
  vi.unstubAllGlobals();
});

function postings(count: number) {
  const charge: Charge = {
    reference: "10-123",
    id: "charge",
    tenantId: "tenant",
    customerId: "customer",
    source: "selected",
    idempotencyKey: "key",
    categoryId: "category",
    paymentMethodId: "pix",
    debtIds: [],
    amount: count * 10,
    payment: { method: "Pix", dueDate: "2026-10-17", customerNote: "" },
    providerExternalId: "",
    pixId: "",
    pixPayload: "",
    createdAt: "2026-10-08T12:00:00Z",
    items: Array.from({ length: count }, (_, index) => ({
      id: `item-${index}`,
      debtId: `debt-${index}`,
      categoryId: "category",
      ordinal: index + 1,
      amount: 10,
      debt: {
        contract: `Contrato ${index + 1}`,
        plate: "ABC1D23",
        fleet: "Matriz",
        product: "Produto",
        occurrenceDate: "2026-10-07",
        dueDate: "2026-10-17",
        description: `Descrição do item ${index + 1}`,
      },
    })),
  };
  return (
    <ThemeProvider theme={theme}>
      <ChargeItemPostings charge={charge} categories={[{ id: "category", name: "Multa" }]} />
    </ThemeProvider>
  );
}

function notifyResize(target: Element) {
  for (const observer of observers) {
    if (observer.targets.has(target)) {
      observer.callback(
        [
          {
            target,
            contentRect: target.getBoundingClientRect(),
            borderBoxSize: [],
            contentBoxSize: [],
            devicePixelContentBoxSize: [],
          },
        ],
        observer,
      );
    }
  }
}

it("limits the list to four measured postings while keeping all ten accessible", () => {
  render(postings(10));
  const list = screen.getByRole("list", { name: "Lançamentos por item" });

  expect(within(list).getAllByRole("listitem")).toHaveLength(10);
  expect(within(list).getByText("Descrição do item 10")).toBeInTheDocument();
  expect(screen.getByText("10 lançamentos")).toBeVisible();
  expect(getComputedStyle(list).maxHeight).toBe("386.88px");
  expect(getComputedStyle(list).overflowY).toBe("scroll");
  expect(list).toHaveAttribute("tabindex", "0");
  expect(list).not.toContainElement(screen.getByRole("heading", { name: "Lançamentos por item" }));
});

it("updates the height limit when one of the first four postings grows", () => {
  render(postings(10));
  const list = screen.getByRole("list", { name: "Lançamentos por item" });
  expect(getComputedStyle(list).maxHeight).toBe("386.88px");

  layout[0].height = 120;
  for (const item of layout.slice(1)) item.top += 40;
  act(() => notifyResize(within(list).getAllByRole("listitem")[0]));

  expect(getComputedStyle(list).maxHeight).toBe("426.88px");
});

it("updates the limit when the list resizes and the posting layout changes", () => {
  render(postings(10));
  const list = screen.getByRole("list", { name: "Lançamentos por item" });
  expect(getComputedStyle(list).maxHeight).toBe("386.88px");

  layout[3] = { top: 480, height: 140 };
  act(() => notifyResize(list));

  expect(getComputedStyle(list).maxHeight).toBe("622.88px");
});

it("removes the scrolling limit when there are four or fewer postings", () => {
  const view = render(postings(5));
  const list = screen.getByRole("list", { name: "Lançamentos por item" });
  expect(getComputedStyle(list).maxHeight).toBe("386.88px");

  for (const count of [4, 1, 0]) {
    view.rerender(postings(count));
    expect(within(list).queryAllByRole("listitem")).toHaveLength(count);
    expect(getComputedStyle(list).maxHeight).toBe("none");
    expect(list).not.toHaveAttribute("tabindex");
  }
});
