import type { ReactNode } from "react";
import { Box } from "@mui/material";

export function JobDetailField({ label, children }: { label: string; children: ReactNode }) {
  return (
    <Box sx={{ overflowWrap: "anywhere", lineHeight: 1.7 }}>
      <Box component="strong" sx={{ fontWeight: 700 }}>
        {label}:
      </Box>{" "}
      {children}
    </Box>
  );
}
