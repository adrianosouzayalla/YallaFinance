import { copyTextToClipboard as copyText } from "@/utils/clipboard";

export interface CopyResult {
  ok: boolean;
  message: string;
}

export async function copyTextToClipboard(
  text: string,
  successMessage: string,
  unavailableMessage: string,
  clipboard?: Pick<Clipboard, "writeText">,
): Promise<CopyResult> {
  const result = await copyText(text, clipboard);
  return result.ok
    ? { ok: true, message: successMessage }
    : { ok: false, message: unavailableMessage };
}

export function copyPixPayload(payload: string, clipboard?: Pick<Clipboard, "writeText">): Promise<CopyResult> {
  return copyTextToClipboard(
    payload,
    "C\u00f3digo PIX copiado",
    "N\u00e3o foi poss\u00edvel copiar o c\u00f3digo PIX. Selecione e copie manualmente.",
    clipboard,
  );
}
