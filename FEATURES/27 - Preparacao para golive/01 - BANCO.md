# 01 — Banco de dados (PostgreSQL)

## Requisitos

- PostgreSQL **17**.
- Banco e usuário com permissão nos schemas `core` e `finance`.
- Acesso de rede da API e do Worker ao banco (porta padrão `5432`).

## Restauração

1. Criar o banco vazio (nome livre, por exemplo `YallaFinance`).
2. Restaurar o backup recebido, com a ferramenta compatível com o formato do arquivo (`pg_restore` ou `psql`).
3. Confirmar a restauração:
   - `SELECT count(*) FROM core.tenant;` deve retornar 2 (Yalla e Yalla Fleet).
   - `SELECT count(*) FROM core."user";` deve retornar pelo menos 1.

## Não fazer

- Não executar `database/insert_base.sql`. O backup já contém os dados.
- Não executar scripts de migração.

## Após a restauração

- Trocar a senha de `admin@yalla.com.br` antes de expor o ambiente. Esse usuário vem do backup.
- Conferir que o registro `Authentication.JwtSigningKey.v1` não existe. Foi removido.

## Conexão

A API e o Worker usam a connection string `ConnectionStrings__YallaFinance`. Ver [02 - API.md](02%20-%20API.md) e [03 - WORKER.md](03%20-%20WORKER.md).
