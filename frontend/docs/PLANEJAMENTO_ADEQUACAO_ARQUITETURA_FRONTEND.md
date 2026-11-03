# Planejamento de adequação arquitetural do frontend — Yalla Finance

> Versão 1.2 — 01/10/2026. Status: execução na main; fases 1–6 implementadas, consulta da fase 7 implementada com mock backend autorizado, fase 8 com limpeza/checks executados. Sem commit.
> Para execução por agentes: utilizar a skill `superpowers:executing-plans`, fase por fase. Delegação não é necessária para este plano.

**Objetivo:** adequar o frontend ao contrato final, preservando as telas, rotas, regras financeiras e operações existentes, com evidência de funcionamento ao final de cada fase.

**Arquitetura:** organização vertical por feature, APIs explícitas e hooks para estado remoto. Transporte com contrato comum e configurações por ambiente; chamadas autenticadas do navegador atravessam a fronteira Next. O rascunho de cobrança permanece separado dos dados persistidos pelo backend.

**Stack:** Next.js App Router, React, TypeScript strict e Material UI existentes; Axios, TanStack Query, React Hook Form, Zod e resolver de formulário conforme o contrato. Manter Node 24.17.0 e as versões atuais de Next/React; não combinar esta migração com atualização do framework.

**Contrato:** [ARQUITETURA_PADRAO_FRONTEND_YALLA_FINANCE_FINAL.md](./ARQUITETURA_PADRAO_FRONTEND_YALLA_FINANCE_FINAL.md).

## Estado da execução

O usuário autorizou execução na `main`, exclusão de código obsoleto e ajustes
de rota necessários, preservando operações/regras financeiras e decidindo depois
sobre o commit. Nenhum commit, staging ou publicação foi realizado.

Evidências e limites: [VALIDACAO_MIGRACAO_FRONTEND.md](./VALIDACAO_MIGRACAO_FRONTEND.md).
A consulta da fase 7 foi implementada com armazenamento em memória no backend,
conforme autorização posterior do usuário: sem persistência em banco, com anotação
explícita do mock. A cobrança vem da resposta real do POST; recarga/nova aba usam
o GET autenticado. Persistência financeira durável fica fora desta entrega.
A produção ainda depende dessa persistência e da definição/validação do store de
sessão na hospedagem, que o usuário informou ainda não estar definida.

O histórico anterior é a referência inicial. As tarefas marcadas abaixo indicam
execução verificada; tarefas abertas identificam pendências, sem sugerir aprovação integral.

## 1. Restrições globais

- Preservar URLs das páginas, identidade visual, responsividade, textos e acessibilidade, exceto correções funcionais explicitamente registradas.
- Preservar filtros, seleção de débitos, regras de cliente/tenant, valores, datas, payloads, métodos HTTP, status financeiros e geração da chave de idempotência.
- Não trocar o backend financeiro nem repetir POSTs reais para testar refatoração. Exceção autorizada: GET de cobrança no backend com mock em memória anotado, preservando os resultados reais da criação.
- Não misturar movimentação de arquivos, mudança de biblioteca e correção funcional no mesmo conjunto de alterações.
- Contexto de tenant vem da sessão validada; query keys não conferem autoridade ao backend.
- Não remover código antigo antes de migrar e validar todos os consumidores.
- Em cada fase, registrar arquivos alterados, comandos, resultados, limitações e diferenças observadas. Execução incompleta não significa aprovação.
- Fazer entregas pequenas e revisáveis. Criar commits por fase apenas quando a execução autorizar commits; este planejamento não autoriza publicação ou merge.

## 2. Preservação de comportamento e exceções previstas

Adequação total não significa preservar defeitos. As mudanças abaixo são exigidas pelo contrato e devem ter testes e registro separados da reorganização:

| Mudança intencional                | Comportamento desejado                                           | Fase |
| ---------------------------------- | ---------------------------------------------------------------- | ---- |
| Token fora do navegador            | Login e operações continuam disponíveis; cookie não expõe Bearer | 3    |
| Limpeza entre sessões              | Novo usuário/tenant não vê dados anteriores                      | 3–4  |
| Falha de carregamento              | Exibir erro recuperável em vez de loading permanente             | 4    |
| Integração rejeitada/indeterminada | Preservar cobrança criada e informar seu estado                  | 6    |
| Consulta da cobrança               | Recarregar pelo backend, sem depender do histórico da aba        | 7    |

Todo desvio adicional deve ser tratado como mudança funcional independente.
O plano preserva o fluxo operacional existente; não promete equivalência de
timing interno, quantidade de requisições ou comportamento defeituoso.

