"use client";

import Link from "next/link";
import { Alert, Box, Button, Typography } from "@mui/material";

export function AccessDeniedPage() {
  return (
    <Box sx={{ mx: "auto", maxWidth: 620, py: { xs: 4, md: 8 } }}>
      <Alert severity="warning" sx={{ mb: 2 }}>
        Você não tem acesso a esta funcionalidade.
      </Alert>
      <Typography color="text.secondary" sx={{ mb: 3 }}>
        Solicite ao administrador da empresa acesso ao perfil correspondente.
      </Typography>
      <Button component={Link} href="/home" variant="contained">
        Voltar para a Home
      </Button>
    </Box>
  );
}
