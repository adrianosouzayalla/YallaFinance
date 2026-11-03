# 02 — API (WebApi)

## Requisitos

- .NET 10 (runtime ASP.NET Core).
- Acesso ao PostgreSQL restaurado. Ver [01 - BANCO.md](01%20-%20BANCO.md).

## Publicação

```powershell
dotnet publish .\src\YallaFinance.WebApi -c Release -o <pasta-de-publicacao>
```

Execução:

```powershell
dotnet <pasta-de-publicacao>\YallaFinance.WebApi.dll
```

## Variáveis de ambiente

| Variável | Valor | Obrigatória |
|---|---|---|
| `ConnectionStrings__YallaFinance` | Connection string do PostgreSQL restaurado | Sim |
| `ASPNETCORE_ENVIRONMENT` | `Production` | Sim |
| `ASPNETCORE_URLS` | Endereço e porta internos, por exemplo `http://+:5077` (TLS fica no proxy) | Sim |

## Verificação

- `GET /health` deve responder saudável.
- Swagger fica desligado em produção.
- Logs saem no console (stdout).

## Particularidades

- **CORS:** não configurar em produção. O navegador não chama a API; só o servidor do Next chama. A lista fixa em `Program.cs` (localhost e duas faixas de IP local) vale só para desenvolvimento.
- **HTTPS:** fora de Development, a API redireciona para HTTPS. Ver [05 - PROXY E HTTPS.md](05%20-%20PROXY%20E%20HTTPS.md).
- **Encaminhamento de cabeçalhos:** a API não configura `UseForwardedHeaders`.
- **Chave do Bearer de integração:** gerada a cada start. O `appsettings` não define chave válida para produção, e não há segredo de produção nele.
- **Login interativo:** usa sessão no banco. Não depende da chave JWT.
