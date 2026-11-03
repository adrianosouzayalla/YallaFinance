# 05 — Proxy reverso e HTTPS

## Desenho

- Proxy público com certificado confiável pelos navegadores, na frente do frontend.
- Frontend (Next, porta 3000) recebe o tráfego do proxy.
- API fica em rede interna. Só o servidor Next precisa acessá-la (`API_URL`).

## Cabeçalhos a encaminhar

| Cabeçalho | Valor |
|---|---|
| `Host` | Domínio público |
| `X-Forwarded-Proto` | `https` |
| `X-Forwarded-For` | IP do cliente |

## Timeouts

- Chamadas aos provedores têm limite de 30 a 60 segundos. O proxy deve aguardar pelo menos esse tempo.

## Pendências que bloqueiam o go-live atrás de proxy

- **Origem pública do Next:** se o proxy termina TLS e encaminha HTTP, as mutations podem ser recusadas, porque a origem HTTPS do navegador não corresponde ao protocolo visto pelo Next. Solução: configurar e validar uma origem pública confiável na fronteira do Next. Ainda não existe no código.
- **Encaminhamento na API:** a API não configura `UseForwardedHeaders`. Hoje ela não lê `X-Forwarded-Proto`. Validar o redirecionamento HTTPS antes de publicar.

## Verificação

- Login pelo domínio público retorna cookie `Secure` e `HttpOnly`.
- `GET https://<api-interna>/health` responde saudável a partir do servidor Next.
