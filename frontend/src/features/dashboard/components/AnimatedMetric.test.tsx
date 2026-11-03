import { act, cleanup, render } from "@testing-library/react";
import { afterEach, beforeEach, expect, it, vi } from "vitest";
import { AnimatedMetric } from "./AnimatedMetric";

let frames: Map<number, FrameRequestCallback>;
let nextFrameId: number;

beforeEach(() => {
  frames = new Map();
  nextFrameId = 0;
  vi.stubGlobal("requestAnimationFrame", (callback: FrameRequestCallback) => {
    frames.set(++nextFrameId, callback);
    return nextFrameId;
  });
  vi.stubGlobal("cancelAnimationFrame", (id: number) => frames.delete(id));
});
afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
});
function advanceFrame(time: number) {
  act(() => {
    const pending = [...frames.values()];
    frames.clear();
    pending.forEach((callback) => callback(time));
  });
}

it("animates an increase from the displayed value and skips unchanged values", () => {
  const view = render(<AnimatedMetric value={100} />);
  advanceFrame(0);
  advanceFrame(650);
  expect(view.container).toHaveTextContent("100");
  view.rerender(<AnimatedMetric value={200} />);
  advanceFrame(700);
  expect(view.container).toHaveTextContent("100");
  advanceFrame(1025);
  expect(view.container).toHaveTextContent("188");
  advanceFrame(1350);
  expect(view.container).toHaveTextContent("200");
  view.rerender(<AnimatedMetric value={200} />);
  expect(frames.size).toBe(0);
});

it("animates decreases, including a transition to zero", () => {
  const view = render(<AnimatedMetric value={100} />);
  advanceFrame(0);
  advanceFrame(650);
  view.rerender(<AnimatedMetric value={0} />);
  advanceFrame(700);
  expect(view.container).toHaveTextContent("100");
  advanceFrame(1025);
  expect(view.container).toHaveTextContent("13");
  advanceFrame(1350);
  expect(view.container).toHaveTextContent("0");
});

it("continues from the current intermediate value when a new target arrives", () => {
  const view = render(<AnimatedMetric value={100} />);
  advanceFrame(0);
  advanceFrame(650);
  view.rerender(<AnimatedMetric value={200} />);
  advanceFrame(700);
  advanceFrame(1025);
  expect(view.container).toHaveTextContent("188");
  view.rerender(<AnimatedMetric value={300} />);
  expect(frames.size).toBe(1);
  advanceFrame(1100);
  expect(view.container).toHaveTextContent("188");
  advanceFrame(1750);
  expect(view.container).toHaveTextContent("300");
});

it("respects reduced motion and cancels pending frames on unmount", () => {
  vi.stubGlobal("matchMedia", () => ({ matches: true }));
  const view = render(<AnimatedMetric value={150} format="currency" />);
  advanceFrame(0);
  expect(view.container.textContent).toMatch(/150,00/);
  expect(frames.size).toBe(0);
  view.rerender(<AnimatedMetric value={175} format="currency" />);
  view.unmount();
  expect(frames.size).toBe(0);
});
