# Validação da migração arquitetural — Yalla Finance

Plano: `PLANEJAMENTO_ADEQUACAO_ARQUITETURA_FRONTEND.md`.
Contrato: `ARQUITETURA_PADRAO_FRONTEND_YALLA_FINANCE_FINAL.md`.
Início: 01/10/2026. Branch inicial: `main`, sem alterações de código na coleta inicial.

**Atualização da fase 7 — 01/10/2026:** consulta por ID implementada e validada com
mock em memória no backend, conforme autorização do usuário para não persistir
em banco. O sucesso imediato usa a resposta do POST; recarga/nova aba usam GET.
Fallback de cobrança em sessionStorage removido. Os registros da referência
inicial abaixo descrevem o comportamento anterior, não o estado atual.

### Correção após teste local — origem do POST

O usuário reportou `Origem da operação inválida.` ao gerar cobrança acessando
`http://localhost:3000`. Reproduzido no servidor local sem cookies, sem envio
financeiro: Origin localhost retornava 403 INVALID_ORIGIN, mas Origin 0.0.0.0
passava a validação. O script inicia Next com `--hostname 0.0.0.0`; Next usa
esse bind em `request.url`, que a nova validação comparava incorretamente
com o Origin do navegador.

Corrigido `session-boundary.ts`: quando APP_ORIGIN não está configurado, deriva
a origem do protocolo da requisição e do header Host recebido. Atrás de proxy,
APP_ORIGIN continua prevalecendo. Origin ausente, divergente ou Sec-Fetch-Site
cross-site continuam bloqueados. Não houve mudança no payload ou chamada ERP.

Teste de regressão reproduziu 403 antes da correção e passou depois. Os 8 testes
da fronteira passaram, incluindo origem pública explícita, rejeição cross-site,
sessão e idempotência. A prova HTTP local após a correção retorna 401 para
localhost sem cookie (origem aceita, autenticação exigida) e 403 para origem
divergente. Nenhuma cobrança real foi criada na verificação.

Verificação após a correção: lint e TypeScript aprovados; suíte completa com
23 arquivos / 74 testes aprovada, exit 0 (72,36 s). O servidor de desenvolvimento
ativo recarregou a alteração sem reinício; build não foi repetido nesta correção.

## Referência inicial — Fase 0

| Verificação                       | Comando em frontend/                                                | Resultado                                                            |
| --------------------------------- | ------------------------------------------------------------------- | -------------------------------------------------------------------- |
| Lint                              | `npm.cmd run lint`                                                  | PASS: exit 0                                                         |
| TypeScript                        | `npx.cmd tsc --noEmit --incremental false`                          | PASS: exit 0                                                         |
| Suíte completa, workers limitados | `npm.cmd test -- --maxWorkers=1`                                    | PASS: 13 arquivos, 31 testes, exit 0; 82,07 s                        |
| Build                             | `npm.cmd run build` com `NEXT_PUBLIC_API_URL=http://localhost:5000` | PASS: exit 0; Next 16.3.5, compilação e geração das rotas concluídas |

Execução diagnóstica `npm.cmd test -- --pool=threads --maxWorkers=1 --reporter=verbose`
também aprovada: 13 arquivos, 31 testes, exit 0; 41,60 s. A referência acima
usa o pool padrão com `maxWorkers=1`, sem alterar configuração ou produto.
O timeout observado anteriormente não se repetiu nessa execução limitada;
isso não demonstra aprovação da execução paralela irrestrita.

Nenhum POST financeiro real foi enviado durante a coleta. O build usou URL
local explícita apenas para satisfazer a configuração; não é evidência de
login real, disponibilidade da API ou integração com Nibo.

## Inventário HTTP da referência inicial

Na referência inicial, as URLs eram da API .NET. Ainda não existia fronteira Next para as consultas
autenticadas do navegador. O cliente compartilhado é `src/services/api/httpClient.ts`;
as chamadas financeiras estão em `src/features/finance/apiFinanceRepository.ts`.

