import { Box, IconButton, Paper, Stack, Tooltip, Typography } from "@mui/material";
import Link from "next/link";
import { financeTokens } from "@/theme/designTokens";
import { AnimatedMetric } from "@/features/dashboard/components/AnimatedMetric";

export type IndicatorIconName = "pending" | "active" | "overdue" | "stale";

interface OperationalIndicatorCardProps {
  label: string;
  value: number;
  valueFormat?: "number" | "currency";
  valueLabel: string;
  description: string;
  href: string;
  actionLabel: string;
  icon: IndicatorIconName;
  accent: string;
  secondaryValue: number;
  secondaryFormat?: "number" | "currency";
  secondaryLabel: string;
}

function IndicatorIcon({ name }: { name: IndicatorIconName }) {
  return (
    <Box
      component="svg"
      viewBox="0 0 24 24"
      aria-hidden="true"
      sx={{
        fill: "none",
        height: 20,
        stroke: "currentColor",
        strokeLinecap: "round",
        strokeLinejoin: "round",
        strokeWidth: 1.8,
        width: 20,
      }}
    >
      {name === "pending" && (
        <>
          <circle cx="12" cy="12" r="8.5" />
          <path d="M12 7v5l3 2" />
        </>
      )}
      {name === "active" && (
        <>
          <path d="m5.5 15.5 4.5-4.5 3 3L18.5 8" />
          <path d="M15.5 8h3v3" />
        </>
      )}
      {name === "overdue" && (
        <>
          <path d="M12 4.5v15" />
          <path d="M16.5 8.5c0-1.7-1.8-3-4.5-3s-4.5 1.3-4.5 3 1.5 2.5 4.5 3 4.5 1.3 4.5 3-1.8 3-4.5 3-4.5-1.3-4.5-3" />
        </>
      )}
      {name === "stale" && (
        <>
          <circle cx="12" cy="12" r="8.5" />
          <path d="M12 7v5l3 2" />
          <path d="M17.5 4.5v3h-3" />
          <path d="M17.5 7.5A7 7 0 0 0 7.2 5.8" />
        </>
      )}
    </Box>
  );
}

function InfoIcon() {
  return (
    <Box
      component="svg"
      viewBox="0 0 24 24"
      aria-hidden="true"
      sx={{
        fill: "none",
        height: 18,
        stroke: "currentColor",
        strokeLinecap: "round",
        strokeLinejoin: "round",
        strokeWidth: 2,
        width: 18,
      }}
    >
      <circle cx="12" cy="12" r="9" />
      <path d="M12 11v5" />
      <path d="M12 8h.01" />
    </Box>
  );
}

export function OperationalIndicatorCard({
  label,
  value,
  valueFormat = "number",
  valueLabel,
  description,
  href,
  actionLabel,
  icon,
  accent,
  secondaryValue,
  secondaryFormat = "currency",
  secondaryLabel,
}: OperationalIndicatorCardProps) {
  return (
    <Paper
      component={Link}
      href={href}
      variant="outlined"
      sx={{
        backgroundColor: accent,
        border: "none",
        borderRadius: `${financeTokens.radius.panel}px`,
        color: "inherit",
        display: "block",
        minHeight: financeTokens.density.operationalCardMinHeight,
        p: financeTokens.density.operationalCardPadding,
        textDecoration: "none",
        transition: "box-shadow 160ms ease, transform 160ms ease",
        "&:hover": {
          boxShadow: "0 10px 24px rgba(7, 41, 101, 0.14)",
          transform: "translateY(-2px)",
        },
      }}
    >
      <Stack spacing={1.35}>
        <Box sx={{ alignItems: "center", display: "flex", justifyContent: "space-between" }}>
          <Box sx={{ alignItems: "center", display: "flex", gap: 1 }}>
            <Box
              aria-hidden="true"
              sx={{
                alignItems: "center",
                backgroundColor: "rgba(255,255,255,0.78)",
                borderRadius: "50%",
                color: financeTokens.colors.header,
                display: "flex",
                height: financeTokens.density.operationalIconSize,
                justifyContent: "center",
                width: financeTokens.density.operationalIconSize,
              }}
            >
              <IndicatorIcon name={icon} />
            </Box>
            <Typography sx={{ color: financeTokens.colors.header, fontSize: financeTokens.typography.body, fontWeight: 700 }}>
              {label}
            </Typography>
          </Box>
          <Tooltip title={description} arrow>
            <IconButton
              aria-label={`O que significa ${label}`}
              size="small"
              onClick={(event) => {
                event.preventDefault();
                event.stopPropagation();
              }}
              sx={{ color: financeTokens.colors.header, p: 0.25 }}
            >
              <InfoIcon />
            </IconButton>
          </Tooltip>
        </Box>

        <Stack direction="row" spacing={0.75} sx={{ alignItems: "baseline" }}>
          <Typography sx={{ color: financeTokens.colors.header, fontSize: financeTokens.typography.metric, fontWeight: 700, lineHeight: 1.1 }}>
            <AnimatedMetric value={value} format={valueFormat} />
          </Typography>
          <Typography color="text.secondary" sx={{ fontSize: financeTokens.typography.secondary, fontWeight: 600 }}>
            {valueLabel}
          </Typography>
        </Stack>

        <Stack direction="row" spacing={0.75} sx={{ alignItems: "baseline" }}>
          <Typography color="text.secondary" sx={{ fontSize: financeTokens.typography.body }}>
            <AnimatedMetric value={secondaryValue} format={secondaryFormat} />
          </Typography>
          <Typography color="text.secondary" sx={{ fontSize: financeTokens.typography.secondary }}>
            {secondaryLabel}
          </Typography>
        </Stack>

        <Box sx={{ color: financeTokens.colors.header, fontSize: financeTokens.typography.body, fontWeight: 700 }}>
          {actionLabel} →
        </Box>
      </Stack>
    </Paper>
  );
}
