import type { ComponentType } from "react";
import HomeOutlinedIcon from "@mui/icons-material/HomeOutlined";
import ReceiptLongOutlinedIcon from "@mui/icons-material/ReceiptLongOutlined";
import AccountBalanceWalletOutlinedIcon from "@mui/icons-material/AccountBalanceWalletOutlined";
import PaymentsOutlinedIcon from "@mui/icons-material/PaymentsOutlined";
import ListAltOutlinedIcon from "@mui/icons-material/ListAltOutlined";
import NoteAddOutlinedIcon from "@mui/icons-material/NoteAddOutlined";
import DevicesOutlinedIcon from "@mui/icons-material/DevicesOutlined";
import PeopleOutlineIcon from "@mui/icons-material/PeopleOutlined";
import BadgeOutlinedIcon from "@mui/icons-material/BadgeOutlined";
import RequestQuoteOutlinedIcon from "@mui/icons-material/RequestQuoteOutlined";
import SettingsOutlinedIcon from "@mui/icons-material/SettingsOutlined";

export type FinanceNavigationItem = {
  href:
    | "/home"
    | "/debts"
    | "/debts/new"
    | "/charges/new"
    | "/direct-charges/new"
    | "/jobs"
    | "/payments"
    | "/sessions"
    | "/users"
    | "/profiles"
    | "/settings";
  label: string;
  breadcrumb: string;
  functionalityCode?: string;
  alwaysVisible?: boolean;
  icon:
    | "home"
    | "debts"
    | "new-debt"
    | "charges"
    | "direct-charges"
    | "jobs"
    | "payments"
    | "sessions"
    | "users"
    | "profiles"
    | "settings";
  Icon: ComponentType<{ fontSize?: "small" | "medium" | "large" | "inherit" }>;
};

export const financeNavigation: readonly FinanceNavigationItem[] = [
  {
    href: "/home",
    label: "Início",
    breadcrumb: "Visão geral",
    alwaysVisible: true,
    icon: "home",
    Icon: HomeOutlinedIcon,
  },
  {
    href: "/debts",
    label: "Débitos",
    breadcrumb: "Débitos",
    functionalityCode: "DEBTS",
    icon: "debts",
    Icon: ReceiptLongOutlinedIcon,
  },
  {
    href: "/debts/new",
    label: "Débito avulso",
    breadcrumb: "Débito avulso",
    functionalityCode: "DEBTS_CREATE",
    icon: "new-debt",
    Icon: NoteAddOutlinedIcon,
  },
  {
    href: "/charges/new",
    label: "Cobrança avulsa",
    breadcrumb: "Cobrança avulsa",
    functionalityCode: "CHARGES_CREATE",
    icon: "charges",
    Icon: AccountBalanceWalletOutlinedIcon,
  },
  {
    href: "/direct-charges/new",
    label: "Cobrança Direta",
    breadcrumb: "Cobrança Direta",
    functionalityCode: "DIRECT_CHARGES_CREATE",
    icon: "direct-charges",
    Icon: RequestQuoteOutlinedIcon,
  },
  {
    href: "/payments",
    label: "Pagamentos",
    breadcrumb: "Pagamentos",
    functionalityCode: "PAYMENTS",
    icon: "payments",
    Icon: PaymentsOutlinedIcon,
  },
  {
    href: "/jobs",
    label: "Jobs",
    breadcrumb: "Jobs",
    functionalityCode: "JOBS",
    icon: "jobs",
    Icon: ListAltOutlinedIcon,
  },
  {
    href: "/sessions",
    label: "Sessões ativas",
    breadcrumb: "Sessões ativas",
    functionalityCode: "SESSIONS",
    icon: "sessions",
    Icon: DevicesOutlinedIcon,
  },
  {
    href: "/users",
    label: "Usuários",
    breadcrumb: "Usuários",
    functionalityCode: "USERS",
    icon: "users",
    Icon: PeopleOutlineIcon,
  },
  {
    href: "/profiles",
    label: "Perfis",
    breadcrumb: "Perfis",
    functionalityCode: "PROFILES",
    icon: "profiles",
    Icon: BadgeOutlinedIcon,
  },
  {
    href: "/settings",
    label: "Configurações",
    breadcrumb: "Configurações do sistema",
    functionalityCode: "SYSTEM_SETTINGS",
    icon: "settings",
    Icon: SettingsOutlinedIcon,
  },
];

export function getRequiredFunctionality(pathname: string): string | undefined {
  if (pathname === "/settings" || pathname.startsWith("/settings/")) return "SYSTEM_SETTINGS";
  if (pathname === "/direct-charges" || pathname.startsWith("/direct-charges/"))
    return "DIRECT_CHARGES_CREATE";
  if (pathname === "/debts/new") return "DEBTS_CREATE";
  if (pathname === "/debts" || pathname.startsWith("/debts/")) return "DEBTS";
  if (
    ["/charges/new", "/charges/review", "/charges/confirmation", "/charges/success"].includes(
      pathname,
    )
  )
    return "CHARGES_CREATE";
  if (pathname.startsWith("/charges/")) return "CHARGES";
  if (pathname === "/payments" || pathname.startsWith("/payments/")) return "PAYMENTS";
  if (pathname === "/jobs" || pathname.startsWith("/jobs/")) return "JOBS";
  if (pathname === "/sessions" || pathname.startsWith("/sessions/")) return "SESSIONS";
  if (pathname === "/users" || pathname.startsWith("/users/")) return "USERS";
  if (pathname === "/profiles" || pathname.startsWith("/profiles/")) return "PROFILES";
  return undefined;
}

export function getActiveNavigationItem(pathname: string): FinanceNavigationItem {
  const exact = financeNavigation.find((item) => item.href === pathname);
  if (exact) return exact;
  const parent =
    pathname === "/direct-charges" || pathname.startsWith("/direct-charges/")
      ? "/direct-charges/new"
      : pathname.startsWith("/settings/")
        ? "/settings"
        : pathname.startsWith("/users/")
          ? "/users"
          : pathname.startsWith("/profiles/")
            ? "/profiles"
            : pathname.startsWith("/sessions/")
              ? "/sessions"
              : pathname.startsWith("/charges/")
                ? "/charges/new"
                : pathname.startsWith("/debts/")
                  ? "/debts"
                  : pathname.startsWith("/jobs/")
                    ? "/jobs"
                    : pathname.startsWith("/payments/")
                      ? "/payments"
                      : "/home";
  return financeNavigation.find((item) => item.href === parent) ?? financeNavigation[0];
}

export function getBreadcrumbItems(pathname: string): Array<{ label: string; href?: string }> {
  const active = getActiveNavigationItem(pathname);
  if (active.href === "/home") return [{ label: "Início" }, { label: active.breadcrumb }];
  return [{ label: "Início", href: "/home" }, { label: active.breadcrumb }];
}
