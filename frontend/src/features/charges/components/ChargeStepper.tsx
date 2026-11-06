"use client";

import type { ReactNode } from "react";
import { Box, Typography } from "@mui/material";
import { financeTokens } from "@/theme/designTokens";

const defaultSteps = ["Dados da cobrança", "Revisão e forma de cobrança", "Confirmação"];

interface ChargeStepperProps {
  activeStep: number;
  steps?: readonly string[];
  icons?: readonly ReactNode[];
  ariaLabel?: string;
  responsive?: boolean;
}

export function ChargeStepper({
  activeStep,
  steps = defaultSteps,
  icons,
  ariaLabel = "Etapas da cobrança",
  responsive = false,
}: ChargeStepperProps) {
  return (
    <Box
      aria-label={ariaLabel}
      component="ol"
      sx={{
        display: "grid",
        gap: 1.5,
        gridTemplateColumns: responsive
          ? { xs: "repeat(2, minmax(0, 1fr))", md: "repeat(4, minmax(0, 1fr))" }
          : "repeat(3, minmax(0, 1fr))",
        listStyle: "none",
        m: 0,
        p: 0,
      }}
    >
      {steps.map((label, index) => {
        const active = index === activeStep;
        const stepLabel = icons?.[index] ? label : `${index + 1}. ${label}`;

        return (
          <Box
            aria-label={active ? `${label}, etapa atual` : label}
            aria-current={active ? "step" : undefined}
            component="li"
            key={label}
            sx={{
              alignItems: "center",
              backgroundColor: active
                ? financeTokens.colors.action
                : financeTokens.colors.stepBackground,
              border: `1px solid ${active ? financeTokens.colors.action : financeTokens.colors.subtleBorder}`,
              borderRadius: `${financeTokens.radius.control}px`,
              display: "flex",
              gap: icons ? "0.5rem" : undefined,
              minHeight: 48,
              justifyContent: "center",
              px: 2,
              color: active ? financeTokens.colors.canvas : financeTokens.colors.mutedText,
            }}
          >
            {icons?.[index] && (
              <Box
                component="span"
                aria-hidden="true"
                sx={{ display: "inline-flex", flexShrink: 0 }}
              >
                {icons[index]}
              </Box>
            )}
            <Typography
              sx={{
                color: active ? financeTokens.colors.canvas : financeTokens.colors.mutedText,
                fontSize: financeTokens.typography.body,
                fontWeight: active ? 700 : 500,
              }}
            >
              {stepLabel}
            </Typography>
          </Box>
        );
      })}
    </Box>
  );
}