| Operação            | Método e endpoint                                      | Entrada/observação                                                                               |
| ------------------- | ------------------------------------------------------ | ------------------------------------------------------------------------------------------------ |
| Login               | POST `/api/auth/login`                                 | email, password e tenantId opcional; seleção de empresa quando retorna opções sem token          |
| Sessão              | GET `/api/auth/session`                                | Bearer; usado pelo SessionGuard                                                                  |
| Logout backend      | POST `/api/auth/logout`                                | Endpoint existente; logout atual do frontend apenas remove cookie e redireciona                  |
| Dashboard           | GET `/api/dashboard/summary`                           | Quatro métricas, count/amount                                                                    |
| Débitos             | GET `/api/debts`                                       | page, pageSize, sortDirection; status técnico, categories, startDate, endDate e sortBy opcionais |
| Detalhe             | GET `/api/debts/{id}`                                  | ID codificado; 404 vira null no repository                                                       |
| Categorias          | GET `/api/categories`                                  | Reutilizadas para traduzir categoryId em nome                                                    |
| Clientes            | GET `/api/customers?search=...`                        | Termo codificado                                                                                 |
| Movimentações       | GET `/api/customers/{id}/movements?occurrenceDate=...` | ID codificado; data da ocorrência                                                                |
| Cobrança            | POST `/api/charges`                                    | Header `Idempotency-Key`; corpo descrito abaixo                                                  |

Confirmado no código dos controllers de auth, dashboard, debts, catalog,
tenants/customers e charges. A autorização continua pertencendo à API .NET.
`GET /api/charges/{id}` não está exposto no ChargesController atual.

### Corpo de criação preservado

`source`, `customerId`, `debtIds`, `amount`, `categoryId`, `occurrenceDate`,
`descriptionDetail`, `dueDate`, `customerNote`, `selectedMovementId`,
`paymentMethod`, `paymentMethodId`. O tenant não é enviado como autoridade
no corpo; a API o obtém do contexto autenticado.

Selecionados: categoria/data/descrição do primeiro débito consultado,
IDs selecionados, valor do draft, pagamento do draft e selectedMovementId null.
O fluxo atual usa o primeiro item do catálogo para paymentMethodId nesse caso.
Avulsa: campos do draft e movimentação selecionada, debtIds vazio, paymentMethod Pix.
Esses comportamentos não devem mudar silenciosamente durante reorganização.

Chave de idempotência: gerada por `chargeFlow.ts`, com cliente e campos do
draft, IDs de débito únicos e ordenados, moeda arredondada e encoding dos
valores. Não substituir o algoritmo por nova identidade durante migração estrutural.

### Respostas e erros existentes

- Corpo de sucesso retornado diretamente; 204 retorna undefined.
- ApiError contém code, message e status; extrai detail e code/extensions.code.
- 401 autenticado no navegador apaga cookie e vai para login com session-expired.
- 403 autenticado no navegador redireciona para `/not-found`.
- Integração Rejected/Indeterminate após HTTP 200 lança erro e não retorna a
  cobrança ao provider; correção intencional prevista na fase 6.
- Histórico de cobrança depende de sessionStorage; migração autoritativa
  depende da fase 7 e do backend.

## Matriz operacional a comparar antes/depois

Estado inicial abaixo registra evidência automatizada ou inspeção; não
representa smoke manual concluído. Nenhum resultado manual deve ser inferido
do build ou de um teste com repository simulado.

