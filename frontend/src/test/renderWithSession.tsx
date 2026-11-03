import { render } from "@testing-library/react";
import { QueryClientProvider } from "@tanstack/react-query";
import type { ReactElement } from "react";
import { createQueryClient } from "@/providers/QueryProvider";
import { SessionProvider } from "@/features/auth/context/SessionProvider";

export function renderWithSession(ui: ReactElement, session: { userId?: string } = {}) {
  const client = createQueryClient();
  const rendered = render(
    <QueryClientProvider client={client}>
      <SessionProvider session={{ tenantId: "tenant-test", contextId: "session-test", userId: session.userId ?? "user-test" }}>
        {ui}
      </SessionProvider>
    </QueryClientProvider>,
  );
  return { ...rendered, client };
}
