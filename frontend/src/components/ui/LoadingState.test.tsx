import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, expect, it } from "vitest";
import { LoadingState } from "./LoadingState";

afterEach(cleanup);

it("releases the page after closing and after another blocking operation", () => {
  const page = (open: boolean) => <>
    <button>Consultar</button>
    <LoadingState mode="blocking" open={open} message="Consultando..." />
  </>;
  const view = render(page(true));
  expect(screen.getByRole("dialog")).toBeInTheDocument();
  expect(screen.queryByRole("button", { name: "Consultar" })).not.toBeInTheDocument();
  view.rerender(page(false));
  expect(screen.getByRole("button", { name: "Consultar" })).toBeVisible();
  expect(document.body.style.overflow).not.toBe("hidden");
  view.rerender(page(true));
  fireEvent.keyDown(screen.getByRole("dialog"), { key: "Escape" });
  expect(screen.getByRole("dialog")).toBeInTheDocument();
  view.rerender(page(false));
  expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  expect(screen.getByRole("button", { name: "Consultar" })).toBeVisible();
  expect(document.body.style.overflow).not.toBe("hidden");
});

it.each(["blocking", "page"] as const)(
  "shows the walking camel instead of a rotating spinner in %s mode",
  (mode) => {
    render(<LoadingState mode={mode} message="Consultando..." />);

    expect(document.querySelector("svg.yallaCamel")).toBeInTheDocument();
    expect(screen.queryByRole("progressbar")).not.toBeInTheDocument();
  },
);