## 3. Pontos de atenção na revisão

1. Logout seguido de login com outro usuário ou tenant: dados anteriores não reaparecem — testar nas fases 3 e 4.
2. API indisponível: erro recuperável e ausência de carregamento infinito — testar na fase 4.
3. Confirmação repetida ou resposta perdida: mesma operação não gera POST automático adicional — testar na fase 6.
4. Recarregamento e acesso direto às etapas: rascunho ausente não produz cobrança incompleta — testar na fase 6.
5. Cobrança criada com integração parcial: ID e estado não são descartados — testar nas fases 6 e 7.

## 4. Mapa de organização desejada

| Origem atual                                                                                                          | Destino/responsabilidade                                                                                                                               |
| --------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `src/app/(finance)/home/HomePageClient.tsx`                                                                           | `src/features/dashboard/components/HomePageClient.tsx`                                                                                                 |
| `src/app/(finance)/debts/*Client.tsx` e `[id]/DebtDetailClient.tsx`                                                   | `src/features/debts/components/`                                                                                                                       |
| `src/app/(finance)/charges/*/*Client.tsx`                                                                             | `src/features/charges/components/`                                                                                                                     |
| `src/app/(auth)/login/LoginForm.tsx`                                                                                  | `src/features/auth/components/LoginForm.tsx`                                                                                                           |
| `src/components/finance/DebtTable.tsx`, `DebtFilters.tsx`, `SelectionBar.tsx`                                         | `src/features/debts/components/`                                                                                                                       |
| `src/components/finance/ChargeStepper.tsx`, `VehicleMatchPanel.tsx`, `ReadOnlyMappingPanel.tsx`, `PrototypePanel.tsx` | `src/features/charges/components/`, após conferir os consumidores                                                                                      |
| `src/components/finance/OperationalIndicatorCard.tsx`, `AnimatedMetric.tsx`                                           | `src/features/dashboard/components/`, se seus consumidores forem dessa feature                                                                         |
| `src/features/finance/businessRules.ts`, `chargeFlow.ts`, `clipboard.ts`                                              | `src/features/charges/`, com regras de seleção de débitos sob proprietário explícito                                                                   |
| `src/features/finance/debtStatus.ts`, `debtNavigation.ts`                                                             | `src/features/debts/`                                                                                                                                  |
| `src/features/finance/types.ts`                                                                                       | Tipos de `debts`, `charges`, `dashboard`, `customers` e `catalogs`; somente contratos transversais realmente compartilhados ficam fora dessas features |
| `src/features/finance/apiFinanceRepository.ts`                                                                        | APIs próprias de cada feature; depois remover o repository                                                                                             |
| `src/features/finance/FinanceProviderImpl.tsx`                                                                        | Queries nas features e `src/features/charges/context/ChargeDraftProvider.tsx`                                                                          |
| `src/services/api/httpClient.ts`                                                                                      | `src/services/api/api-client.ts`, `api-client.server.ts` e `api-error.ts`, com contrato comum e configuração separada                                  |

`FinanceShell`, `FinanceHeader`, `FinanceSidebar`, `FinanceSessionSummary` e
metadados de navegação permanecem transversais. Outros componentes só mudam
de proprietário após verificar todos os imports. Mover testes junto dos
componentes e regras; não criar pastas vazias.

## Fase 0 — Estabelecer a referência de funcionamento

**Arquivos:** testes existentes, `frontend/vitest.config.ts` se um problema de execução for comprovado, e `frontend/docs/VALIDACAO_MIGRACAO_FRONTEND.md`.

- [x] Inventariar endpoints realmente usados, payloads, erros, headers e páginas; registrar no documento de validação sem credenciais.
- [x] Executar lint, TypeScript, testes e build. Investigar separadamente os timeouts de workers já observados; não alterar regras do produto para fazer testes passarem.
- [ ] Registrar fluxo manual de login/empresa, indicadores, filtro/paginação, detalhe, seleção, cobrança avulsa, revisão, confirmação, sucesso, cópia PIX e entrega.
- [x] Registrar regras e casos de borda existentes, inclusive seleção inválida, movimentação ausente/múltipla, moeda, datas e duplo clique.
- [x] Complementar testes de comportamento onde faltar proteção para os fluxos que serão alterados; usar transporte simulado nas operações financeiras.

**Saída:** referência reproduzível e limitações conhecidas. Se a suíte não executar integralmente, resolver a causa antes da migração ampla. Não afirmar que o estado atual está aprovado com base apenas em testes parciais.

## Fase 1 — Reorganizar arquivos sem trocar mecanismos

