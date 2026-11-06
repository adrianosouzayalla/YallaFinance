import { ChargeDraftProvider } from "@/features/charges/context/ChargeDraftProvider";

export default function ChargesLayout({ children }: { children: React.ReactNode }) {
  return <ChargeDraftProvider>{children}</ChargeDraftProvider>;
}
