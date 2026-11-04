-- Feature 28, Fase 3: troca obrigatoria de senha no proximo acesso.
-- Usuarios existentes recebem false. Executar somente pelo responsavel pelo executor de scripts (scriptsPS/05-ExecutarScriptsDB.ps1),
-- que controla a transacao; este arquivo nao contem BEGIN/COMMIT.

ALTER TABLE core."user"
    ADD COLUMN must_change_password boolean NOT NULL DEFAULT false;