**Arquivos:** origens/destinos do mapa; `app/**/page.tsx`; testes colocados junto das features.

- [x] Mover componentes, regras e testes, preservando exportações e implementações.
- [x] Ajustar imports; manter páginas pequenas compondo os mesmos componentes.
- [x] Dividir tipos por proprietário sem alterar campos ou conversões.
- [x] Manter temporariamente `FinanceProvider` e repository para que a etapa seja executável.
- [x] Comparar renderização, navegação e testes com a fase 0.

**Saída:** mesmas telas e operações na nova localização. Mudanças de formulário, sessão e cache ficam para as próximas fases.

## Fase 2 — APIs por feature e contrato de transporte

**Criar:** `features/dashboard/api/dashboard.client.ts`, `features/debts/api/debts.client.ts`, `features/charges/api/charges.client.ts`, `features/customers/api/customers.client.ts`, `features/catalogs/api/categories.client.ts`, `features/catalogs/api/payment-methods.client.ts`, `services/api/api-client.ts`, `services/api/api-error.ts`.

**Modificar:** repository temporário, consumidores e `package.json`/lockfile para Axios.

- [x] Escrever testes que capturem URL, método, query params, payload e `Idempotency-Key` das operações atuais antes de trocar o transporte.
- [x] Definir transporte: `get<T>(path, options): Promise<T>`, `post<T>(path, body, options): Promise<T>`; opções suportam params, headers e `AbortSignal`.
- [x] Manter corpo tipado, resposta 204 sem corpo e normalização de erros. Preservar parâmetros e codificação de IDs.
- [x] Extrair endpoints existentes: débitos, dashboard, clientes/movimentos, categorias, formas de pagamento, criação e entrega de cobrança.
- [x] Preservar composição/conversões do fluxo atual em funções identificáveis; não concentrar consultas de todos os módulos em `charges.client.ts`.
- [x] Usar o repository temporário apenas como ponte para consumidores ainda não migrados; não introduzir novas dependências dele.
- [x] Confirmar que nenhum retry automático de POST foi introduzido.

**Saída:** chamadas equivalentes à referência. Sessão atual só permanece durante esta transição; a fase não constitui conclusão da adequação de segurança.

## Fase 3 — Sessão server-only e fronteira Next

**Criar:** `features/auth/server/session.ts`, `services/api/api-client.server.ts`; `*.client.ts`/`*.server.ts` nas APIs autenticadas, e Route Handlers Next para operações existentes.

**Modificar:** `features/auth/actions.ts`, `authSession.ts`, `components/auth/SessionGuard.tsx`, `app/(finance)/layout.tsx` e APIs da fase 2.

- [x] Antes da implementação, registrar a decisão de armazenamento da sessão: identificador opaco, armazenamento de credenciais no servidor, expiração e remoção no logout. Conferir suporte do ambiente de execução; memória de um único processo não é persistência de produção.
- [x] Testar login, seleção de empresa, expiração e logout; o browser não recebe Bearer em cookie legível, props, storage ou resposta HTTP.
- [x] Validar sessão em cada operação protegida. Server Components e Server Actions chamam os módulos server diretamente.
- [x] Criar encaminhamento explícito por operação: `/api/debts`, `/api/debts/[id]`, `/api/dashboard/summary`, `/api/customers`, `/api/customers/[id]/movements`, `/api/categories`, `/api/payment-methods` e `/api/charges` no Next; confirmar os destinos .NET reais antes de implementar cada rota.
- [x] Não aceitar URL arbitrária, copiar headers indiscriminadamente ou alterar Bearer em instância global. Preservar idempotência e distinguir 401 de 403.
- [x] Definir origem privada no servidor, resposta autenticada `private, no-store` e proteção de mutations contra CSRF.
- [x] Limpar dados locais do contexto anterior e validar login de outro usuário/tenant na mesma aba.

**Saída:** fluxo operacional de login preservado, credenciais protegidas e chamadas encaminhadas corretamente. Mudanças de segurança registradas como intencionais.

## Fase 4 — Migrar estado remoto para TanStack Query

**Criar:** `providers/QueryProvider.tsx`; hooks e query keys em `dashboard`, `debts`, `customers` e `catalogs`.

**Modificar:** `providers/AppProviders.tsx`, componentes dessas features e `FinanceProviderImpl.tsx` temporário.