| Fluxo                   | Resultado esperado preservado                                               | Evidência inicial                                                             |
| ----------------------- | --------------------------------------------------------------------------- | ----------------------------------------------------------------------------- |
| Entrada `/`             | Redirecionar para login                                                     | Teste page.test.tsx aprovado                                                  |
| Login/empresa           | Seleção de empresa quando necessário; navegação para home                   | Inspeção; smoke real pendente                                                 |
| Shell e menu            | Mesmas entradas, breadcrumbs, sessão e responsividade                       | Testes de header/sidebar/session/navigation aprovados; visual manual pendente |
| Dashboard               | Quatro cards e URLs com status técnicos                                     | HomePageClient.test.tsx aprovado                                              |
| Filtros                 | Open/InCollection/Paid/Canceled; rejeitar status inválido; limpar recupera  | DebtsPageClient.test.tsx e DebtStatus.test.tsx aprovados                      |
| Paginação/detalhe       | Mesmos parâmetros e dados autorizados                                       | Inspeção; completar caracterização antes de mudar consultas                   |
| Seleção                 | Débitos abertos de um cliente/tenant; não misturar identidades              | chargeCustomer.test.ts aprovado                                               |
| Avulsa                  | Selecionar explicitamente entre múltiplas movimentações/formas de pagamento | NewChargeClient.test.tsx aprovado                                             |
| Revisão                 | Preservar nome da forma de pagamento selecionada                            | ChargeReviewClient.test.tsx aprovado                                          |
| Etapas                  | Quatro estágios e navegação coerente                                        | ChargeStepper.test.tsx aprovado; recarregamento/manual pendente               |
| Confirmação             | Não duplicar envio por duplo clique; manter payload e chave                 | Inspeção do guard submitted/pending; teste de interação a complementar        |
| Sucesso/PIX/entrega     | Exibir dados, copiar PIX e registrar entrega                                | Inspeção; smoke e testes pertinentes pendentes                                |
| Erro parcial            | Atual erro genérico; melhoria preservará ID/estado da cobrança              | Mudança intencional da fase 6, teste a criar                                  |
| Logout/troca de usuário | Fluxo atual remove cookie; isolamento será corrigido                        | Inspeção; testes previstos na fase 3                                          |

Datas de negócio usam America/Sao_Paulo; preservar comparação e mensagens
das regras atuais. Validação de seleção/avulsa e identidade de cliente/tenant
deve permanecer independente das bibliotecas de formulário e cache.

## Entrega e progresso — 01/10/2026

Execução na `main`, autorizada pelo usuário, sem commit, staging, merge ou publicação.
Rotas de páginas preservadas. As novas rotas `app/api/` são fronteiras explícitas para endpoints .NET existentes.

| Fase | Estado                                               | Limite                                                          |
| ---- | ---------------------------------------------------- | --------------------------------------------------------------- |
| 0    | Referência e caracterização automatizada registradas | Não houve smoke inicial contra a API real                       |
| 1    | Implementada                                         | Componentes/regras/testes movidos; tipos preservados            |
| 2    | Implementada                                         | Contratos HTTP e idempotência preservados                       |
| 3    | Implementada no ambiente Node                        | Produção depende da definição da hospedagem/store               |
| 4    | Implementada                                         | Cache por tenant e versão de sessão; falhas recuperáveis        |
| 5    | Implementada                                         | RHF/Zod reutilizam as regras existentes                         |
| 6    | Implementada                                         | Rascunho restrito às cobranças; resultados parciais preservados |
| 7    | Consulta implementada com mock backend autorizado   | Sem persistência durável; reiniciar a API apaga os registros     |
| 8    | Limpeza, checks e smoke executados                   | Fallback removido; limites de produção registrados              |

### Organização resultante

- `app/`: somente páginas/layouts, composição e Route Handlers explícitos.
- Features: `auth`, `dashboard`, `debts`, `charges`, `customers` e `catalogs`.
- `components/finance`: shell, navegação, cabeçalho e resumo da sessão.
- `components/ui`: LoadingState, PrototypePanel e SummaryCard compartilhados.
- `services/api`: Axios browser/server, opções comuns e ApiError.
- `providers`: tema, toast e QueryProvider; SessionProvider expõe somente tenant/contexto públicos.
- APIs autenticadas separadas em `*.client.ts` e `*.server.ts`.
- `FinanceProvider`, `FinanceProviderImpl`, repository antigo, `features/finance/types.ts`, transporte antigo e reexports de transição removidos depois de migrar consumidores.
- Fases 1–6 não mudaram o backend. A fase 7 acrescentou consulta autorizada, repositório mock em memória e registro da entrega; sem alteração de banco ou de decisões ERP.

