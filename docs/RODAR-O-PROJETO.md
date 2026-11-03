# Como rodar o YallaFinance (guia para agente)

Objetivo: subir o banco restaurado, a API, o Worker e o frontend, e confirmar que estão funcionando.

Este guia assume Windows com PowerShell. Os comandos devem ser executados na raiz deste repositório, salvo indicação contrária.

## 0. Regras

- Não executar comandos de git (commit, push, branch, reset). Quem controla o git é o responsável.
- Não criar nem alterar `global.json`, `.env` com segredos reais, nem arquivos em `src/` sem autorização.
- Não rodar testes de integração. A suíte do projeto é só unitária, e este repositório não a inclui.
- Não aplicar scripts de migração nem carga base (`insert_base.sql`). O banco vem do backup.

## 1. Pré-requisitos

| Item | Versão | Verificação |
|---|---|---|
| .NET SDK | Faixa `11.0.100-rc.2` (conforme `global.json`, com `rollForward: latestPatch`) | `dotnet --list-sdks` deve listar uma versão 11.0.x |
| Node.js | `24.17.0` (arquivo `frontend/.node-version`) | `node -v` |
| npm | Compatível com Node 24 | `npm -v` |
| PostgreSQL | 17 | Servidor acessível com o backup restaurado |

Se o SDK exigido não estiver instalado, instale a versão da faixa indicada. Não edite `global.json` para contornar.

## 2. Banco de dados

1. Restaurar o backup fornecido pela equipe em um banco PostgreSQL 17 vazio.
2. Confirmar a restauração:
   ```powershell
   psql -h <host> -U <usuario> -d <banco> -c "SELECT count(*) FROM core.tenant;"
   ```
   Resultado esperado: 2 (Yalla e Yalla Fleet).
3. Anotar a connection string. Ela será usada na API e no Worker.

Formato da connection string:

```
Host=<host>;Port=5432;Database=<banco>;Username=<usuario>;Password=<senha>
```

## 3. API (WebApi)

Definir a connection string apenas no processo, sem gravar em arquivo:

```powershell
$env:ConnectionStrings__YallaFinance = "Host=localhost;Port=5432;Database=YallaFinance;Username=YallaFinance;Password=<senha>"
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project .\src\YallaFinance.WebApi
```

- Endereço: `http://localhost:5077`.
- Swagger em `http://localhost:5077/swagger` (somente em Development).
- Em Development, o perfil `launchSettings.json` define a porta 5077.

Verificação, em outro terminal:

```powershell
Invoke-RestMethod http://localhost:5077/health
```

Deve responder `Healthy`.

## 4. Worker

Em outro terminal, com a mesma connection string:

```powershell
$env:ConnectionStrings__YallaFinance = "<mesma connection string da API>"
$env:DOTNET_ENVIRONMENT = "Development"
dotnet run --project .\src\YallaFinance.Worker
```

- Não tem porta HTTP.
- Verificação: o console mostra o serviço iniciado, sem exceção. Jobs pendentes começam a ser processados em alguns segundos.
- Não há gravação de evidências em arquivo: nenhuma pasta precisa ser criada.

## 5. Frontend

```powershell
Set-Location .\frontend
Copy-Item .\.env.example .\.env.local
```

Editar `frontend\.env.local` e garantir:

```dotenv
API_URL=http://localhost:5077
```

Instalar e subir:

```powershell
npm ci
npm run dev
```

- Endereço: `http://localhost:3000`.
- Não usar `NEXT_PUBLIC_` para `API_URL`.

Para validar sem subir o servidor de desenvolvimento:

```powershell
npm run lint
npm run build
```

## 6. Verificação de ponta a ponta (manual)

1. Abrir `http://localhost:3000/login`.
2. Entrar com o usuário administrador do backup. A senha não consta deste guia; pedir ao responsável.
3. Confirmar que a página inicial carrega e que o menu aparece.
4. Abrir Configurações e confirmar as abas Empresa, Conexões e Jobs.
5. Abrir Jobs e confirmar que a lista carrega.

Se algum passo falhar, registrar a mensagem exata e o passo onde ocorreu.

## 7. Problemas comuns

| Sintoma | Causa provável | Ação |
|---|---|---|
| `dotnet` não encontra o SDK | Faixa exigida não instalada | Instalar a faixa 11.0.100-rc.2 ou superior. Não alterar `global.json` |
| API falha ao iniciar com erro de conexão | Connection string incorreta ou banco fora do ar | Conferir host, porta, usuário e senha. Testar com `psql` |
| `/health` não responde | API não subiu ou porta diferente | Conferir o console e `launchSettings.json` |
| Frontend exibe erro de API | `API_URL` diferente da porta da API | Ajustar `frontend\.env.local` e reiniciar `npm run dev` |
| Login recusado | Senha incorreta ou usuário inexistente no backup | Confirmar com o responsável |
| Frontend não compila | Dependências desatualizadas | Rodar `npm ci` de novo na pasta `frontend` |
| Worker não processa Jobs | Connection string diferente da API | Usar exatamente a mesma string |

## 8. Encerrar

Parar cada processo com `Ctrl+C` no terminal onde foi iniciado. Não é preciso limpar banco ou arquivos.
