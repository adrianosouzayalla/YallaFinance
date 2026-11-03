"use client";

import { NiboConnectionScreen } from "../providers/nibo/NiboConnectionScreen";
import { SicoobConnectionScreen } from "../providers/sicoob/SicoobConnectionScreen";
import { AsaasConnectionScreen } from "../providers/asaas/AsaasConnectionScreen";
import { UnsupportedConnectionScreen } from "./shared/UnsupportedConnectionScreen";
import type { BusyReporter } from "./shared/SettingsComponents";

export function ConnectionProviderHost({ provider, id, reportBusy, reportDirty, onCreated }: {
  provider: string; id: string | null; reportBusy: BusyReporter;
  reportDirty(resource: string, dirty: boolean): void; onCreated(id: string): void;
}) {
  const screenProps = { id, reportBusy, reportDirty, onCreated };
  if (provider === "Nibo") return <NiboConnectionScreen {...screenProps} />;
  if (provider === "Sicoob") return <SicoobConnectionScreen {...screenProps} />;
  if (provider === "Asaas") return <AsaasConnectionScreen {...screenProps} />;
  return <UnsupportedConnectionScreen provider={provider} {...screenProps} />;
}
