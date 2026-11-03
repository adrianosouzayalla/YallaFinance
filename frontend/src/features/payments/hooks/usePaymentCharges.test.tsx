import { cleanup, fireEvent, screen, waitFor } from "@testing-library/react";
import { useState } from "react";
import { afterEach, expect, it, vi } from "vitest";
import { renderWithSession } from "@/test/renderWithSession";
import { usePaymentCharges } from "./usePaymentCharges";

const mocks = vi.hoisted(() => ({ list: vi.fn() }));
vi.mock("../api/payments.client", () => ({ paymentsApi: { list: mocks.list } }));
afterEach(() => { cleanup(); mocks.list.mockReset(); });

function Probe() {
  const [paymentStatus, setStatus] = useState<"available" | undefined>();
  const query = usePaymentCharges({ startDate: "", endDate: "", page: 1, pageSize: 5, paymentStatus });
  return <><button onClick={() => setStatus("available")}>Filtrar</button>
    <span>{query.data?.totalItems ?? "sem dados"}</span><span>{query.isFetching ? "atualizando" : "pronto"}</span></>;
}

it("keeps previous data visible while a new status is fetched", async () => {
  let finish!: (value: { totalItems: number }) => void;
  mocks.list.mockResolvedValueOnce({ totalItems: 10 });
  mocks.list.mockImplementationOnce(() => new Promise((resolve) => { finish = resolve; }));
  renderWithSession(<Probe />);
  await screen.findByText("10");
  fireEvent.click(screen.getByText("Filtrar"));
  await waitFor(() => expect(mocks.list).toHaveBeenCalledTimes(2));
  expect(screen.getByText("10")).toBeVisible();
  expect(screen.getByText("atualizando")).toBeVisible();
  finish({ totalItems: 3 });
  await screen.findByText("3");
});