- [x] Adicionar TanStack Query sem atualizar Next/React. Criar QueryClient estável e configuração explícita de retry/refetch; mutations financeiras com `retry: false`.
- [x] Implementar `useDashboardSummary`, `useDebtSearch` e `useDebt` (contexto obtido do SessionProvider), propagando signal e contexto; mover catálogos para queries próprias.
- [x] Manter filtros, ordenação e paginação existentes; impedir resposta antiga de substituir resultado de filtro mais recente.
- [x] Invalidar somente o contexto afetado. Cancelar consultas e limpar cache no logout/expiração/mudança de sessão.
- [x] Testar isolamento, carregamento, vazio e recuperação após falha, inclusive indisponibilidade de categorias.
- [x] Retirar carregamento obrigatório dos 1.000 débitos do provider. Durante a transição, fornecer aos consumidores restantes dados por queries explícitas sem alterar as regras de seleção/duplicidade.

**Saída:** dados remotos com cache por contexto e estados locais; nenhuma tela fica dependente de um carregamento global infinito. Quantidade de chamadas pode mudar, mas filtros e resultados devem permanecer equivalentes.

## Fase 5 — Formulários com React Hook Form e Zod

**Criar:** schemas e hooks de formulário em `features/charges`; em `features/auth` quando aplicável ao fluxo existente.

**Modificar:** `NewChargeClient`, `ChargeReviewClient`, `ChargeConfirmationClient`, `LoginForm` e testes relacionados.

- [x] Adicionar bibliotecas e resolver, preservando valores iniciais, máscaras, mensagens, obrigatoriedade e momento de exibição dos erros.
- [x] Testar valores monetários, precisão, datas no fuso de negócio, cliente, categoria, pagamento e movimentação antes de trocar o mecanismo.
- [x] Reutilizar regras puras existentes; schema não substitui autorização nem validação no backend.
- [x] Garantir equivalência do payload enviado e do comportamento ao voltar entre etapas.

**Saída:** mesmos formulários e validações percebidas, agora no padrão. Qualquer diferença de UX deve ser registrada e avaliada antes de encerrar a fase.

## Fase 6 — Separar rascunho e preservar resultado financeiro

**Criar:** `features/charges/context/ChargeDraftProvider.tsx`, `features/charges/hooks/useCreateCharge.ts` e testes do fluxo de cobrança.

**Modificar:** layout das etapas, componentes de cobrança, `chargeFlow.ts` e API de criação.

- [x] Limitar o provider ao escopo das rotas de cobrança; manter rascunho entre navegações internas.
- [x] Testar revisão/voltar, ausência de rascunho, acesso direto e recarregamento; sem rascunho válido, retornar à etapa inicial com orientação.
- [x] Preservar o algoritmo de idempotência e comparar corpo/header com a referência. Testar duplo clique e resposta perdida sem POST automático adicional.
- [x] Separar erro HTTP de `Completed`, `Rejected` e `Indeterminate`; preservar ID e estado recebidos, sem converter estado técnico em `DebtStatus`.
- [x] Testar integração rejeitada/indeterminada e orientar consulta antes de novo envio. Não implementar decisão de ERP no frontend.
- [x] Invalidar débitos e dashboard conforme a resposta real; não marcar uma cobrança como concluída por otimismo.

**Saída:** fluxo normal preservado e resultado parcial representado explicitamente. Alteração intencional em relação ao erro genérico atual deve constar da validação.

## Fase 7 — Persistência autoritativa e consulta de cobrança

**Escopo autorizado e implementado:** acrescentado `GET /api/charges/{id}` no backend, com MediatR e repositório singleton mock em memória. Consulta e entrega usam o tenant autenticado; ID inexistente ou de outro tenant retorna 404. O contrato reutiliza os dados reais da criação, incluindo integração e entrega. Não houve alteração de banco ou persistência durável.

- [x] Revisar persistência e contratos existentes antes de propor endpoint. Registrar separadamente a implementação backend necessária e seu escopo; não inventar rota funcional no frontend.
- [x] Exigir contrato de consulta autorizada por tenant contendo os dados da cobrança necessários à tela, IDs reais, entrega e resultado financeiro conforme o backend.
- [x] Após disponibilização do endpoint, criar `features/charges/api/charges.server.ts`/`charges.client.ts` e `hooks/useCharge.ts` para consulta por ID.
- [x] Carregar sucesso e acompanhamento pelo backend; remover dependência do histórico em `sessionStorage`.
- [x] Testar recarregamento, nova aba, ID inexistente, acesso sem permissão, contexto de outro tenant e resultado parcial.
- [x] Preservar cópia PIX e entrega com os dados retornados pelo contrato real.

**Saída verificada:** cobrança consultável independentemente da aba enquanto a mesma API estiver em execução. O sucesso imediato aproveita o resultado do POST em cache, sem GET redundante. Refresh/nova aba consultam o backend. **TODO de produção:** substituir o repositório mock por persistência real; reiniciar a API descarta os registros, e múltiplas instâncias não compartilham o catálogo. A autorização do mock não redefine o requisito final de persistência autoritativa.