A consulta de débitos da tela usa `useDebtSearch` para preservar o momento de aplicação dos filtros e a recuperação da URL. A consulta de até 1.000 débitos existente foi mantida somente nas etapas que precisam validar seleção/duplicidade, com cache e erro recuperável. Dashboard, lista, detalhe e sucesso não dependem desse carregamento global.

### Sessão e hospedagem

O cookie `HttpOnly` contém o token de sessão retornado pela API; não existe store de sessão em disco nem `SESSION_STORE_DIR` no runtime. O Next lê o cookie e encaminha o token como Bearer em chamadas servidor a servidor. `API_URL` é obrigatória para o servidor Next. O runtime não lê `APP_ORIGIN`; proxy que termina HTTPS precisa de suporte explícito à origem pública antes de encaminhar mutations autenticadas.

Em produção, o cookie usa `Secure`, então o acesso dos usuários precisa ser HTTPS. O Next não depende de diretório de sessão compartilhado entre instâncias. A assinatura e a validade dos tokens emitidos pela API continuam sendo uma pendência separada, registrada no README da solução. Cookie legado exige novo login após a migração de segurança.

Cada chamada protegida do browser envia a versão pública da sessão em
`X-Yalla-Context`. A fronteira compara com a sessão vigente lida do cookie antes de chamar
a API. Contexto antigo retorna `409 SESSION_CONTEXT_CHANGED` e atualiza a página;
não usa credenciais de um novo login para enviar um draft da sessão anterior.
O header não é encaminhado ao backend e não concede autoridade sobre tenant.

### Formulários e comportamento financeiro

RHF/Zod reutilizam regras puras, máscaras, mensagens e datas America/Sao_Paulo.
O login conserva valores padrão e o reset de senha após resposta/seleção de empresa
que já ocorria no formulário React anterior; não altera esse comportamento durante a refatoração.
Confirmação não possui campos editáveis e mantém sua validação de ação existente.

Corpo de criação e algoritmo da chave de idempotência preservados, mutations
sem retry automático. `Completed`, `Rejected` e `Indeterminate` não são convertidos
em DebtStatus. Erro parcial agora conserva ID, dados e resultado recebido.
O frontend não decide recriação de cliente Nibo, conflito de recebível ou recuperação ERP.

Resultados são gravados primeiro no cache da sessão. Falha/quota de storage
não converte uma cobrança concluída em falha financeira nem descarta o ID.
Cobranças não são gravadas em sessionStorage: recarga ou nova aba consultam
o backend, preservando os dados enquanto a mesma API estiver em execução.

### Revisão final e correções verificadas

Uma revisão independente, somente leitura, encontrou três pontos. Foram corrigidos
com testes que reproduziram as falhas antes das correções:

1. Aba antiga com cookie de outra sessão: comparação de contexto antes do backend, sem POST financeiro.
2. Quota/falha do storage após criação: cache primeiro e, inicialmente, gravação local best-effort; na fase 7 o armazenamento local da cobrança foi removido.
3. Sucesso bloqueado pela consulta de 1.000 débitos: boundary apenas em nova/revisão/confirmação; sucesso independente dessa API.

Correções desse checkpoint: 4 arquivos / 18 testes aprovados. Na revisão da
fase 7 foi reproduzida e corrigida outra falha: cancelamento da requisição após
o retorno da integração descartava o resultado antes de registrá-lo no mock.
Dois casos de regressão confirmam que Completed e Indeterminate permanecem
consultáveis, sem iniciar nova operação financeira.

### Verificação final automatizada

Comandos executados em `frontend/` depois das correções da revisão:

