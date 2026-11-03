import { render, screen } from "@testing-library/react";
import { expect, it } from "vitest";
import { PaymentReceiptDetails } from "./PaymentReceiptDetails";

it("shows each received payment with its amount, date, method, provider and bank reference", () => {
  render(<PaymentReceiptDetails amount={150} receivedAmount={150} settled settledAt="2026-10-15T15:30:00Z"
    receipts={[
      { id: "receipt-1", endToEndId: "E2E-1", amount: 100, receivedAt: "2026-10-14T15:00:00Z", paymentMethodName: "PIX", provider: "Cielo" },
      { id: "receipt-2", endToEndId: "E2E-2", amount: 50, receivedAt: "2026-10-15T15:00:00Z", paymentMethodName: "PIX", provider: "Cielo" },
    ]} />);

  expect(screen.getByText("Recebimento 1")).toBeVisible();
  expect(screen.getByText("Recebimento 2")).toBeVisible();
  expect(screen.getByText("receipt-1")).toBeVisible();
  expect(screen.getByText("receipt-2")).toBeVisible();
  expect(screen.getAllByText("PIX")).toHaveLength(2);
  expect(screen.getAllByText("Cielo")).toHaveLength(2);
  expect(screen.getByText("E2E-1")).toBeVisible();
  expect(screen.getByText("E2E-2")).toBeVisible();
  expect(screen.getByText("Baixa confirmada em")).toBeVisible();
});