## Fase 8 — Remover transições e validar a entrega completa

**Remover após confirmar ausência de consumidores:** repository/provider antigos, reexports de compatibilidade e armazenamento de cobranças substituído pelo backend.

- [x] Verificar imports com `rg`: sem repository/provider antigos, `created-charge-storage.ts` ou `useCreatedCharge`; fallback de cobrança da mesma aba removido.
- [x] Revisar todas as APIs: sem chamadas de negócio dispersas em componentes, segredos no bundle, instância global com Bearer mutável ou ciclos entre features.
- [x] Atualizar README e AGENTS com referência ao contrato final; documentação histórica não deve competir com ele.
- [x] Executar lint, TypeScript, suíte completa, build e matriz operacional disponível no navegador com transporte simulado; diferenças das fases 3, 4, 6 e consulta mock da fase 7 registradas. Integração externa real não certificada.
- [x] Revisar o Definition of Done arquitetural do contrato e registrar evidências da migração.

**Saída:** projeto aderente, funções atuais validadas e dependências backend resolvidas. Sem endpoint autoritativo, checks completos ou evidência operacional, registrar entrega parcial.

## 5. Comandos e evidências

Executar em `frontend/`, com ambiente local configurado sem imprimir segredos:

```powershell
npm.cmd run lint
npx.cmd tsc --noEmit --incremental false
npm.cmd test
npm.cmd run build
```

Para diagnóstico da execução de testes, pode-se executar um arquivo isolado
ou limitar workers, registrando a razão. Não reduzir a cobertura para esconder
falhas. Se o build conflitar com desenvolvimento ativo, usar ambiente isolado;
não encerrar processos do usuário sem necessidade ou autorização.

Durante a fase, executar testes pertinentes. No fechamento, exigir lint,
TypeScript, suíte pertinente integral, build e smoke manual. Resultado esperado:
comandos concluídos sem erro e casos funcionais conferidos. Uma falha anterior
deve ser identificada como anterior, sem ser silenciosamente ignorada.

## 6. Revisão, recuperação e progresso

- Se surgir regressão, interromper a próxima fase e corrigir a menor mudança responsável.
- Comparar com a referência, não ajustar testes somente para aceitar comportamento novo.
- Recuperar apenas alterações da fase; não usar reset destrutivo nem apagar trabalho do usuário.
- Antes de cada fase, conferir mudanças concorrentes e revalidar os contratos afetados.
- Registrar em `VALIDACAO_MIGRACAO_FRONTEND.md`: fase, status, evidência, diferença intencional, dependência e resultado.

| Fase | Status da execução                                                   | Condição principal                                 |
| ---- | -------------------------------------------------------------------- | -------------------------------------------------- |
| 0    | Referência automatizada registrada; smoke inicial real não executado | Limite explicitado no registro                     |
| 1    | Implementada                                                         | Reorganização e testes preservados                 |
| 2    | Implementada                                                         | Contratos HTTP aprovados                           |
| 3    | Implementada no Node atual                                           | Store de produção depende da hospedagem            |
| 4    | Implementada                                                         | Queries, isolamento e recuperação testados         |
| 5    | Implementada                                                         | Regras e comportamento preservados                 |
| 6    | Implementada                                                         | Resultados parciais e idempotência preservados     |
| 7    | Consulta implementada com mock backend autorizado                    | Persistência durável fica para produção            |
| 8    | Limpeza, checks e smoke executados                                   | Fallback removido; limites de produção registrados  |

## 7. Critério final de aceite

- [x] Rotas e componentes visuais preservados; operações disponíveis no smoke com transporte simulado. Comparação visual automatizada antes/depois e integração real não certificadas.
- [x] Mesmas regras financeiras, filtros, payloads e identidade da operação.
- [x] Exceções funcionais implementadas documentadas e verificadas; consulta mock da fase 7 autorizada.
- [x] Sessão, contexto e dados de usuários isolados no ambiente Node verificado.
- [x] Cobrança consultável no backend, com resultados financeiros preservados.
- [x] Mecanismo temporário remanescente documentado: repositório mock em memória no backend, sem armazenamento de cobrança no browser.
- [ ] Definition of Done do contrato final atendido.
- [ ] Checks e matriz operacional concluídos; limitações e dependências resolvidas.

Este documento acompanha a execução autorizada. O escopo local com consulta mock
está implementado e validado; aceite integral para produção exige persistência
durável e validação da hospedagem/sessão. Não houve commit ou publicação.
