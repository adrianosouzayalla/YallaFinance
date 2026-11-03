# YallaFinance

Solução YallaFinance com API .NET, Worker de Jobs e sincronização, frontend Next.js, autenticação e fluxo de cobranças e pagamentos com integrações financeiras (Nibo, Asaas e Sicoob).

## Estrutura

- `src/YallaFinance.Domain`: tipos de domínio e enums, sem dependências.
- `src/YallaFinance.Application`: casos de uso, validação e contratos (repositórios e serviços).
- `src/YallaFinance.Infrastructure`: repositórios PostgreSQL, providers de integração e serviços de pagamento.
- `src/YallaFinance.WebApi`: API HTTP (controllers, autenticação por sessão, Swagger em Development).
- `src/YallaFinance.Worker`: processo separado com dois serviços em segundo plano: execução de Jobs (com retry automático) e sincronização de recebimentos Pix.
- `frontend`: aplicação Next.js (BFF com sessão em cookie HttpOnly e telas operacionais e checkout público).
- `database`: esquema PostgreSQL, carga base e dicionário de dados.
- `tests`: testes unitários, sem acesso a banco real.

## Pré-requisitos

- .NET SDK compatível com o `global.json` da raiz (o arquivo fixa a faixa `11.0.100-rc.2`, com `rollForward: latestPatch`). Os projetos têm como alvo `net10.0`.
- Node.js 24.17.0 (`frontend/.node-version`).
- npm compatível.
- Docker Desktop, para o PostgreSQL local.

## Backend

```powershell
dotnet restore .\YallaFinance.sln
dotnet build .\YallaFinance.sln --configuration Release
dotnet test .\YallaFinance.sln --configuration Release
```

A solução mantém somente a suíte unitária. **Testes integrados são proibidos como regra core em todo o projeto**, conforme [AGENTS.md](AGENTS.md). Os testes não precisam de PostgreSQL, Docker, `.env` ou credenciais reais. A política detalhada está em [tests/README.md](tests/README.md).

## PostgreSQL

```powershell
Copy-Item .\.env.example .\.env
# Defina POSTGRES_PASSWORD no .env
docker compose up -d postgres
```

O `docker-compose.yml` sobe somente o PostgreSQL (`postgres:17-alpine`), com volume persistente `yallafinance-postgres-data`. API, Worker e frontend rodam fora do container.

| Variável | Obrigatória | Uso / padrão |
| --- | --- | --- |
| `POSTGRES_DB` | Não | Nome do banco; padrão `YallaFinance`. |
| `POSTGRES_USER` | Não | Usuário; padrão `YallaFinance`. |
| `POSTGRES_PASSWORD` | Sim | Senha do PostgreSQL. O compose não sobe sem ela. |
| `POSTGRES_PORT` | Não | Porta publicada no host; padrão `5432`. |
| `POSTGRES_CONTAINER_NAME` | Não | Nome do container; padrão `yallafinance-postgres`. |

### Esquema e carga base

O banco de ambiente é restaurado a partir do **backup** fornecido pela equipe, que já contém os dados. Não é preciso aplicar carga base nem scripts de migração para subir o ambiente.

- `database/scripts/V001__baseline.sql` é o esquema de referência (dump do PostgreSQL 17), com os schemas `core` e `finance`.
- `database/insert_base.sql` é somente para criar um banco do zero. Não é usado para subir o ambiente com o backup.

## API (WebApi)

A connection string do banco não fica em `appsettings`. Defina a variável de ambiente antes de iniciar:

```powershell
$env:ConnectionStrings__YallaFinance = "Host=localhost;Port=5432;Database=YallaFinance;Username=YallaFinance;Password=<senha>"
dotnet run --project .\src\YallaFinance.WebApi
```

- Endereço de desenvolvimento: `http://localhost:5077` (perfil em `Properties/launchSettings.json`).
- Swagger fica ativo somente em Development.
- Fora de Development, a API aplica redirecionamento para HTTPS (`UseHttpsRedirection`).
- Logs: console (Serilog). Não há gravação em arquivo.

## Worker

```powershell
$env:ConnectionStrings__YallaFinance = "<mesma connection string da API>"
dotnet run --project .\src\YallaFinance.Worker
```

- Não expõe porta HTTP.
- `Jobs:BatchSize` (padrão 10) e `Jobs:PollingIntervalSeconds` (padrão 5) controlam o consumo de Jobs.
- Sincronização de recebimentos Pix roda a cada 15 segundos, com janela padrão de 7 dias por conexão.
- Retry automático de Jobs depende da configuração `job_retry_limit` na aba **Jobs** das configurações da empresa. Sem configuração, nenhum Job é retentado.
- Vários Workers podem rodar ao mesmo tempo: o consumo de Jobs usa `FOR UPDATE SKIP LOCKED`.

## Frontend

```powershell
Set-Location .\frontend
Copy-Item .\.env.example .\.env.local
npm.cmd ci
npm.cmd run lint
npm.cmd run build
npm.cmd run dev
```

