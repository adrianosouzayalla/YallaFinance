"use client";
import {
  Alert,
  Box,
  Button,
  Divider,
  FormControlLabel,
  MenuItem,
  Stack,
  Switch,
  TextField,
  Typography,
} from "@mui/material";
import { useState } from "react";
import type { ProfileItem, UserItem } from "@/services/api/users/types";
export function UserEditor({
  user,
  profiles,
  initialPassword,
  busy,
  error,
  onClose,
  onSave,
}: {
  user: UserItem | null;
  profiles: ProfileItem[];
  initialPassword: string | null;
  busy: boolean;
  error: string;
  onClose: () => void;
  onSave: (input: {
    login: string;
    name: string;
    email: string;
    active: boolean;
    profileId: string | null;
  }) => void;
}) {
  const [login, setLogin] = useState(user?.login ?? "");
  const [name, setName] = useState(user?.name ?? "");
  const [email, setEmail] = useState(user?.email ?? "");
  const [profileId, setProfile] = useState(user?.profileId ?? "");
  const [active, setActive] = useState(true);
  return (
    <Stack
      component="form"
      spacing={3}
      sx={{ p: 2.5 }}
      onSubmit={(event) => {
        event.preventDefault();
        if (busy || !name.trim() || !email.trim() || !login.trim() || (!user && !initialPassword))
          return;
        onSave({ login, name, email, active, profileId: profileId || null });
      }}
    >
      {error && <Alert severity="error">{error}</Alert>}
      {!user && (
        <Alert severity={initialPassword ? "info" : "warning"}>
          {initialPassword
            ? `A senha inicial será: ${initialPassword}`
            : "Configure a senha inicial da empresa antes de cadastrar."}
        </Alert>
      )}
      <Stack component="section" spacing={2} aria-labelledby="user-access-heading">
        <Typography id="user-access-heading" variant="subtitle2" sx={{ fontWeight: 700 }}>
          Dados de acesso
        </Typography>
        <Box
          sx={{
            display: "grid",
            gridTemplateColumns: { xs: "1fr", md: "minmax(0, 1fr) minmax(0, 2fr)" },
            gap: 2,
            alignItems: "start",
          }}
        >
          <TextField
            size="small"
            fullWidth
            label="Login"
            value={login}
            disabled={Boolean(user)}
            onChange={(e) => setLogin(e.target.value)}
            helperText={user ? undefined : "Identificador de acesso, sem espaços."}
            slotProps={{ htmlInput: { maxLength: 100, autoCapitalize: "none" } }}
          />
          <TextField
            size="small"
            fullWidth
            label="Nome"
            value={name}
            onChange={(e) => setName(e.target.value)}
            slotProps={{ htmlInput: { maxLength: 200 } }}
          />
        </Box>
      </Stack>
      <Divider />
      <Stack component="section" spacing={2} aria-labelledby="user-company-heading">
        <Typography id="user-company-heading" variant="subtitle2" sx={{ fontWeight: 700 }}>
          Dados na empresa
        </Typography>
        <Box
          sx={{
            display: "grid",
            gridTemplateColumns: { xs: "1fr", md: "repeat(2, minmax(0, 1fr))" },
            gap: 2,
            alignItems: "start",
          }}
        >
          <TextField
            size="small"
            fullWidth
            label="E-mail na empresa"
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            slotProps={{ htmlInput: { maxLength: 254 } }}
          />
          <TextField
            size="small"
            fullWidth
            select
            label="Perfil"
            value={profileId}
            onChange={(e) => setProfile(e.target.value)}
          >
            <MenuItem value="">Sem perfil</MenuItem>
            {user?.profileId && !profiles.some((p) => p.id === user.profileId) && (
              <MenuItem value={user.profileId}>{user.profileName} (inativo)</MenuItem>
            )}
            {profiles.map((p) => (
              <MenuItem key={p.id} value={p.id}>
                {p.name}
              </MenuItem>
            ))}
          </TextField>
        </Box>
      </Stack>
      {!user && (
        <FormControlLabel
          control={<Switch checked={active} onChange={(_, v) => setActive(v)} />}
          label="Usuário ativo"
        />
      )}
      <Divider />
      <Stack direction="row" spacing={1.5} sx={{ justifyContent: "flex-end" }}>
        <Button type="button" variant="outlined" disabled={busy} onClick={onClose}>
          Cancelar
        </Button>
        <Button
          type="submit"
          variant="contained"
          disabled={
            busy || !name.trim() || !email.trim() || !login.trim() || (!user && !initialPassword)
          }
        >
          Salvar
        </Button>
      </Stack>
    </Stack>
  );
}
