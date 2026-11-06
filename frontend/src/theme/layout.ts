// Constantes de espaçamento e grade compartilhadas pelas telas.

export const fieldGap = "1rem";

export const actionGap = "0.75rem";

export const panelInset = "1.25rem";

export const buttonRow = {
  display: "flex",
  flexWrap: "wrap",
  gap: actionGap,
  justifyContent: "flex-end",
} as const;

export const chargeGrid = {
  display: "grid",
  gap: fieldGap,
  gridTemplateColumns: "minmax(0, 1fr)",
  alignItems: "start",
} as const;

export const fieldGrid = {
  display: "grid",
  gap: fieldGap,
  width: "100%",
  gridTemplateColumns: {
    xs: "minmax(0, 1fr)",
    md: "repeat(3, minmax(0, 1fr))",
  },
} as const;
