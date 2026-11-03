"use client";

import { useEffect } from "react";
import { useToast } from "@/providers/ToastProvider";
import { HomeIndicators } from "@/features/dashboard/components/HomeIndicators";

export function HomePageClient() {
  const { showToast } = useToast();

  useEffect(() => {
    const url = new URL(window.location.href);

    if (url.searchParams.get("toast") !== "login-success") return;

    showToast({ message: "Login realizado com sucesso.", severity: "success" });
    url.searchParams.delete("toast");
    window.history.replaceState(null, "", `${url.pathname}${url.search}${url.hash}`);
  }, [showToast]);

  return <HomeIndicators />;
}
