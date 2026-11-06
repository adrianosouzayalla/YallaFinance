interface CopyTextResult {
  ok: boolean;
}

export async function copyTextToClipboard(
  text: string,
  clipboard?: Pick<Clipboard, "writeText">,
): Promise<CopyTextResult> {
  const target = clipboard ?? (typeof navigator === "undefined" ? undefined : navigator.clipboard);

  if (target && typeof target.writeText === "function") {
    try {
      await target.writeText(text);
      return { ok: true };
    } catch {
      // HTTP em endereços de rede local pode impedir a Clipboard API; tenta a cópia compatível abaixo.
    }
  }

  if (typeof document === "undefined" || typeof document.execCommand !== "function")
    return { ok: false };

  const previousFocus =
    document.activeElement instanceof HTMLElement ? document.activeElement : null;
  const textarea = document.createElement("textarea");
  textarea.value = text;
  textarea.setAttribute("readonly", "");
  textarea.setAttribute("aria-hidden", "true");
  textarea.style.position = "fixed";
  textarea.style.opacity = "0";
  textarea.style.pointerEvents = "none";
  textarea.style.left = "-9999px";
  document.body.appendChild(textarea);
  try {
    textarea.focus();
    textarea.select();
    return { ok: document.execCommand("copy") };
  } catch {
    return { ok: false };
  } finally {
    textarea.remove();
    previousFocus?.focus();
  }
}