| Comando                                                 | Resultado                                                                 |
| ------------------------------------------------------- | ------------------------------------------------------------------------- |
| `npm.cmd run lint`                                      | PASS, exit 0, sem warnings                                                |
| `npx.cmd tsc --noEmit --incremental false`              | PASS, exit 0                                                              |
| `npm.cmd test -- --pool=threads --maxWorkers=1`         | PASS, 23 arquivos / 72 testes, exit 0; 79,00 s                            |
| `npm.cmd run build` com `API_URL=http://localhost:5000` | PASS, exit 0; Next 16.3.5; 11 Route Handlers e páginas existentes gerados |

Testes abrangem contratos HTTP/payload, JSON/204/ProblemDetails, cancelamento,
ausência de retry, sessão/expiração/logout/CSRF, duas sessões concorrentes,
resposta atrasada de filtros, isolamento de contexto, formulários, duplo clique,
erros parciais, quota de storage, PIX/entrega e sucesso sem API de débitos.
Testes antigos movidos junto do código e preservados.

Workers foram limitados após o timeout anterior para executar a suíte inteira.
Não houve alteração de assertions para mascarar falhas nem certificação do paralelismo irrestrito.
Nenhum POST financeiro real foi enviado. A integração Development pode alcançar Nibo;
smoke usa transporte simulado e não certifica a integração externa.

### Fase 7 — consulta mock implementada e autorizada

O usuário autorizou implementar a consulta sem persistência, usando mock no
backend e anotação explícita. O `ChargesController` agora expõe GET por ID;
`GetChargeQueryHandler` consulta pelo tenant do contexto autenticado. O
`InMemoryChargeRepository` fica singleton na API e possui TODO indicando
substituição futura por persistência real. Não houve SQL, migration ou scaffold.

O mock registra os dados reais retornados pelo POST: ID Yalla, valores, PIX,
datas, resultado financeiro, ID Nibo somente quando confirmado, e entrega.
Resposta Rejected/Indeterminate permanece parcial; repetir manualmente a mesma
operação atualiza o resultado financeiro, sem apagar criação/entrega anteriores.
O resultado já retornado pela integração é registrado mesmo se a requisição
for cancelada nesse momento. O fluxo financeiro existente não foi substituído.

Frontend: `charges.client.ts`/`charges.server.ts`, Route Handler
`app/api/charges/[id]/route.ts` e `hooks/useCharge.ts`. Query key inclui tenant,
versão pública da sessão e ID. `useCreateCharge` fornece o resultado do POST ao
cache; a primeira exibição não depende de GET. Cache vazio consulta a API.
`created-charge-storage.ts` e `useCreatedCharge` foram removidos. Entrega usa a
data confirmada pelo backend; falha não é registrada como sucesso.

Verificação backend, na raiz: `dotnet test YallaFinance.sln --no-restore --verbosity minimal`
**PASS: 44 testes unitários + 7 testes de integração HTTP, exit 0**. Nos testes
HTTP somente a integração financeira externa é substituída; autenticação,
controllers, handlers e repositórios da API são exercitados. Casos: consulta em
outro cliente, resultados parciais, tenant diferente/ID inexistente/sem login,
entrega e repetição, atualização do resultado e descarte após recriar a API.

**Limite explícito:** reiniciar a API elimina registros; instâncias diferentes
não compartilham o catálogo. Persistência autoritativa durável não foi
implementada, conforme o escopo solicitado, e permanece requisito de produção.

### Conferência operacional no navegador

Roteiro local em `.tmp/frontend-smoke.mjs`, executado com `node .tmp/frontend-smoke.mjs`
na raiz. Chrome headless com perfil exclusivo, servidor Next **production** na porta
3100 e fixture HTTP local na porta 5055. Diretório de sessões exclusivo por execução.
O roteiro encerrou somente os processos que criou. Resultado final: **PASS, exit 0**.
Capturas de home, revisão, sucesso e navegação móvel foram geradas em
`.tmp/frontend-smoke/`; artefatos locais são ignorados pelo Git.

