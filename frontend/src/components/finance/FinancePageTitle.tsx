import { Box, Typography, type TypographyProps } from "@mui/material";
import { financeTokens } from "@/theme/designTokens";
import { getActiveNavigationItem, type FinanceNavigationItem } from "./navigation";

type FinancePageTitleProps = Omit<TypographyProps<"h1">, "component"> & {
  navigationHref: FinanceNavigationItem["href"];
};

export function FinancePageTitle({
  navigationHref,
  children,
  sx,
  variant = "h1",
  ...props
}: FinancePageTitleProps) {
  const { Icon } = getActiveNavigationItem(navigationHref);
  return (
    <Typography
      {...props}
      component="h1"
      variant={variant}
      sx={[
        {
          display: "flex",
          alignItems: "center",
          gap: 1.5,
          color: financeTokens.colors.header,
          fontWeight: 700,
        },
        ...(Array.isArray(sx) ? sx : sx ? [sx] : []),
      ]}
    >
      <Box component="span" aria-hidden="true" sx={{ display: "inline-flex", flexShrink: 0 }}>
        <Icon fontSize="inherit" />
      </Box>
      <Box component="span">{children}</Box>
    </Typography>
  );
}
