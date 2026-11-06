/** Keep bigint components as decimal text throughout the browser contract. */
export function isChargeReference(value: string): boolean {
  if (value.length > 39 || !/^[1-9][0-9]*-[1-9][0-9]*$/.test(value)) return false;
  const maximum = "9223372036854775807";
  return value
    .split("-")
    .every(
      (part) => part.length < maximum.length || (part.length === maximum.length && part <= maximum),
    );
}