| Cenário                         | Resultado observado                                                                                              |
| ------------------------------- | ---------------------------------------------------------------------------------------------------------------- |
| Entrada, login e empresa        | Redirecionamento para login; seleção explícita de Empresa A; dashboard autorizado                                |
| Sessão                          | Cookie HttpOnly com 64 caracteres hexadecimais; invisível a document.cookie; nenhum Bearer no conteúdo da página |
| Viewport móvel                  | Menu abre, permite navegar para consulta e fecha; capturas em 390 × 844                                          |
| API de indicadores indisponível | Erro recuperável; botão Tentar novamente recupera dados após restaurar fixture                                   |
| Débitos                         | Filtro Paid, limpeza, paginação e detalhe enviam consultas correspondentes                                       |
| Acesso direto às etapas         | Revisão avulsa sem draft mostra erro existente; confirmação sem draft redireciona à etapa inicial com orientação |
| Cobrança avulsa                 | Cliente/categoria, moeda 1.234,56, data, descrição, vencimento e seleção explícita de DEF4G56 entre duas placas  |
| Revisão e voltar                | Voltar da confirmação à revisão conserva dados preparados                                                        |
| Confirmação repetida            | Duas ativações enviam somente um POST, valor 1234.56, movement-1 e chave estável                                 |
| Sucesso e PIX                   | IDs/dados exibidos; código realmente copiado para clipboard                                                      |
| Entrega                         | Um POST de entrega; botão desabilitado após confirmação                                                          |
| Sucesso imediato                 | Dados da resposta do POST em cache; nenhum GET redundante nem registro de cobrança em sessionStorage            |
| Refresh do sucesso              | GET recupera dados e entrega; nenhuma consulta de débitos, mesmo com fixture de débitos em 503                    |
| Nova aba                         | Cache vazio consulta cobrança e entrega pelo GET; não cria ou entrega novamente                                 |
| Cobrança selecionada            | Um débito selecionado; source Selected, debtIds [debt-0], valor 100 e payload preservado                         |
| Logout e outra empresa          | Sessão anterior removida; Empresa B não recupera cobrança da Empresa A                                           |
| Contexto de aba antiga          | POST com contexto antigo retorna 409 SESSION_CONTEXT_CHANGED; nenhum POST adicional chega ao backend             |

O formulário novo ao retornar da revisão para `/charges/new` continua iniciando
com valores padrão, como no código anterior; não foi criada restauração de campos
que não existia. A manutenção de draft entre confirmação e revisão foi conferida.

Limites: transporte .NET simulado; não certifica login/autorização de produção,
cadastros externos, estado real da Nibo ou comparação visual automatizada antes/depois.
Não houve criação real de cobrança. Erros Rejected/Indeterminate, expiração,
401/403, concorrência, storage indisponível e resposta atrasada têm evidência nos
testes automatizados. O smoke de navegador usa fixture HTTP; os 7 testes de
integração separados exercitam a implementação real do GET e do mock na API .NET.

### Definition of Done e exceções remanescentes

O checklist do contrato foi conferido: localização por feature, rotas simples,
APIs explícitas e rastreáveis, separação browser/server, cache/formulários,
cancelamento, idempotência, distinção de estados financeiros, erros e checks
possuem implementação/evidência. Foram removidos o provider/repository antigos,
o hook novo sem consumidor e a pasta vazia de finance. Imports antigos ausentes;
nenhum segredo/API_URL/store de sessão encontrado nos chunks públicos do build.

**Escopo local solicitado concluído:** consulta mock no backend e sucesso com
recarga/nova aba validados, sem histórico de cobrança no browser. **Ainda não é
aceite integral de produção:** falta persistência durável, o armazenamento de
sessão precisa ser validado conforme a hospedagem escolhida, e a integração
financeira real não foi exercitada neste smoke. README/AGENTS e planejamento
registram esses limites. Sem commit, staging ou publicação.
