"use client";

import { formatCurrency } from "@/utils/currency";
import { useEffect, useRef, useState } from "react";

interface AnimatedMetricProps {
  value: number;
  format?: "number" | "currency";
  className?: string;
}

const numberFormatter = new Intl.NumberFormat("pt-BR");

export function AnimatedMetric({ value, format = "number", className }: AnimatedMetricProps) {
  const [displayValue, setDisplayValue] = useState(0);
  const displayedValue = useRef(0);

  useEffect(() => {
    const from = displayedValue.current;
    if (from === value) return;

    const reducedMotion =
      typeof window.matchMedia === "function" &&
      window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    if (reducedMotion) {
      const frame = requestAnimationFrame(() => {
        displayedValue.current = value;
        setDisplayValue(value);
      });
      return () => cancelAnimationFrame(frame);
    }

    const duration = 650;
    let start: number | null = null;
    let frame = 0;
    const tick = (now: number) => {
      start ??= now;
      const progress = Math.max(0, Math.min((now - start) / duration, 1));
      const eased = 1 - Math.pow(1 - progress, 3);
      const next = from + (value - from) * eased;
      displayedValue.current = next;
      setDisplayValue(next);
      if (progress < 1) frame = requestAnimationFrame(tick);
    };
    frame = requestAnimationFrame(tick);
    return () => cancelAnimationFrame(frame);
  }, [value]);

  return (
    <span className={className}>
      {format === "currency"
        ? formatCurrency(displayValue)
        : numberFormatter.format(Math.round(displayValue))}
    </span>
  );
}
