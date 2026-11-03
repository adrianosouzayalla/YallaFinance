"use client";

import { startTransition, useActionState, useEffect } from "react";
import { Controller, useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { loginSchema } from "@/features/auth/schemas/login.schema";
import { useSearchParams } from "next/navigation";
import { Box, Button, MenuItem, Paper, TextField, Typography } from "@mui/material";
import { loginAction } from "@/features/auth/actions";
import { financeTokens } from "@/theme/designTokens";
import { LoadingState } from "@/components/ui/LoadingState";
import {
  INITIAL_LOGIN_STATE,
  type LoginActionState,
  type LoginInput,
} from "@/features/auth/authSession";

type LoginFormAction = (
  previousState: LoginActionState,
  formData: FormData,
) => Promise<LoginActionState>;

interface LoginFormProps {
  action?: LoginFormAction;
}

export function LoginForm({ action = loginAction }: LoginFormProps) {
  const [state, formAction, pending] = useActionState(action, INITIAL_LOGIN_STATE);
  const form = useForm<LoginInput>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: "", password: "", tenantId: "" },
  });
  const { reset, getValues } = form;
  useEffect(() => {
    reset({ email: state.email ?? "", password: getValues("password"), tenantId: "" });
  }, [state, reset, getValues]);
  const searchParams = useSearchParams();
  const sessionExpired = searchParams.get("error") === "session-expired";
  const apiUnavailable = searchParams.get("error") === "api-unavailable";

  return (
    <>
    <LoadingState mode="blocking" open={pending} message="Entrando no Yalla Finance..." />
    <Paper
      component="section"
      sx={{
        backgroundColor: "transparent",
        boxShadow: "none",
        p: 0,
        width: { xs: "100%", sm: financeTokens.auth.formWidth },
        maxWidth: financeTokens.auth.formMaxWidth,
      }}
    >
      <Box
        component="img"
        src="/assets/brand/yallafinance-logo.svg"
        alt="Yalla Finance"
        sx={{ display: "block", width: financeTokens.auth.logoWidth, maxWidth: "100%", height: "auto", mx: "auto" }}
      />

      <Box
        component="form"
        action={formAction}
        onSubmit={(event) => {
          void form.handleSubmit((values) => {
            const data = new FormData();
            for (const [key, value] of Object.entries(values))
              if (value !== undefined) data.set(key, value);
            startTransition(() => formAction(data));
          })(event);
        }}
        aria-label="Acesso ao Yalla Finance"
        noValidate
        sx={{ display: "flex", flexDirection: "column", gap: financeTokens.auth.formGap, mt: financeTokens.auth.formTopMargin }}
      >
        <Controller
          name="email"
          control={form.control}
          render={({ field }) => (
            <TextField
              {...field}
              autoComplete="username"
              error={Boolean(state.fieldErrors.email)}
              fullWidth
              helperText={state.fieldErrors.email}
              label="Login"
            />
          )}
        />
        <Controller
          name="password"
          control={form.control}
          render={({ field }) => (
            <TextField
              {...field}
              autoComplete="current-password"
              error={Boolean(state.fieldErrors.password)}
              fullWidth
              helperText={state.fieldErrors.password}
              label="Senha"
              sx={{ mt: financeTokens.auth.formGap / 2 }}
              type="password"
            />
          )}
        />
        {state.tenantOptions && state.tenantOptions.length > 1 ? (
          <Controller
            name="tenantId"
            control={form.control}
            render={({ field }) => (
              <TextField {...field} select label="Empresa" required>
                {state.tenantOptions!.map((tenant) => (
                  <MenuItem key={tenant.id} value={tenant.id}>
                    {tenant.name}
                  </MenuItem>
                ))}
              </TextField>
            )}
          />
        ) : null}

        {sessionExpired ? (
          <Typography color="warning.main" role="alert" variant="body2">
            Sua sessão expirou. Entre novamente para continuar.
          </Typography>
        ) : null}

        {apiUnavailable ? (
          <Typography color="warning.main" role="alert" variant="body2">
            A conexão com o sistema foi perdida. Verifique sua conexão e tente entrar novamente.
          </Typography>
        ) : null}

        {state.formError ? (
          <Typography color="error" role="alert" variant="body2">
            {state.formError}
          </Typography>
        ) : null}

        <Button disabled={pending} fullWidth type="submit" variant="contained">
          Entrar
        </Button>
      </Box>
    </Paper>
    </>
  );
}
