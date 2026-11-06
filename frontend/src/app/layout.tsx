import type { Metadata } from "next";
import type { CSSProperties } from "react";
import "@fontsource/inter/400.css";
import "@fontsource/inter/500.css";
import "@fontsource/inter/600.css";
import "@fontsource/inter/700.css";
import { AppProviders } from "@/providers/AppProviders";
import { financeTokens } from "@/theme/designTokens";
import "@/theme/styles/yallaCamelLoader.css";
import "@/theme/styles/globals.css";

export const metadata: Metadata = {
  title: "Yalla Finance",
  icons: {
    icon: "/favicon.ico",
    shortcut: "/assets/brand/favicon-32.png",
    apple: "/assets/brand/favicon-512.png",
  },
  description: "Consulta de débitos e geração de cobranças da Yalla Car.",
};

const uiScaleStyles = {
  "--yf-ui-scale": financeTokens.uiScale,
} as CSSProperties;

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html lang="pt-BR" style={uiScaleStyles}>
      <body>
        <div className="yf-app-zoom">
          <AppProviders>{children}</AppProviders>
        </div>
      </body>
    </html>
  );
}
