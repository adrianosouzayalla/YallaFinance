export function normalizePlate(value: string): string {
  return value.replace(/[^a-z0-9]/gi, "").toUpperCase();
}

export function isValidPlate(value: string): boolean {
  return /^[A-Z]{3}[0-9][A-Z0-9][0-9]{2}$/.test(value);
}

export function formatPlate(value: string): string {
  const plate = normalizePlate(value);
  return plate.length > 3 && !/[A-Z]/.test(plate[4] ?? "")
    ? `${plate.slice(0, 3)}-${plate.slice(3)}`
    : plate;
}
