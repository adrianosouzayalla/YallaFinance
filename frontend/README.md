# Yalla Finance — frontend

O contrato vigente é [ARQUITETURA_PADRAO_FRONTEND_YALLA_FINANCE_FINAL.md](docs/ARQUITETURA_PADRAO_FRONTEND_YALLA_FINANCE_FINAL.md). Os desenhos anteriores são históricos. O [planejamento](docs/PLANEJAMENTO_ADEQUACAO_ARQUITETURA_FRONTEND.md) e o [registro de validação](docs/VALIDACAO_MIGRACAO_FRONTEND.md) descrevem a migração e suas pendências.

## Organização

| Pasta                     | Responsabilidade                                                      |
| ------------------------- | --------------------------------------------------------------------- |
| `src/app/`                | Rotas, layouts, composição e Route Handlers explícitos                |
| `src/features/auth/`      | Login, sessão pública, validação e credenciais exclusivas do servidor |
| `src/features/dashboard/` | Indicadores e suas consultas                                          |
| `src/features/debts/`     | Lista, filtros, seleção, detalhe e regras de status                   |
| `src/features/charges/`   | Etapas da cobrança, regras, formulários, rascunho e mutations         |
| `src/features/customers/` | Consultas de clientes e movimentações de frota                        |
| `src/features/catalogs/`  | Categorias e formas de pagamento                                      |
| `src/components/finance/` | Shell, menu, cabeçalho, navegação e identificação da sessão           |
| `src/components/ui/`      | Componentes visuais reutilizados entre features                       |
| `src/providers/`          | Providers globais de tema, toast e TanStack Query                     |
| `src/services/api/`       | Transporte Axios e normalização de erros; sem endpoints de negócio    |
| `src/theme/`, `public/`   | Tema, tokens e recursos visuais                                       |

Cada feature contém apenas as pastas de que precisa: `components`, `api`, `hooks`, `types`, `schemas`, `context` e/ou `server`. Testes ficam próximos do código que validam.

O [diagrama de autenticação por cookie e BFF](docs/FLUXO_AUTENTICACAO_COOKIE_BFF_YALLA_FINANCE.md) mostra o fluxo ponta a ponta de login, chamadas autenticadas, proteção de origem, logout e expiração.

A [tabela compartilhada com seleção entre páginas](docs/TABELA_SELECAO_PAGINADA.md) é o padrão obrigatório para todas as telas operacionais com tabelas, conforme decisão aprovada em 03/10/2026. A documentação descreve o componente genérico, o hook de seleção e como usá-los em outras telas. Cada feature fornece suas colunas e regras; necessidades adicionais devem evoluir o componente compartilhado. A tela de débitos preserva a seleção ao paginar ou ordenar.

Exemplo de rastreabilidade: `/debts` → `DebtsPageClient` → `useDebtSearch` → `debts.client.ts` → Route Handler `/api/debts` → `debts.server.ts` → API .NET `GET /api/debts`. Login usa Server Action → `auth.server.ts` diretamente. A API .NET continua responsável pela autorização e pelas decisões financeiras/ERP.

## Execução local

Os [campos compartilhados de moeda e data](docs/CAMPOS_MOEDA_DATA.md) documentam `CurrencyField`, `DateField`, utilitários de formatação e integração com React Hook Form. Os componentes permanecem genéricos; validações e contratos de negócio pertencem às features.

Usar Node **24.17.0** e as versões de Next/React do lockfile.

```powershell
cd frontend
npm.cmd ci
$env:API_URL = 'http://localhost:5000'
npm.cmd run dev
```

Alternativamente, configurar `frontend/.env.local` conforme `.env.example`. O script `scriptsPS/suporte/Start-Frontend.ps1`, na raiz do repositório, configura a URL local.

| Variável            | Uso                                                                                                           |
| ------------------- | ------------------------------------------------------------------------------------------------------------- |
| `API_URL`           | Obrigatória para o servidor Next; origem privada da API .NET, lida somente no servidor                         |
| `NODE_ENV`          | Gerenciada pelos comandos Next; não precisa ser configurada manualmente                                       |

O browser chama a mesma origem do Next. O cookie `HttpOnly` contém o token da sessão da API; o JavaScript do navegador não pode lê-lo e o Next o encaminha como Bearer server-to-server. Não existe store de sessão em disco nem variável `SESSION_STORE_DIR` no runtime. `X-Yalla-Context` identifica a versão pública da sessão para impedir operações de uma aba antiga com credenciais de outro login; ele não substitui a autenticação nem determina o tenant autorizado. Em produção, o cookie `Secure` exige acesso por HTTPS.

`API_URL` é lida pelo processo Next. Se o browser acessar o frontend por um IP da rede, `localhost` nessa variável ainda aponta para o computador onde Next está rodando. Use o endereço alcançável pelo Next quando a API estiver em outro host ou container.

A validação de origem dos POSTs usa o protocolo e o `Host` vistos pelo Next, não o endereço de bind do processo (`0.0.0.0`). O runtime não lê `APP_ORIGIN`; proxies que terminam HTTPS e encaminham HTTP precisam de suporte explícito à origem pública antes de serem usados para mutations autenticadas.

**Hospedagem de produção ainda não definida:** o store atual suporta Node com disco privado persistente. Várias instâncias precisam compartilhar esse diretório; serverless/disco efêmero exige outro store persistente. Definir o ambiente e validar esse requisito antes da publicação. Restringir o acesso ao diretório de sessões à conta do servidor e excluí-lo de distribuição pública/backups sem proteção.

## Verificação

```powershell
npm.cmd run lint
npx.cmd tsc --noEmit --incremental false
npm.cmd test -- --pool=threads --maxWorkers=1
npm.cmd run build
```

## Consulta de cobrança — mock backend autorizado

A criação já retorna os dados necessários à tela de sucesso. `useCreateCharge` coloca essa resposta no cache; a tela usa esse resultado imediatamente, sem exigir outro GET. Em recarga ou nova aba, `useCharge` consulta `/api/charges/[id]` no Next, encaminhado ao `GET /api/charges/{id}` da API .NET. O histórico de cobranças em `sessionStorage` foi removido.

Por autorização do usuário, a consulta usa um **mock em memória no backend**, com anotação TODO em `src/YallaFinance.Infrastructure/Mockup/Repositories/InMemoryChargeRepository.cs` na raiz do projeto. Ele registra a resposta real da criação, incluindo resultado financeiro e entrega, e consulta somente o tenant autenticado. Não inventa sucesso nem identificadores externos e não altera a chamada financeira existente.

**Limite do mock:** reiniciar a API apaga as cobranças registradas; instâncias distintas não compartilham os dados. Persistência de produção fica para uma implementação futura. O backend pode continuar chamando o provider financeiro configurado; os testes desta migração usam integração financeira simulada.
