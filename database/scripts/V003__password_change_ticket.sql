-- Feature 28, Fase 4: tickets da troca obrigatoria de senha compartilhados entre instancias da API.
-- Tabela tecnica, sem auditoria por decisao aprovada pelo responsavel (exceção registrada na Fase 4).
-- Guarda somente o hash do ticket. Executar pelo executor de scripts (scriptsPS/05-ExecutarScriptsDB.ps1), sem BEGIN/COMMIT.

CREATE TABLE core.password_change_ticket (
    id uuid NOT NULL,
    ticket_hash text NOT NULL,
    login text NOT NULL,
    tenant_id uuid NOT NULL,
    created_at timestamp with time zone NOT NULL,
    expires_at timestamp with time zone NOT NULL,
    used_at timestamp with time zone,
    CONSTRAINT pk_password_change_ticket PRIMARY KEY (id),
    CONSTRAINT uq_password_change_ticket_hash UNIQUE (ticket_hash),
    CONSTRAINT ck_password_change_ticket_expiration CHECK (expires_at > created_at),
    CONSTRAINT fk_password_change_ticket_tenant FOREIGN KEY (tenant_id) REFERENCES core.tenant (id) ON DELETE RESTRICT ON UPDATE RESTRICT
);

CREATE INDEX ix_password_change_ticket_expires_at ON core.password_change_ticket (expires_at);
