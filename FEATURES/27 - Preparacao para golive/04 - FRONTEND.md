# 04 — Frontend (Next.js)

## Requisitos

- Node.js **24.17.0** (`frontend/.node-version`).
- Acesso do servidor Next à API (`API_URL`).

## Build e execução

```bash
cd frontend
npm ci
npm run build
npm run start
```

- Porta padrão do `next start`: 3000. Para outra porta: `npm run start -- -p <porta>`.

## Variáveis de ambiente (servidor)

| Variável | Valor | Obrigatória |
|---|---|---|
| `API_URL` | Endereço da API acessível pelo servidor Next, por exemplo `https://api.<dominio>` ou endereço interno | Sim |

- `API_URL` é lida só no servidor. Não usar prefixo `NEXT_PUBLIC_`.
- Sem `API_URL`, o servidor não sobe as chamadas à API.
- Reiniciar o frontend após alterar variáveis.

## Sessão

- O cookie `yalla-finance-session` é `HttpOnly` e `Secure` em produção. O acesso do navegador precisa ser HTTPS.
- Não há store de sessão em disco. Várias instâncias do Next funcionam, desde que usem a mesma API e o mesmo banco.

## Particularidades

- **Origem das mutations:** a validação de origem compara o protocolo visto pelo Next. Atrás de proxy que termina TLS, ver [05 - PROXY E HTTPS.md](05%20-%20PROXY%20E%20HTTPS.md).
- **Checkout público:** rota `/pay/[token]`, sem login. Depende do domínio público da empresa (`PublicOrigin` do tenant), configurado no banco.
