"use client";
import { useState, type FormEvent } from "react";
import { Box, Button, Checkbox, FormControlLabel, Stack, Switch, TextField, Typography } from "@mui/material";
import type { FunctionalityOption, ProfileItem } from "@/features/users/types";

export function ProfileEditor({ profile, functionalities, busy, onClose, onSave }: {
  profile: ProfileItem | null;
  functionalities: FunctionalityOption[];
  busy: boolean;
  onClose: () => void;
  onSave: (input: { code: string; name: string; active: boolean; functionalityIds: string[] }) => void;
}) {
  const [code, setCode] = useState(profile?.code ?? "");
  const [name, setName] = useState(profile?.name ?? "");
  const [active, setActive] = useState(true);
  const [selected, setSelected] = useState<string[]>(profile?.functionalityIds ?? []);
  const selectedIds = selected;

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    onSave({ code: code.trim(), name: name.trim(), active, functionalityIds: selectedIds });
  }

  return <Stack component="form" spacing={2.5} sx={{ p: 2.5 }} onSubmit={submit}>
    <Stack direction="row" spacing={2} useFlexGap sx={{ flexWrap: "wrap" }}>
      <TextField size="small" label="Código" value={code} disabled={Boolean(profile)} onChange={(event) => setCode(event.target.value)}
        slotProps={{ htmlInput: { maxLength: 50 } }} sx={{ flex: "1 1 220px" }} />
      <TextField size="small" label="Nome" value={name} onChange={(event) => setName(event.target.value)}
        slotProps={{ htmlInput: { maxLength: 100 } }} sx={{ flex: "2 1 280px" }} />
    </Stack>
    {!profile && <FormControlLabel label="Perfil ativo" control={<Switch checked={active} onChange={(_, value) => setActive(value)} />} />}
    <Box sx={{ border: "1px solid", borderColor: "divider", borderRadius: 1.5, p: 2.5 }}>
      <Stack spacing={1.75}>
        <Box>
          <Typography component="h2" variant="h6">Controle de acesso</Typography>
          <Typography color="text.secondary" variant="body2">Escolha as funcionalidades disponíveis para usuários deste perfil.</Typography>
        </Box>
        {functionalities.map((item) => {
          const checked = selectedIds.includes(item.id);
          return <Box key={item.id} sx={{ alignItems: "center", border: "1px solid", borderColor: "divider", borderRadius: 1, display: "flex", gap: 1.5, p: 1.5 }}>
            <Checkbox checked={checked} disabled={busy} onChange={(event) => setSelected((current) => event.target.checked
              ? [...current, item.id] : current.filter((id) => id !== item.id))} slotProps={{ input: { "aria-label": item.name } }} />
            <Box>
              <Typography sx={{ fontWeight: 600 }}>{item.name}</Typography>
              <Typography color="text.secondary" variant="body2">{item.description}</Typography>
            </Box>
          </Box>;
        })}
      </Stack>
    </Box>
    <Stack direction="row" spacing={1.5} sx={{ justifyContent: "flex-end" }}>
      <Button type="button" variant="outlined" disabled={busy} onClick={onClose}>Cancelar</Button>
      <Button type="submit" variant="contained" disabled={busy || !code.trim() || !name.trim()}>Salvar</Button>
    </Stack>
  </Stack>;
}
