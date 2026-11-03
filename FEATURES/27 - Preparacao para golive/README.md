# Feature 27 — Preparação para go-live

Objetivo: dar à equipe de infra o que cada frente precisa para subir o YallaFinance.

A infra recebe:
1. O **backup** do banco, com os dados já carregados.
2. Os arquivos desta pasta, um por frente.

## Ordem de subida

| # | Frente | Arquivo |
|---|---|---|
| 1 | PostgreSQL (restauração do backup) | [01 - BANCO.md](01%20-%20BANCO.md) |
| 2 | API (WebApi) | [02 - API.md](02%20-%20API.md) |
| 3 | Worker | [03 - WORKER.md](03%20-%20WORKER.md) |
| 4 | Frontend (Next.js) | [04 - FRONTEND.md](04%20-%20FRONTEND.md) |
| 5 | Proxy e HTTPS | [05 - PROXY E HTTPS.md](05%20-%20PROXY%20E%20HTTPS.md) |

## Pendências que bloqueiam ou exigem decisão antes do go-live

- **Origem pública atrás do proxy:** mutations do frontend podem ser recusadas em HTTPS terminado no proxy. Ver [05 - PROXY E HTTPS.md](05%20-%20PROXY%20E%20HTTPS.md).
- **Encaminhamento de cabeçalhos:** a API não lê `X-Forwarded-Proto`. Ver [05 - PROXY E HTTPS.md](05%20-%20PROXY%20E%20HTTPS.md).
- **Conta admin do backup** (`admin@yalla.com.br`): senha trocada no backup (concluído). Ver [01 - BANCO.md](01%20-%20BANCO.md).
- **Diretório de evidências do Nibo:** não ativar em produção sem decisão. Ver [03 - WORKER.md](03%20-%20WORKER.md).

## Atenção, sem bloqueio

- CORS: não configurar em produção. O navegador não chama a API; só o servidor do Next chama. A lista fixa no código vale só para desenvolvimento.
- Logs apenas em console (API e Worker). Coleta de logs fica com a infra.
- Chave do Bearer de integração é gerada a cada start da API. Reiniciar invalida tokens de integração. Não afeta o login.
- Consulta de frota (movimentações) lê JSON em memória. Não afeta o restante.
