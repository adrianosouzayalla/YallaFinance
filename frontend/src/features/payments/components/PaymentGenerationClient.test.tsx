import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, expect, it, vi } from "vitest";
import { QueryClientProvider } from "@tanstack/react-query";
import { SessionProvider } from "@/features/auth/context/SessionProvider";
import { createQueryClient } from "@/providers/QueryProvider";
import { PaymentGenerationClient } from "./PaymentGenerationClient";
import type { PaymentPreparation } from "../types/payment.types";

const mocks = vi.hoisted(() => ({ preparation: undefined as PaymentPreparation | undefined }));
vi.mock("next/navigation", () => ({
  useSearchParams: () => new URLSearchParams("chargeIds=11111111-1111-4111-8111-111111111111"),
  useRouter: () => ({ push: vi.fn(), replace: vi.fn() }),
}));
vi.mock("../hooks/usePaymentPreparation", () => ({ usePaymentPreparation: () => ({ data: mocks.preparation, isError: false }) }));
vi.mock("../hooks/useGeneratePaymentRequests", () => ({ useGeneratePaymentRequests: () => ({ mutateAsync: vi.fn(), isPending: false }) }));
afterEach(() => { cleanup(); sessionStorage.clear(); });

it("preserves the form when eligibility refreshes instead of recreating it", () => {
  mocks.preparation = {
    charges: [{ id: "11111111-1111-4111-8111-111111111111", reference: "10-123", customerName: "Cliente", contract: "C1", amount: 42,
      eligible: true, blockReason: null, createdAt: "2026-10-09T12:00:00Z", dueDate: "2026-10-30" }],
    methods: [{ id: "22222222-2222-4222-8222-222222222222", name: "Pix", code: "Pix" }],
    maximumLinkDays: 30,
  };
  const client = createQueryClient();
  const { rerender } = render(<PaymentGenerationClient />, { wrapper: ({ children }) =>
    <QueryClientProvider client={client}><SessionProvider session={{ tenantId: "tenant-test", contextId: "session-test" }}>
      {children}
    </SessionProvider></QueryClientProvider> });
  const note = screen.getByLabelText("Nota interna");
  fireEvent.change(note, { target: { value: "Manter meu preenchimento" } });
  fireEvent.click(screen.getByRole("checkbox", { name: "Pix" }));
  mocks.preparation = { ...mocks.preparation, charges: mocks.preparation.charges.map((charge) => ({ ...charge,
    eligible: false, blockReason: "ActiveRequestExists" })) };
  rerender(<PaymentGenerationClient />);
  expect(screen.getByLabelText("Nota interna")).toBe(note);
  expect(note).toHaveValue("Manter meu preenchimento");
  expect(screen.getByRole("checkbox", { name: "Pix" })).not.toBeChecked();
});
