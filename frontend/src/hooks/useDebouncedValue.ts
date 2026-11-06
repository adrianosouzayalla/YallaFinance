import { useEffect, useState } from "react";

export function useDebouncedValue<T>(value: T, delayMilliseconds = 300): T {
  const [settled, setSettled] = useState(value);
  useEffect(() => {
    const timer = setTimeout(() => setSettled(value), delayMilliseconds);
    return () => clearTimeout(timer);
  }, [value, delayMilliseconds]);
  return settled;
}