O frontend local usa `http://localhost:3000`. Copie o arquivo de exemplo somente na configuração inicial; preserve um `.env.local` que já esteja configurado. Detalhes do frontend em [frontend/README.md](frontend/README.md).

## Configuração do servidor Next

As variáveis abaixo pertencem ao **servidor Next**, não ao browser nem à API .NET. Em desenvolvimento ficam em `frontend/.env.local`; em produção, nas variáveis de ambiente da hospedagem do frontend.

| Variável | Responsabilidade | Quando mudar |
| --- | --- | --- |
| `API_URL` | Obrigatória. Endereço da API .NET, acessado server-to-server pelo Next. | Quando o endereço ou a porta do backend mudar. |
| `NODE_ENV` | Controlada pelos comandos do Next. Não configurar manualmente. | `next dev` usa desenvolvimento; `next start` usa produção. |

Após alterar variáveis, reinicie o frontend. `API_URL` não deve ser exposta no bundle do navegador (sem prefixo `NEXT_PUBLIC_`).

### Desenvolvimento

```dotenv
API_URL=http://localhost:5077
```

`5077` é a porta do perfil de desenvolvimento da API. Se a API rodar em outra porta, ajuste `API_URL` no `.env.local`.

A validação de origem usa o protocolo e o `Host` da requisição recebida pelo Next, incluindo a porta acessada pelo navegador. Trocar a porta do frontend não exige reconfigurar o backend por causa dessa validação.

### Produção

```dotenv
API_URL=https://api.seudominio.com.br
```

`API_URL` precisa ser acessível pelo servidor Next; pode ser um endereço interno da infraestrutura. O runtime ainda não possui uma configuração de origem pública para proxies HTTPS: se o proxy terminar TLS e encaminhar HTTP ao Next, as mutations podem ser recusadas porque a origem HTTPS do browser não corresponde ao protocolo visto pelo Next.

A proteção de origem impede que outro site execute operações com o cookie de sessão do usuário (CSRF). Ela acontece no Next.

### Sessão

O login cria uma sessão no banco (`core.user_session`). O token fica no cookie `yalla-finance-session` (HttpOnly, SameSite=Lax, path `/`) e é encaminhado pelo servidor Next nas chamadas autenticadas. Em produção, o cookie é `Secure`, então o acesso do navegador precisa ser HTTPS. O Next não mantém store de sessão em disco nem em memória: várias instâncias do Next funcionam, desde que apontem para a mesma API e o mesmo banco.

## Particularidades antes de produção

Estes pontos refletem a implementação atual. Publicar com as variáveis configuradas não resolve nenhum deles:

- **HTTPS público:** o cookie de sessão é `Secure` em produção. Os usuários devem acessar o frontend por HTTPS.
- **Conta administrativa da carga base:** `CatalogAuthenticationService` valida o usuário no PostgreSQL e confere o hash da senha. A carga base cria o usuário `admin@yalla.com.br`. Para testes, essa conta é mantida. Antes de produção, troque a senha ou desative a conta.
- **Chave JWT:** o `appsettings` contém uma chave de desenvolvimento, mas `DependencyInjection` gera uma nova chave aleatória a cada inicialização. Ela vale somente para o Bearer de integração. O login interativo usa sessão no banco. Reiniciar a API invalida os tokens de integração e exige nova emissão. Instâncias distintas usam chaves diferentes. Definir o mecanismo de assinatura e rotação é pendência de produção.
- **Dados mock:** `Mockup/Data` (JSON) é carregado na inicialização da infraestrutura. Hoje só as movimentações de frota usam esse dado, em repositório em memória. Os demais dados vêm do PostgreSQL. Os arquivos mock são reservados para débito e cobrança avulsos, previstos para outra fase.
- **Proxy e encaminhamento:** fora de Development, a API redireciona para HTTPS. A API não configura `UseForwardedHeaders`, então não lê `X-Forwarded-Proto` nem `X-Forwarded-For` do proxy. Validar esse caminho antes de publicar atrás de proxy.
- **CORS:** não configurar em produção. O navegador chama as rotas do próprio Next, e o Next chama a API no servidor. A lista fixa no código (localhost e duas faixas de IP local) vale só para desenvolvimento.

## Documentação

- [docs/ARQUITETURA.md](docs/ARQUITETURA.md): visão ponta a ponta (camadas, cobrança, pagamento, recebimento, Jobs, banco e pendências). Versão em PDF: [docs/ARQUITETURA.pdf](docs/ARQUITETURA.pdf).
- [frontend/docs/PLANEJAMENTO_ADEQUACAO_ARQUITETURA_FRONTEND.md](frontend/docs/PLANEJAMENTO_ADEQUACAO_ARQUITETURA_FRONTEND.md) e [frontend/docs/VALIDACAO_MIGRACAO_FRONTEND.md](frontend/docs/VALIDACAO_MIGRACAO_FRONTEND.md): status e evidências da adequação do frontend.
