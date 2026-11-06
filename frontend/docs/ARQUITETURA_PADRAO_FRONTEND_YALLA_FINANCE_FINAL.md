# Arquitetura Padrão do Frontend — Yalla Finance — FINAL

> Versão: Final 1.0 — 01/10/2026.
> Status: **APROVADO — referência arquitetural vigente para o frontend.**
> Este documento é o contrato padrão para implementação, refatoração e revisão do frontend do Yalla Finance. Sua aprovação arquitetural não implica que toda a migração do código já tenha sido executada.

## Consolidação final a partir da V4

- Define fronteira Next para chamadas autenticadas com Bearer mantido no servidor.
- Permite acesso direto à API para chamadas públicas conforme seu contrato.
- Distingue APIs de feature justificadas de camadas intermediárias redundantes.
- Atualiza exemplos com tenant explícito, cancelamento e endpoints reais.
- Uniformiza TanStack Query e React Hook Form + Zod como padrões.
- Formaliza a precedência deste documento para decisões específicas de frontend.

## 1. Objetivo

Este documento define o padrão de organização do frontend do **Yalla
Finance**, desenvolvido com Next.js.

O objetivo principal não é criar o maior número possível de abstrações,
mas manter o projeto:

-   simples de ler;
-   previsível;
-   separado por responsabilidade;
-   fácil de manter;
-   fácil de navegar;
-   fácil de revisar;
-   fácil de evoluir;
-   principalmente, fácil de rastrear entre **tela → comportamento →
    chamada HTTP → endpoint do backend**.

Uma pessoa que esteja analisando uma tela deve conseguir descobrir
rapidamente qual endpoint do backend fornece ou altera os dados daquela
funcionalidade, sem precisar atravessar Providers, Repositories,
Gateways, Adapters ou outras abstrações que apenas encapsulem chamadas
HTTP.

------------------------------------------------------------------------

## 2. Princípio principal: rastreabilidade

O fluxo padrão deve ser curto e explícito:

``` text
ROTA
  ↓
COMPONENTE DA FEATURE
  ↓
HOOK
  ↓
API DA FEATURE
  ↓
API CLIENT
  ↓
BACKEND
```

Exemplo:

``` text
app/(finance)/debts/page.tsx
        ↓
features/debts/components/DebtsPage.tsx
        ↓
features/debts/hooks/useDebts.ts
        ↓
features/debts/api/debts.api.ts
        ↓
apiClient
        ↓
GET /api/debts
```

O caminho inverso também deve ser facilmente compreendido:

``` text
GET /api/debts
      ↑
debtsApi.getAll()
      ↑
useDebts()
      ↑
DebtsPage
      ↑
/debts
```

### Regra arquitetural

> Deve ser possível partir de uma tela ou componente e identificar o
> endpoint correspondente pelo fluxo explícito adequado ao ambiente.
> **Component → Hook → API → Endpoint** é o exemplo de execução no cliente;
> chamadas autenticadas incluem a fronteira Next, e operações no servidor
> não exigem hook. Os fluxos completos estão definidos na seção 40.

O `apiClient` pode existir entre API e backend, porém ele é apenas
infraestrutura HTTP e não deve esconder o endpoint utilizado.

------------------------------------------------------------------------

## 3. Estrutura recomendada

``` text
frontend/
├── public/
│
├── docs/
│
├── src/
│   ├── app/
│   │   ├── layout.tsx
│   │   ├── page.tsx
│   │   ├── loading.tsx
│   │   ├── error.tsx
│   │   ├── global-error.tsx
│   │   ├── not-found.tsx
│   │   │
│   │   ├── (auth)/
│   │   │   └── login/
│   │   │       └── page.tsx
│   │   │
│   │   └── (finance)/
│   │       ├── layout.tsx
│   │       │
│   │       ├── home/
│   │       │   └── page.tsx
│   │       │
│   │       ├── debts/
│   │       │   ├── page.tsx
│   │       │   └── [id]/
│   │       │       └── page.tsx
│   │       │
│   │       └── charges/
│   │           ├── new/
│   │           │   └── page.tsx
│   │           ├── review/
│   │           │   └── page.tsx
│   │           ├── confirmation/
│   │           │   └── page.tsx
│   │           └── success/
│   │               └── page.tsx
│   │
│   ├── features/
│   │   ├── auth/
│   │   │   ├── actions/
│   │   │   ├── components/
│   │   │   └── types/
│   │   │
│   │   ├── dashboard/
│   │   │   ├── api/
│   │   │   ├── hooks/
│   │   │   ├── components/
│   │   │   ├── types/
│   │   │   └── schemas/
│   │   │
│   │   ├── debts/
│   │   │   ├── api/
│   │   │   │   └── debts.api.ts
│   │   │   ├── hooks/
│   │   │   │   ├── useDebts.ts
│   │   │   │   └── useDebt.ts
│   │   │   ├── components/
│   │   │   │   ├── DebtsPage.tsx
│   │   │   │   ├── DebtsTable.tsx
│   │   │   │   ├── DebtFilters.tsx
│   │   │   │   └── DebtDetails.tsx
│   │   │   ├── types/
│   │   │   │   └── debt.types.ts
│   │   │   └── schemas/
│   │   │       └── debt.schema.ts
│   │   │
│   │   └── charges/
│   │       ├── api/
│   │       │   └── charges.api.ts
│   │       ├── hooks/
│   │       ├── components/
│   │       ├── types/
│   │       └── schemas/
│   │
│   ├── components/
│   │   ├── ui/
│   │   ├── auth/
│   │   │   └── SessionGuard.tsx
│   │   └── finance/
│   │       ├── FinanceShell.tsx
│   │       └── FinanceMenu.tsx
│   │
│   ├── services/
│   │   └── api/
│   │       ├── api-client.ts
│   │       └── api-error.ts
│   │
│   ├── providers/
│   │   ├── AppProviders.tsx
│   │   ├── QueryProvider.tsx
│   │   └── NotificationProvider.tsx
│   │
│   ├── theme/
│   └── test/
│
├── next.config.ts
├── tsconfig.json
├── eslint.config.mjs
└── vitest.config.ts
```

Não é necessário criar antecipadamente todas as subpastas de uma
feature. Uma pasta deve existir quando houver responsabilidade real para
ela.

------------------------------------------------------------------------

# 4. Responsabilidade de cada área

## 4.1 `app/` --- rotas e composição

`app/` representa a estrutura de navegação do Next.js.

Responsabilidades:

-   rotas;
-   layouts;
-   route groups;
-   parâmetros de rota;
-   `loading.tsx`;
-   `error.tsx`;
-   `not-found.tsx`;
-   composição inicial da página;
-   fronteiras Server/Client quando aplicável.

Uma `page.tsx` deve ser pequena.

Exemplo:

``` tsx
import { DebtsPage } from "@/features/debts/components/DebtsPage";

export default function Page() {
  return <DebtsPage />;
}
```

### `app/` não deve

-   chamar endpoints diretamente;
-   executar `fetch` ou Axios para APIs de negócio sem uma justificativa
    arquitetural explícita;
-   concentrar regras de negócio;
-   implementar tabelas e formulários complexos;
-   duplicar lógica existente em uma feature;
-   conhecer detalhes da integração do backend.

A rota informa **onde** uma funcionalidade aparece.

A feature informa **como** aquela funcionalidade funciona.

------------------------------------------------------------------------

# 5. `features/` --- funcionalidades do sistema

As funcionalidades devem ser organizadas verticalmente.

Evitar:

``` text
features/
└── finance/
    ├── repositories/
    ├── providers/
    ├── rules/
    ├── contracts/
    └── ...
```

`finance` representa praticamente todo o produto e tende a se
transformar em uma pasta genérica.

Preferir:

``` text
features/
├── debts/
├── charges/
├── dashboard/
└── auth/
```

Cada feature reúne o código relacionado àquela capacidade do sistema.

Exemplo:

``` text
features/debts/
├── api/
├── hooks/
├── components/
├── types/
└── schemas/
```

Isso permite abrir `features/debts` e visualizar praticamente tudo
relacionado à consulta e manipulação de débitos.

------------------------------------------------------------------------

# 6. `features/<feature>/api/` --- comunicação com o backend

Toda chamada HTTP de negócio deve estar explicitamente declarada na API
da respectiva feature.

Exemplo:

``` text
features/debts/api/debts.api.ts
```

``` ts
export const debtsApi = {
  getAll: (params: GetDebtsParams, options: { signal?: AbortSignal } = {}) =>
    apiClient.get<PaginatedResponse<Debt>>("/api/debts", {
      params,
      signal: options.signal,
    }),

  getById: (id: string) =>
    apiClient.get<Debt>(`/api/debts/${encodeURIComponent(id)}`),

  // Ilustrativo: criação e cancelamento não estão no controller atual.
  create: (request: CreateDebtRequest) =>
    apiClient.post<Debt>("/api/debts", request),

  cancel: (id: string) =>
    apiClient.post(`/api/debts/${encodeURIComponent(id)}/cancel`),
};
```

A finalidade é permitir que o desenvolvedor abra um único arquivo e
enxergue claramente quais endpoints aquela feature utiliza.

### Deve conter

-   endpoint;
-   verbo HTTP;
-   request;
-   response;
-   parâmetros;
-   chamada ao `apiClient`.

### Não deve conter

-   componentes React;
-   estados de tela;
-   loading visual;
-   regras de apresentação;
-   navegação;
-   Toast;
-   lógica de tabela.

------------------------------------------------------------------------

# 7. Por que usar `api` em vez de `services`

Para chamadas ao backend, o nome `api` deve ser preferido a `service`.

Evitar:

``` text
DebtService
FinancialService
FinanceService
```

`Service` é genérico e não informa imediatamente qual responsabilidade o
objeto possui.

Preferir:

``` text
debts.api.ts
charges.api.ts
dashboard.api.ts
```

Ao encontrar:

``` ts
debtsApi.getAll()
```

fica evidente que existe uma comunicação com uma API.

Essa escolha melhora a leitura e reduz navegação desnecessária.

------------------------------------------------------------------------

# 8. `hooks/` --- ligação entre React e API

Hooks representam o comportamento da feature no contexto React.

TanStack Query é o padrão para dados remotos no cliente. Hooks encapsulam queries e
mutations.

Exemplo:

``` ts
export function useDebts(tenantId: string | undefined, params: GetDebtsParams) {
  return useQuery({
    queryKey: ["debts", tenantId, params],
    enabled: Boolean(tenantId),
    queryFn: ({ signal }) => debtsApi.getAll(params, { signal }),
  });
}
```

Mutation:

``` ts
export function useCancelDebt(tenantId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: debtsApi.cancel,
    onSuccess: () => {
      queryClient.invalidateQueries({
        queryKey: ["debts", tenantId],
      });
    },
  });
}
```

### Hook pode conhecer

-   TanStack Query;
-   cache;
-   query keys;
-   mutations;
-   invalidação;
-   estados assíncronos;
-   API da feature.

### Hook não deve esconder o fluxo

Evitar:

``` text
useDebts
  ↓
FinanceManager
  ↓
DebtService
  ↓
DebtRepository
  ↓
Gateway
  ↓
HTTP
```

Preferir:

``` text
useDebts
  ↓
debtsApi.getAll
  ↓
HTTP
```

------------------------------------------------------------------------

# 9. `components/` dentro da feature

Componentes que conhecem conceitos específicos da feature devem
permanecer dentro dela.

Exemplo:

``` text
features/debts/components/
├── DebtsPage.tsx
├── DebtsTable.tsx
├── DebtFilters.tsx
├── DebtStatusChip.tsx
└── DebtDetails.tsx
```

Todos esses componentes conhecem o conceito de `Debt`.

Portanto, pertencem à feature `debts`.

### Regra

> Se um componente conhece uma entidade, regra ou comportamento
> específico de negócio, ele deve pertencer à feature correspondente.

------------------------------------------------------------------------

# 10. `src/components/` --- componentes compartilhados

`src/components` deve conter apenas componentes reutilizáveis entre
diferentes funcionalidades.

Exemplos:

``` text
components/
├── ui/
│   ├── SelectablePaginatedTable.tsx
│   ├── AppLoading.tsx
│   ├── PageHeader.tsx
│   └── ConfirmDialog.tsx
│
├── auth/
│   └── SessionGuard.tsx
│
└── finance/
    ├── FinanceShell.tsx
    └── FinanceMenu.tsx
```

Exemplo da diferença:

``` text
SelectablePaginatedTable
→ sabe renderizar uma tabela genérica.

DebtsTable
→ sabe renderizar débitos.
```

Portanto:

``` text
components/ui/SelectablePaginatedTable.tsx
features/debts/components/DebtsTable.tsx
```

Não mover um componente para `components/` apenas porque ele é utilizado
mais de uma vez dentro da mesma feature.

## 10.1 Padrão obrigatório de tabelas operacionais

**Decisão aprovada pelo usuário em 03/10/2026**, após validação funcional da seleção e da mudança de página na tela de débitos.

Todas as telas operacionais que apresentam registros em tabela devem usar `src/components/ui/SelectablePaginatedTable.tsx` como componente padrão. Para seleção entre páginas, utilizar `src/hooks/usePaginatedSelection.ts`. A regra vale para novas telas e para alterações nas tabelas operacionais existentes.

O componente compartilhado permanece independente de qualquer feature ou entidade de negócio. Cada tela fornece colunas, identificação estável dos registros, elegibilidade, paginação/ordenação controladas e ações. Consultas, cálculos e validações de negócio ficam na feature; wrappers como `DebtTable` apenas configuram o componente padrão.

A seleção deve preservar registros entre páginas e ordenações. O checkbox do cabeçalho atua somente nos registros elegíveis da página atual; resumos e ações usam a seleção completa. A tela controla a limpeza por filtros e contexto, incluindo mudança de empresa/sessão.

Necessidades adicionais de tabelas operacionais devem evoluir o componente compartilhado, evitando implementações paralelas de tabela, paginação ou seleção. Documentação de uso e contrato: [TABELA_SELECAO_PAGINADA.md](TABELA_SELECAO_PAGINADA.md).

------------------------------------------------------------------------

# 11. `services/api/` --- infraestrutura HTTP compartilhada

Essa pasta possui uma responsabilidade técnica muito específica:

``` text
services/api/
├── api-client.ts
└── api-error.ts
```

O `api-client.ts` pode ser responsável por:

-   Base URL;
-   headers comuns;
-   autenticação;
-   serialização;
-   interceptação técnica;
-   tratamento HTTP comum;
-   conversão padronizada de erros.

Exemplo conceitual:

``` ts
export const apiClient = createApiClient({
  baseURL: process.env.NEXT_PUBLIC_API_URL,
});
```

### Não colocar endpoints de negócio aqui

Evitar:

``` text
services/api/
├── api-client.ts
├── debts.ts
├── charges.ts
├── contracts.ts
├── fines.ts
└── customers.ts
```

Endpoints pertencem às features:

``` text
features/debts/api/debts.api.ts
features/charges/api/charges.api.ts
```

`services/api` sabe **como conversar via HTTP**.

`features/*/api` sabe **com qual endpoint conversar**.

------------------------------------------------------------------------

# 12. Repository no frontend

Não utilizar Repository apenas para encapsular uma chamada HTTP.

Evitar:

``` text
DebtsPage
    ↓
useDebts
    ↓
DebtRepository
    ↓
DebtApiService
    ↓
ApiClient
    ↓
GET /api/debts
```

Se o Repository apenas executa:

``` ts
getDebts() {
  return apiClient.get("/api/debts");
}
```

ele não está adicionando uma responsabilidade relevante; está apenas
aumentando a distância entre a tela e o backend.

Preferir:

``` text
DebtsPage
    ↓
useDebts
    ↓
debtsApi.getAll
    ↓
apiClient
    ↓
GET /api/debts
```

Repository só deve ser introduzido futuramente se existir uma
necessidade concreta que justifique essa abstração.

Não criar abstrações antecipadamente apenas por simetria com a
arquitetura do backend.

------------------------------------------------------------------------

# 13. Provider no frontend

O termo `Provider` deve ser reservado, preferencialmente, para React
Context Providers ou infraestrutura equivalente.

Exemplo:

``` text
providers/
├── AppProviders.tsx
├── QueryProvider.tsx
└── NotificationProvider.tsx
```

Não utilizar `Provider` no frontend para representar ERP ou integração
financeira.

O frontend não deve conhecer detalhes como:

``` text
ConcreteFinancialProvider
ErpProvider
FinancialProvider
```

Esses conceitos pertencem ao backend.

O backend é responsável por decidir qual integração financeira executará
determinada operação.

Para o frontend, o contrato continua sendo algo como:

``` text
POST /api/charges
GET /api/debts
GET /api/debts/{id}
```

O frontend não precisa saber qual provider financeiro o backend utilizou.

------------------------------------------------------------------------

# 14. Tipos

Tipos específicos devem permanecer próximos da feature.

Exemplo:

``` text
features/debts/types/debt.types.ts
```

Pode conter:

``` ts
export interface Debt {
  id: string;
  contractId: string;
  amount: number;
  dueDate: string;
  status: DebtStatus;
}
```

Evitar uma pasta global enorme:

``` text
src/types/
├── debt.ts
├── charge.ts
├── contract.ts
├── customer.ts
├── fine.ts
└── ...
```

Tipos globais somente devem existir quando forem verdadeiramente
compartilhados por várias partes independentes da aplicação.

------------------------------------------------------------------------

# 15. Schemas e validação

Schemas específicos da feature devem permanecer nela.

Exemplo:

``` text
features/charges/schemas/create-charge.schema.ts
```

Responsabilidades:

-   validação de formulário;
-   schemas Zod integrados ao React Hook Form nos formulários de negócio;
-   regras de formato relacionadas à entrada do usuário.

Não colocar validação específica de cobrança em uma pasta genérica
global.

------------------------------------------------------------------------

# 16. Regras de dependência

A direção esperada é:

``` text
app
 ↓
features
 ↓
services/api
```

Componentes compartilhados podem ser utilizados por `app` e `features`.

``` text
              components
               ↑      ↑
               │      │
app ───────→ features
               │
               ↓
          services/api
```

### Dependências desejadas

`app` pode conhecer:

-   `features`;
-   `components`;
-   `providers`;
-   `theme`.

`features` pode conhecer:

-   seus próprios módulos;
-   `components` compartilhados;
-   `services/api`;
-   bibliotecas externas necessárias.

`services/api` não deve conhecer:

-   telas;
-   componentes;
-   features;
-   regras visuais.

------------------------------------------------------------------------

# 17. Exemplo completo --- consulta de débitos

## Rota

``` text
src/app/(finance)/debts/page.tsx
```

``` tsx
import { DebtsPage } from "@/features/debts/components/DebtsPage";

export default function Page() {
  return <DebtsPage />;
}
```

## Componente

``` text
src/features/debts/components/DebtsPage.tsx
```

``` tsx
export function DebtsPage() {
  const { data, isLoading, error } = useDebts(tenantId, filters); // tenantId vem da sessão validada

  // composição da tela
}
```

## Hook

``` text
src/features/debts/hooks/useDebts.ts
```

``` ts
export function useDebts(tenantId: string | undefined, filters: GetDebtsParams) {
  return useQuery({
    queryKey: ["debts", tenantId, filters],
    enabled: Boolean(tenantId),
    queryFn: ({ signal }) => debtsApi.getAll(filters, { signal }),
  });
}
```

## API

``` text
src/features/debts/api/debts.api.ts
```

``` ts
export const debtsApi = {
  getAll: (filters: GetDebtsParams, options: { signal?: AbortSignal } = {}) =>
    apiClient.get<PaginatedResponse<Debt>>("/api/debts", {
      params: filters,
      signal: options.signal,
    }),
};
```

Resultado:

``` text
/debts
   ↓
DebtsPage
   ↓
useDebts
   ↓
debtsApi.getAll
   ↓
GET /api/debts
```

Esse deve ser o padrão de leitura esperado em todo o projeto.

------------------------------------------------------------------------

# 18. Exemplo ilustrativo --- cancelar débito (endpoint não implementado)

No componente:

``` ts
const cancelDebt = useCancelDebt(tenantId);
```

O desenvolvedor navega para:

``` text
features/debts/hooks/useCancelDebt.ts
```

e encontra:

``` ts
mutationFn: debtsApi.cancel
```

Navega para:

``` text
features/debts/api/debts.api.ts
```

e encontra:

``` ts
cancel: (id: string) =>
  apiClient.post(`/api/debts/${encodeURIComponent(id)}/cancel`)
```

Portanto:

``` text
Botão "Cancelar"
      ↓
useCancelDebt
      ↓
debtsApi.cancel
      ↓
POST /api/debts/{id}/cancel
```

Não deve ser necessário investigar uma cadeia de objetos para descobrir
isso.

------------------------------------------------------------------------

# 19. O que evitar

## 19.1 Chamadas HTTP em componentes

Evitar:

``` tsx
const response = await fetch("/api/debts");
```

dentro de:

``` text
DebtsPage.tsx
DebtsTable.tsx
DebtFilters.tsx
```

------------------------------------------------------------------------

## 19.2 Chamadas HTTP diretamente no hook

Embora seja tecnicamente possível, evitar:

``` ts
export function useDebts() {
  return useQuery({
    queryFn: () => apiClient.get("/api/debts"),
  });
}
```

Preferir:

``` ts
queryFn: () => debtsApi.getAll()
```

Isso mantém todos os endpoints da feature visíveis em um único local.

------------------------------------------------------------------------

## 19.3 Abstrações sem responsabilidade concreta

Evitar criar sequências como:

``` text
Component
 ↓
Hook
 ↓
Manager
 ↓
Service
 ↓
Repository
 ↓
Gateway
 ↓
Adapter
 ↓
ApiClient
```

Cada camada adicional precisa resolver um problema real.

Abstração sem responsabilidade clara reduz a rastreabilidade.

------------------------------------------------------------------------

## 19.4 `features/finance` como feature universal

Evitar concentrar toda a aplicação em:

``` text
features/finance/
```

Preferir capacidades:

``` text
features/
├── debts/
├── charges/
├── dashboard/
└── ...
```

------------------------------------------------------------------------

## 19.5 Componentes específicos em pasta global

Evitar:

``` text
components/finance/DebtsTable.tsx
components/finance/DebtFilters.tsx
```

Preferir:

``` text
features/debts/components/DebtsTable.tsx
features/debts/components/DebtFilters.tsx
```

------------------------------------------------------------------------

# 20. Convenção de nomes

A nomenclatura deve tornar a navegação previsível.

Para `debts`:

``` text
debts.api.ts

useDebts.ts
useDebt.ts
useCreateDebt.ts
useCancelDebt.ts

DebtsPage.tsx
DebtsTable.tsx
DebtDetails.tsx
DebtFilters.tsx

debt.types.ts
debt.schema.ts
```

API:

``` ts
debtsApi.getAll()
debtsApi.getById()
debtsApi.create()
debtsApi.cancel()
```

Hooks:

``` ts
useDebts()
useDebt()
useCreateDebt()
useCancelDebt()
```

Evitar nomes genéricos:

``` text
Manager
Handler
Helper
Processor
CommonService
FinanceService
GenericRepository
DataProvider
```

quando existir um nome que represente diretamente a responsabilidade.

## 20.1 Decisões de organização (Feature 30)

Decisões aprovadas pelo responsável em 04/11/2026. Ver `FEATURES/30 - Organização FrontEnd/`.

| Código | Decisão | Situação |
|---|---|---|
| D1 | Componentes React em `PascalCase.tsx`; hooks em `useAlgo.ts`; demais módulos TypeScript em `camelCase.ts`. Arquivos de rota mantêm os nomes exigidos pelo Next. | Aprovada |
| D2 | Chamadas ao backend ficam em `services/api/<domínio>/`, com `<domínio>Api.client.ts` (navegador, passa pelo BFF) e `<domínio>Api.server.ts` (servidor, `server-only`, chama a API .NET). Tipos de endpoint ficam em `services/api/<domínio>/`. Componentes e hooks não escrevem caminho de API. Cada endpoint compartilhado entre telas tem um único domínio dono. | Aprovada |
| D3 | `components/ui`, `hooks`, `utils`, `services`, `theme` e `providers` não importam de `features/`. Uma feature não importa componentes ou APIs de outra feature; código compartilhado sobe para o compartilhado. | Aprovada |
| D4 | Rotas de `app/api` exportam uma função por método HTTP (`GET`, `POST`, `PUT`, ...). Não usar função genérica `handle`. | Aprovada |
| D5 | Handlers locais recebem nome da ação completa (`handleCopyPix`); callbacks recebidos por props usam `onAlgo`. | Aprovada |
| D6 | Componente acima de cerca de 300 linhas deve ser revisado para divisão. Gatilho de revisão, não limite rígido. | Aprovada |
| D7 | Testes ficam ao lado do código testado. Helpers compartilhados de teste ficam em `src/test/`. Fixtures não ficam em código de produção. | Aprovada |
| D8 | Formatação com Prettier (`.prettierrc.json`) e `.editorconfig` (UTF-8, LF, 2 espaços em TS/TSX/JS/JSON/CSS), aplicada em alteração isolada. Todo o `src` segue o padrão. | Aprovada |
| D9 | Um único componente de status em `components/ui`, que recebe rótulo e tom; o mapeamento de status fica em cada domínio. | Aprovada |
| D10 | Módulos que só exportam funções não usam `"use client"`; a diretiva marca apenas a fronteira de componentes. | Aprovada |

Nota: a estrutura de chamadas ao backend segue a D2 (`services/api/debts/debtsApi.client.ts`), e não o exemplo `debts.api.ts` desta seção.

## 20.2 Estilos e tokens (Feature 30, fase 7)

- Toda estilização fica em `src/theme/`: `designTokens.ts` (tokens), `theme.ts` (tema do MUI e `CssBaseline`), `checkoutTheme.ts` e `checkoutStyles.ts` (checkout), `layout.ts` (espaçamentos e grades) e `styles/` (CSS globais e do loader).
- Não há cor, sombra, transparência ou hexadecimal literal fora de `src/theme/`. Novos valores entram como token com nome pelo papel na interface.
- Telas do app usam `financeTokens.colors` e `financeTokens.overlays`/`shadows`. O checkout público usa `checkoutColors` (alias de `financeTokens.checkoutPalette`).
- Bordas e textos compostos usam template literal com o token, ex.: `` `1px solid ${financeTokens.colors.listDivider}` ``.

------------------------------------------------------------------------

# 21. Regra de proximidade

Código deve ficar o mais próximo possível da funcionalidade à qual
pertence.

Antes de colocar algo em uma pasta compartilhada, perguntar:

> Isso é realmente utilizado por funcionalidades independentes?

Se não, manter dentro da feature.

Exemplo:

``` text
DebtStatusChip
```

Mesmo que seja utilizado em cinco telas relacionadas a débito, continua
pertencendo a:

``` text
features/debts/components/
```

Não precisa virar componente global.

------------------------------------------------------------------------

# 22. Regra para novas features

Ao criar uma nova funcionalidade, iniciar pela feature correspondente.

Exemplo:

``` text
features/fines/
```

Adicionar apenas o necessário:

``` text
features/fines/
├── api/
│   └── fines.api.ts
├── hooks/
│   └── useFines.ts
├── components/
│   ├── FinesPage.tsx
│   └── FinesTable.tsx
└── types/
    └── fine.types.ts
```

Se não existir formulário, não criar `schemas/`.

Se não existir API, não criar `api/`.

Se não existir transformação, não criar `mappers/`.

A estrutura deve crescer conforme necessidades concretas.

------------------------------------------------------------------------

# 23. Regra para mappers

Mapper não deve ser criado automaticamente.

Utilizar somente quando existir diferença relevante entre o contrato da
API e o modelo utilizado pela interface.

Exemplo:

``` text
features/debts/mappers/debt.mapper.ts
```

Caso API e frontend utilizem praticamente a mesma estrutura, consumir o
contrato diretamente é preferível a criar transformação artificial.

------------------------------------------------------------------------

# 24. Tratamento de loading

O componente único é `src/components/ui/LoadingState.tsx`. O modo `blocking`
usa um overlay de viewport com backdrop claro e spinner, impede interações por
ponteiro e teclado e mantém o conteúdo montado por trás. A carga inicial,
consultas iniciadas pelo usuário (filtros, ordenação, paginação, atualização,
detalhes e retry) e mutações iniciadas pelo usuário bloqueiam a tela. Cada
tela agrega operações simultâneas em um único overlay, com mensagem curta em
português. Sucesso ou erro libera a interface; falhas mostram erro recuperável.

Polling, invalidação/refetch automático de cache e demais atualizações em
segundo plano nunca bloqueiam; preservam os dados existentes e podem exibir
indicador local discreto. Sugestões de autocomplete mantêm spinner inline no
campo. Progresso de tarefa de negócio, como Job em execução, permanece no
componente contextual e não usa overlay. O modo `page` é reservado ao fallback
Next antes da montagem da tela, é responsivo e usa a identidade visual comum.

O padrão global de loading já definido pelo projeto deve continuar
centralizado.

Os componentes da feature podem consumir os estados:

``` ts
isLoading
isPending
isFetching
```

mas não devem implementar novas estratégias globais de loading de forma
isolada.

A infraestrutura visual reutilizável permanece em componentes
compartilhados.

------------------------------------------------------------------------

# 25. Tratamento de erros

Erros técnicos HTTP podem ser normalizados em:

``` text
services/api/api-error.ts
```

A feature decide como apresentar o erro ao usuário.

Exemplo:

``` text
HTTP 404
    ↓
api-error
    ↓
useDebt
    ↓
DebtDetails
    ↓
mensagem apropriada
```

Não colocar mensagens específicas de débito dentro da infraestrutura
HTTP.

------------------------------------------------------------------------

# 26. Autenticação

Autenticação é uma feature transversal com características próprias.

Pode permanecer em:

``` text
features/auth/
```

e componentes globais de proteção podem permanecer em:

``` text
components/auth/
```

Exemplo:

``` text
features/auth/
├── actions/
├── types/
└── ...

components/auth/
└── SessionGuard.tsx
```

O cliente HTTP pode consumir a sessão/token quando necessário, mas não
deve implementar regras de interface de autenticação.

------------------------------------------------------------------------

# 27. O frontend não replica a Clean Architecture do backend

O backend do Yalla Finance possui necessidades diferentes:

``` text
API
 ↓
Application
 ↓
Domain
 ↓
Infrastructure
 ↓
Providers externos
```

Não é necessário reproduzir essa arquitetura literalmente no frontend.

No frontend, a prioridade é:

``` text
Route
 ↓
Feature
 ↓
Hook
 ↓
API
 ↓
Backend
```

O backend protege o domínio e abstrai integrações externas.

O frontend organiza experiência, estado da interface e consumo do
contrato HTTP.

São problemas diferentes e podem utilizar estruturas diferentes.

------------------------------------------------------------------------

# 28. Checklist para Pull Request

Ao revisar código novo, verificar:

-   [ ] A rota está simples?
-   [ ] A funcionalidade está dentro da feature correta?
-   [ ] O componente conhece alguma entidade de negócio? Se sim, está
    dentro da feature?
-   [ ] Existe `fetch` ou Axios diretamente em componente?
-   [ ] Existe `fetch` ou Axios diretamente em `page.tsx` sem
    justificativa?
-   [ ] Toda chamada de negócio está em `features/<feature>/api`?
-   [ ] O hook aponta diretamente para a API da feature?
-   [ ] É possível identificar facilmente qual endpoint está sendo
    chamado?
-   [ ] Existe Repository que apenas repassa uma chamada HTTP?
-   [ ] Existe Provider que não é realmente um React Provider?
-   [ ] Foi criada alguma abstração que apenas encaminha a chamada para
    outro objeto?
-   [ ] Tipos específicos estão próximos da feature?
-   [ ] Componentes realmente compartilhados estão em `components/`?
-   [ ] Componentes específicos permaneceram dentro da feature?
-   [ ] O frontend conhece algum detalhe de um provider financeiro ou ERP? Se sim,
    revisar.
-   [ ] Os nomes dos arquivos deixam a responsabilidade evidente?
-   [ ] A nova estrutura reduziu ou aumentou a quantidade de navegação
    necessária?

------------------------------------------------------------------------

# 29. Critério de rastreabilidade

Uma implementação está adequada quando um desenvolvedor consegue
responder rapidamente:

1.  Qual rota apresenta essa funcionalidade?
2.  Qual componente representa a tela?
3.  Qual hook carrega ou altera os dados?
4.  Qual função da API é utilizada?
5.  Qual verbo HTTP é utilizado?
6.  Qual endpoint do backend é chamado?

Exemplo esperado:

``` text
Tela:
app/(finance)/debts/page.tsx

↓ componente

features/debts/components/DebtsPage.tsx

↓ hook

features/debts/hooks/useDebts.ts

↓ API

features/debts/api/debts.api.ts

↓ endpoint

GET /api/debts
```

Se para responder essas perguntas for necessário navegar por diversos
objetos intermediários, a implementação deve ser reavaliada.

------------------------------------------------------------------------

# 30. Princípios finais

## Simplicidade acima de abstração

Não adicionar uma camada apenas porque ela existe em outro projeto ou no
backend.

## Responsabilidade explícita

O nome e a localização de um arquivo devem indicar sua finalidade.

## Organização por funcionalidade

Código relacionado deve permanecer junto.

## Endpoints visíveis

As APIs de cada feature devem funcionar como um mapa simples das
integrações daquela funcionalidade com o backend.

## Poucas etapas para rastrear

O fluxo ideal é:

``` text
Component
 ↓
Hook
 ↓
Feature API
 ↓
Endpoint
```

## Infraestrutura não conhece negócio

`apiClient`, tema, providers globais e componentes genéricos não devem
conhecer Debts, Charges, providers concretos ou outras regras específicas.

## Frontend não conhece o ERP

Providers financeiros concretos são detalhes internos do backend.

## Criar abstrações somente quando existir necessidade

Repository, Mapper, Adapter, Gateway ou Manager só devem existir quando
resolverem um problema concreto e identificável.

------------------------------------------------------------------------

# 31. Regra oficial para implementação

> No Yalla Finance, o frontend deve ser organizado por feature e
> otimizado para rastreabilidade. Rotas apenas compõem funcionalidades.
> Componentes específicos permanecem dentro de suas features. Hooks
> concentram a integração da feature com o estado React/TanStack Query.
> Toda chamada HTTP de negócio deve estar declarada explicitamente em
> `features/<feature>/api`. `services/api` deve conter apenas
> infraestrutura HTTP compartilhada. Não devem ser introduzidos
> Repository, Provider, Gateway, Adapter, Manager ou outras camadas que
> apenas encaminhem chamadas sem adicionar responsabilidade concreta. A
> partir de uma tela, deve ser possível identificar o endpoint do
> backend seguindo diretamente `Component → Hook → API → Endpoint`.

------------------------------------------------------------------------

## Resultado esperado

A estrutura deve permitir que qualquer desenvolvedor abra o projeto e
pense:

``` text
"Estou na tela de débitos."
        ↓
"Ela usa DebtsPage."
        ↓
"DebtsPage usa useDebts."
        ↓
"useDebts chama debtsApi.getAll."
        ↓
"debtsApi.getAll chama GET /api/debts."
```

Sem investigação desnecessária.

Esta versão final é a referência principal para decisões de
organização do frontend do Yalla Finance, conforme a precedência definida abaixo.

------------------------------------------------------------------------

# 32. Precedência e relação com a arquitetura global

Esta versão final substitui exclusivamente as regras de frontend explicitamente relacionadas na tabela abaixo.
`ARQUITETURA.MD` continua como contrato global para backend, autorização, isolamento de tenant, domínio, persistência e integrações externas. Em caso de diferença especificamente sobre organização do frontend, prevalece este documento.

| Tema | Decisão final |
|---|---|
| Chamadas por feature | `features/<feature>/api/*.api.ts` substitui `features/<feature>/services` |
| Infraestrutura HTTP | `services/api` substitui `lib/api`; contrato comum de transporte, com instâncias/configurações separadas por ambiente e sem implementações concorrentes da mesma responsabilidade |
| Endpoints | Declarados nos arquivos de API de cada feature, sem catálogo global obrigatório |
| Contratos específicos | Permanecem na feature; contratos transversais ficam em módulo compartilhado apenas quando necessário |
| Dados remotos no cliente | TanStack Query é o padrão; Context não substitui seu cache |
| Formulários de negócio | React Hook Form + Zod, incluindo resolver e valores iniciais |
| Transporte HTTP | Axios é mantido como escolha do contrato global; a migração deve preservar o comportamento do cliente atual |
| Sessão | Sessão protegida no servidor substitui persistência de token em localStorage ou cookie legível pelo JavaScript |
| Providers | Query, tema, notificações e sessão quando necessário; tenant autenticado deriva da sessão validada |

Axios não é requisito do Next.js. Sua adoção é uma decisão do projeto e
não deve criar uma camada paralela ao cliente HTTP existente.

Cliente HTTP único significa um contrato consistente de transporte,
serialização e normalização de erros, não uma instância compartilhada entre
navegador e servidor. Instâncias/configurações específicas de cada ambiente
são permitidas e devem preservar esse contrato.

No navegador, chamadas autenticadas usam a origem Next. No servidor,
o transporte usa a URL privada da API .NET e aplica o contexto autenticado
por request. Não alterar o Bearer em defaults ou interceptors de uma instância
global compartilhada entre usuários. Credenciais de uma sessão não podem
afetar requisições de outra sessão.

Uma alteração futura deste contrato deve atualizar os documentos afetados
e registrar explicitamente a regra substituída. Não implementar padrões
contraditórios com base em documentos diferentes.

# 32.1 Regra definitiva — simplicidade e BFF

A arquitetura final mantém a rastreabilidade como critério principal. Uma fronteira Next (Route Handler, Server Action ou BFF) **não é obrigatória para toda chamada**. Ela somente deve existir quando possuir responsabilidade concreta, como proteger sessão/credenciais, manter Bearer token fora do browser ou resolver uma necessidade real de execução no servidor.

Preservar método, URL, payload e resposta não torna a fronteira redundante quando ela valida sessão e mantém credenciais fora do navegador.

Quando houver caminhos diferentes para browser e servidor, preferir nomenclatura explícita:

```text
features/debts/api/
├── debts.client.ts
└── debts.server.ts
```

Se não houver necessidade dessa separação, manter apenas:

```text
features/debts/api/debts.api.ts
```

Não criar os dois caminhos antecipadamente.

# 33. Fronteiras de execução no Next.js

O fluxo com hooks das seções anteriores representa consultas e mutations
executadas no cliente. Ele não é obrigatório para código executado no servidor.

Fluxos permitidos:

```text
Client Component → hook → API da feature → cliente HTTP → backend
Server Component → API da feature para servidor → cliente HTTP → backend
Server Action → API da feature para servidor → cliente HTTP → backend
```

`app/page.tsx` pode carregar dados por uma função da API da feature e
compor o resultado. A regra continua sendo não espalhar URLs e detalhes
de transporte pelas rotas. Server Components não utilizam hooks de cliente.

- Usar `use client` nos pontos que necessitam de estado, eventos ou APIs do navegador.
- Módulos de sessão, credenciais e transporte exclusivos do servidor devem usar `server-only`.
- Nunca passar token como prop para Client Components.
- Providers de cliente devem ficar no menor escopo necessário.
- Se houver hidratação de React Query, criar cache de servidor por request,
  sem compartilhar dados de sessão entre usuários.

O alvo para requisições autenticadas do navegador é uma fronteira Next
de mesma origem, por Route Handler ou Server Action. Ela utiliza a sessão
protegida e encaminha o Bearer token à API .NET. A API .NET continua sendo
responsável pela autorização e pela identidade do tenant.

## Limites da fronteira Next

A fronteira Next deve validar a sessão em cada operação protegida e
encaminhar somente operações, métodos e destinos explicitamente definidos
no servidor. Não aceitar URL ou host arbitrário fornecido pelo navegador
nem funcionar como proxy aberto.

Encaminhar somente headers permitidos pelo contrato da operação, como
`Content-Type` e `Idempotency-Key` quando aplicáveis. Não copiar indiscriminadamente
cookies, headers ou credenciais recebidos. O Bearer encaminhado à API .NET
deve ser obtido exclusivamente da sessão validada no servidor; identidade
e tenant continuam sujeitos à autorização da API .NET.

Respostas autenticadas não devem entrar em cache compartilhado entre usuários.
O padrão para essas respostas HTTP é `Cache-Control: private, no-store`,
sem persistência em cache compartilhado do servidor. Qualquer exceção exige
definição explícita de usuário, tenant, permissões relevantes e invalidação.
Essa regra não impede o cache de React Query isolado por contexto, conforme
a seção 35. Mutações continuam sujeitas à proteção contra CSRF da seção 36.

Os arquivos da API da feature devem distinguir os caminhos de execução somente quando necessário. Na arquitetura final, preferir `debts.client.ts` e `debts.server.ts` quando os dois ambientes realmente existirem.
Uma fronteira Next deve ser explícita na rastreabilidade:

```text
useDebts → debtsApi.getAll → Route Handler Next → debtsServerApi.getAll
         → GET /api/debts na API .NET
```

Essa etapa adicional tem responsabilidade concreta de proteção da sessão;
não é uma abstração que apenas renomeia outra função.

> Regra definitiva: chamadas públicas podem acessar diretamente a API conforme seu contrato. Chamadas autenticadas do navegador utilizam a fronteira Next enquanto a API .NET exigir Bearer token mantido no servidor. Server Components e Server Actions acessam a API .NET pelo módulo de servidor, sem passar por um Route Handler interno. Não criar uma fronteira adicional sem responsabilidade concreta.


# 34. Estado remoto, estado de tela e rascunhos

| Estado | Responsável |
|---|---|
| Débitos, indicadores e catálogos | TanStack Query no cliente ou leitura no servidor |
| Campos e erros de formulário | React Hook Form + Zod |
| Filtros, seleção e abertura de diálogos | Estado local; URL quando houver benefício para navegação |
| Rascunho entre etapas de cobrança | `ChargeDraftProvider` no escopo de `charges` |
| Cobrança criada, status financeiro e entrega | Backend como fonte de verdade |

`ChargeDraftProvider` pode permanecer em `features/charges` porque
compartilha um rascunho entre criação, revisão e confirmação. Não deve
carregar todos os débitos, funcionar como cliente HTTP ou armazenar o
histórico autoritativo de cobranças.

Definir comportamento para recarregamento e acesso direto às etapas:
sem rascunho válido, voltar à etapa inicial com mensagem apropriada.
Persistência do rascunho requer decisão explícita sobre dados sensíveis;
nunca persistir token ou usar storage como prova de autorização.

Storage temporário, se necessário, deve ser versionado, identificado por
usuário e tenant e limpo no logout ou mudança de contexto. Cobranças não
podem depender apenas de `sessionStorage` para serem consultadas.

A consulta de cobrança por ID depende de endpoint autorizado no backend.
Se ele não existir, registrar essa dependência antes de migrar a tela de
sucesso; não simular persistência definitiva no navegador.

# 35. Query keys e isolamento de cache

As variáveis que distinguem os dados devem participar da query key.
Tenant vem da sessão autenticada, nunca de um campo arbitrário enviado
como fonte de autoridade ao backend.

Exemplo completo de hook no cliente:

```ts
export function useDebts(tenantId: string | undefined, filters: GetDebtsParams) {
  return useQuery({
    queryKey: ["debts", tenantId, filters],
    enabled: Boolean(tenantId),
    queryFn: ({ signal }) => debtsApi.getAll(filters, { signal }),
  });
}
```

Os exemplos abreviados anteriores pressupõem `tenantId` obtido da sessão
validada. Incluir usuário ou escopo adicional quando isso alterar a resposta;
nunca incluir o token na key.

- Criar um QueryClient estável no cliente.
- Cancelar queries e limpar o cache no logout, expiração ou troca de contexto.
- Separar cache por tenant não substitui limpeza entre usuários do mesmo tenant.
- Bloquear consultas autenticadas até a sessão estar pronta.
- Propagar `AbortSignal` ao transporte.
- Definir `staleTime`, política de refetch e retry; não repetir mutations
  financeiras automaticamente.
- Invalidar dados afetados após mutation: cobrança, débitos e dashboard,
  sempre no contexto correto.

## 35.1 Padronização de query keys

Para reduzir esquecimento de tenant/contexto, cada feature pode centralizar a construção das próprias query keys. Essa abstração é permitida porque possui responsabilidade concreta de identidade e invalidação de cache; ela não deve esconder a API.

```ts
export const debtKeys = {
  all: (tenantId: string) => ["debts", tenantId] as const,
  list: (tenantId: string, filters: DebtFilters) =>
    ["debts", tenantId, "list", filters] as const,
  detail: (tenantId: string, id: string) =>
    ["debts", tenantId, "detail", id] as const,
};
```

# 36. Autenticação e encerramento da sessão

A sessão deve ser validada no servidor, próxima ao acesso protegido.
Presença de cookie ou perfil desserializado não prova autenticação.
`SessionGuard` pode apoiar a experiência no cliente, mas não constitui
fronteira de segurança.

- Cookie de sessão com `HttpOnly`, `SameSite`, expiração e `Secure` em produção HTTPS.
- Preferir identificador opaco de sessão; credenciais de backend permanecem no servidor.
- Não armazenar Bearer token em localStorage, sessionStorage ou cookie acessível ao JavaScript.
- Login, logout e mudanças autenticadas devem validar entrada e proteger
  a fronteira de cookie contra CSRF, incluindo verificação de origem conforme o mecanismo escolhido.
- Em 401, encerrar sessão e limpar cache, rascunho e storage do contexto anterior.
- Em 403, apresentar acesso negado, preservando distinção entre falta de permissão e sessão expirada.
- Infraestrutura HTTP normaliza erros; a camada de autenticação decide
  encerramento e navegação. Evitar redirecionamentos de UI dentro do transporte genérico.

# 37. Contratos HTTP, endpoints e resultados financeiros

O cliente HTTP compartilhado deve retornar diretamente o corpo tipado,
e não `AxiosResponse<T>`. Assim, hooks e componentes não dependem do transporte.

```ts
apiClient.get<T>(path, options): Promise<T>
apiClient.post<T>(path, body, options): Promise<T>
```

Estas assinaturas são conceituais. A implementação deve suportar params,
headers e cancelamento; retornar `undefined` em respostas sem corpo;
e normalizar erros com status, código, mensagem, erros de campo e
identificador de rastreio quando disponível. TypeScript não valida JSON
em runtime: contratos críticos podem exigir schema de resposta.

Os endpoints reais do backend usam o prefixo `/api`, como
`GET /api/debts`, `GET /api/debts/{id}`, `POST /api/charges` e `GET /api/charges/{id}`.
Os exemplos anteriores com `/debts`
e `/charges` são abreviados; cancelamento e criação de débito são
ilustrativos, não afirmações de endpoints existentes.

Ao adicionar uma chamada, confirmar rota, verbo, payload e resposta no
controller/contrato real. Documentar separadamente URL Next e URL .NET
quando houver encaminhamento pela fronteira de sessão.

## Resultado parcial de cobrança

Estado da entidade financeira e estado do processamento de integração são conceitos distintos. Nunca reutilizar `DebtStatus` como estado técnico de integração.

```text
DebtStatus
├── Open
├── InCollection
├── Paid
└── Canceled

IntegrationProcessingStatus
└── segue o contrato real do backend
```

O frontend não deve inventar estados técnicos; deve representar o contrato fornecido pelo backend.

Falha HTTP e resultado de integração financeira são situações distintas.
Uma resposta de criação pode conter cobrança existente no Yalla e
integração `Completed`, `Rejected` ou `Indeterminate`.

- Preservar ID e resultado recebido em todos esses estados.
- Não descartar a cobrança lançando apenas um erro genérico após HTTP 200.
- Mostrar sucesso completo apenas quando o contrato confirmar a conclusão.
- Para rejeição, mostrar a cobrança e orientar a ação permitida pelo backend.
- Para resultado indeterminado, oferecer consulta/reconciliação antes de novo envio.
- Manter a mesma chave de idempotência ao repetir a mesma operação;
  uma nova intenção deve ser distinguida conforme contrato do backend.
- Não executar retry automático de POST financeiro.

O frontend não escolhe ERP nem implementa regras de integração. Pode
exibir identificador externo ou nome do provedor quando isso ajudar o
usuário, usando informação do contrato. A neutralidade arquitetural não
exige ocultar informação operacional útil.

# 38. Dependências entre features e catálogos

Features podem consumir contratos e APIs públicas de outras features
quando houver responsabilidade clara. Não importar componentes internos,
estado privado ou hooks indiscriminadamente; não criar ciclos.

Clientes e catálogos devem ter proprietário explícito, por exemplo:

```text
features/customers/api/customers.api.ts
features/catalogs/api/categories.api.ts
features/catalogs/api/payment-methods.api.ts
```

Essas pastas só serão criadas quando a migração exigir suas responsabilidades.
`charges` pode consumir esses módulos sem copiar endpoints.

Uma função de API representa uma operação HTTP identificável. Composição
de várias consultas deve permanecer explícita no hook ou em função da
feature com responsabilidade concreta. Conversão entre DTO e formulário
pode usar mapper quando necessária; não transformar `charges.api.ts`
em um novo repository universal.

`api-client` recebe contexto autenticado da infraestrutura apropriada ao
ambiente; não importa componentes de auth ou regras de negócio. Componentes
transversais de sessão e layout são exceções explícitas à regra de componentes
genéricos, sem tornar `components/` um depósito de componentes de negócio.

# 39. Loading, formulários e critérios de migração

Aplicar em todas as telas o padrão obrigatório descrito na seção 24:
carregamentos iniciais e ações manuais bloqueiam a viewport; polling e
atualizações automáticas não. `loading.tsx` cobre a transição da rota e não
substitui o estado de consultas iniciadas no navegador. Autocomplete mantém
spinner inline; erros sempre liberam o bloqueio e oferecem retry quando cabível.

Cada consulta apresenta estado inicial, vazio, erro recuperável e atualização
conforme sua responsabilidade, seguindo o bloqueio inicial e manual definido
na seção 24. `loading.tsx` não substitui o loading de consultas iniciadas em
efeitos no navegador.

Formulários de negócio utilizam React Hook Form e Zod; validações no
frontend apoiam a experiência e não substituem a validação do backend.
Usar validação e conversão de moeda/data consistentes com o contrato.
Regras de domínio continuam explícitas e testáveis, sem forçar toda
regra a caber em um schema de campos.

A migração deve ocorrer por funcionalidade, preservando rotas e comportamento:

1. Consolidar contrato documental e dependências do backend.
2. Corrigir sessão e isolamento de contexto.
3. Estabelecer cliente HTTP único e QueryProvider.
4. Migrar dashboard e débitos, com loading e erro locais.
5. Migrar catálogos e formulários de cobrança.
6. Separar rascunho de cobrança e preservar resultados parciais.
7. Remover o provider/repository antigos após migração dos consumidores.

Mover arquivos e trocar transporte não deve alterar regras financeiras,
payloads ou identidade da operação sem revisão específica.

## Checklist arquitetural adicional

- [ ] Client e Server estão separados sem expor credenciais?
- [ ] Existe Route Handler/BFF? Se sim, ele resolve uma responsabilidade concreta além de encaminhar HTTP?
- [ ] Quando existem caminhos client/server, a nomenclatura deixa a fronteira explícita?
- [ ] Query keys identificam tenant e demais contextos relevantes?
- [ ] Logout e troca de contexto limpam dados da sessão anterior?
- [ ] Há tratamento local de erro, vazio e loading?
- [ ] Cobrança parcialmente concluída preserva ID e estado financeiro?
- [ ] Retry e idempotência respeitam o contrato da operação?
- [ ] Endpoint e payload correspondem ao backend existente?
- [ ] Compartilhamento entre features tem proprietário e não cria ciclos?
- [ ] A navegação entre etapas e o recarregamento têm comportamento definido?
- [ ] Lint, TypeScript, testes pertinentes e build foram executados e seus resultados registrados?

Testes devem verificar comportamentos relevantes: isolamento entre sessões,
erro de carregamento recuperável, resultado parcial, idempotência e fluxo
de revisão/confirmação. Falha de infraestrutura de teste deve ser registrada
separadamente de falha de comportamento; execução incompleta não comprova aprovação.

# 40. Regra arquitetural consolidada — APROVADA

O fluxo deve permanecer o menor possível.

### Cliente

```text
Route → Feature Component → Hook → Feature API → Endpoint
```

### Servidor

```text
Server Component / Server Action → Feature Server API → Endpoint
```

### Quando uma fronteira Next for realmente necessária

```text
Feature Component
  ↓
Hook
  ↓
Feature Client API
  ↓
Next Session Boundary
  ↓
Feature Server API
  ↓
.NET Endpoint
```

Nenhuma etapa adicional deve existir sem responsabilidade objetiva.

Antes de criar `Repository`, `Service`, `Manager`, `Gateway`, `Adapter`, `Mapper`, `Provider`, `Context`, `Route Handler` ou `Server Action`, responder:

1. Qual responsabilidade concreta esse objeto possui?
2. Essa responsabilidade já pertence a outro objeto?
3. Ele protege uma fronteira real ou reduz duplicação relevante?
4. Ele facilita ou dificulta encontrar o endpoint?
5. Se removido, algum comportamento real seria perdido?

Evitar camadas intermediárias que não acrescentem responsabilidade além daquela já exercida pela API da feature. Funções como debtsApi.getAll são justificadas por centralizar endpoint, contrato e transporte. Hooks são justificados por cache e estado React; fronteiras Next, por sessão e proteção das credenciais. Não eliminar essas responsabilidades apenas porque seus métodos encaminham parâmetros.

## Resultado esperado

Ao abrir uma tela do Yalla Finance, o desenvolvedor deve conseguir responder rapidamente:

```text
Onde estou?
  ↓
Qual feature implementa isso?
  ↓
Qual hook/operação executa?
  ↓
Qual API da feature é chamada?
  ↓
Existe uma fronteira Next? Por quê?
  ↓
Qual endpoint real da API .NET é executado?
```

A arquitetura final prioriza **responsabilidades claras, proteção explícita das fronteiras autenticadas e rastreabilidade direta entre interface e backend**.

## Como ler e implementar os exemplos

Os snippets são conceituais. Componentes com hooks devem declarar `use client`;
`tenantId` vem da sessão validada e `filters` do estado da tela. Imports e estado
omitidos devem ser completados na implementação, sem inventar contexto de sessão.

Para chamadas protegidas do navegador, `apiClient` usa a origem Next. O módulo
`*.server.ts` usa a URL da API .NET configurada no servidor e adiciona o Bearer
da sessão. Não configurar o navegador para acessar diretamente endpoints
protegidos da API .NET neste modelo de sessão.

A mesma rota `/api/debts` pode existir nas duas origens; documentar explicitamente
URL Next e endpoint .NET. O exemplo de `NEXT_PUBLIC_API_URL` aplica-se somente
às chamadas públicas diretas. A URL privada do backend deve usar configuração
exclusiva do servidor.

Criação e cancelamento de débito são exemplos futuros, não endpoints existentes.
Eles não autorizam criar rotas ou botões sem contrato de backend aprovado.

# 41. Regras obrigatórias para o Codex

Este documento deve ser tratado pelo Codex como **contrato arquitetural**, e não como sugestão de organização.

Ao implementar ou refatorar o frontend, o Codex deve preservar as regras abaixo.

## 41.1 Não reinterpretar a arquitetura

O Codex não deve introduzir automaticamente:

```text
Repository
Service
Manager
Gateway
Adapter
Provider global
Clean Architecture completa no frontend
arquitetura hexagonal no frontend
BFF para toda chamada
catálogo global obrigatório de endpoints
pasta global de tipos de negócio
```

Esses elementos só podem existir quando houver responsabilidade concreta e identificável, conforme as regras deste documento.

## 41.2 Não inventar contratos de backend

Antes de criar qualquer chamada HTTP, o Codex deve confirmar no código real do backend:

1. controller;
2. rota;
3. verbo HTTP;
4. request;
5. response;
6. autorização;
7. comportamento esperado em erro.

Se o endpoint não existir, registrar a dependência e **não criar uma implementação fictícia**.

Exemplos antigos de criação ou cancelamento de débito continuam sendo apenas exemplos arquiteturais e não requisitos funcionais.

## 41.3 Não alterar regra de negócio durante refatoração estrutural

Mover arquivos entre pastas, substituir Provider por TanStack Query, separar client/server ou consolidar o transporte HTTP não autoriza alteração de:

- regra financeira;
- payload;
- status;
- fluxo de cobrança;
- permissão;
- regra de tenant;
- comportamento de integração.

Mudança funcional deve ser tratada separadamente.

## 41.4 Não antecipar abstrações

Não criar antecipadamente:

```text
schemas/
mappers/
context/
query-keys/
*.client.ts
*.server.ts
Route Handler
Server Action
```

se a feature ainda não possui necessidade concreta dessas responsabilidades.

## 41.5 Preservar a rastreabilidade

Toda implementação deve permitir identificar:

```text
Tela
  ↓
Feature
  ↓
Hook/operação
  ↓
API da feature
  ↓
[fronteira Next, somente quando necessária]
  ↓
Endpoint real da API .NET
```

Nenhum objeto intermediário deve existir apenas para encaminhar a mesma operação.

## 41.6 Preservar separação de responsabilidades

O Codex deve respeitar:

```text
app/
→ navegação, layout, composição e fronteiras Next

features/
→ funcionalidades de negócio do frontend

features/<feature>/api/
→ endpoints e contratos HTTP da feature

features/<feature>/hooks/
→ React/TanStack Query

features/<feature>/components/
→ UI específica da feature

components/
→ componentes realmente compartilhados

services/api/
→ infraestrutura HTTP genérica

providers/
→ React Providers globais legítimos
```

## 41.7 Não expor credenciais

O Codex nunca deve:

- armazenar Bearer token em `localStorage`;
- armazenar Bearer token em `sessionStorage`;
- disponibilizar token em cookie legível por JavaScript;
- passar token como prop para Client Component;
- utilizar variável `NEXT_PUBLIC_*` para segredo do backend.

A URL privada e as credenciais de backend permanecem em contexto server-only.

## 41.8 Não confundir estado financeiro com estado técnico

O Codex deve preservar como conceitos distintos:

```text
DebtStatus
├── Open
├── InCollection
├── Paid
└── Canceled
```

versus o estado técnico de processamento de integração fornecido pelo backend.

Nunca criar mapping que converta silenciosamente um estado técnico em `DebtStatus`.

## 41.9 Não conhecer implementação interna do ERP

O frontend não deve tomar decisões com base em:

```text
ConcreteFinancialIntegrationService
ConcreteFinancialProvider
ProviderResolver
IExternalFinancialProvider
```

Informações de provider só podem ser exibidas quando fizerem parte do contrato HTTP e tiverem utilidade de UX/operação.

---

# 42. Definition of Done arquitetural

Uma feature está arquiteturalmente concluída quando:

- [ ] está localizada na feature correta;
- [ ] a rota permanece simples;
- [ ] componentes de negócio estão dentro da própria feature;
- [ ] componentes compartilhados são realmente compartilhados;
- [ ] dados remotos no cliente utilizam TanStack Query;
- [ ] formulários utilizam React Hook Form + Zod quando aplicável;
- [ ] toda chamada HTTP de negócio está na API da feature;
- [ ] `services/api` continua sem conhecer negócio;
- [ ] o endpoint real do backend é facilmente rastreável;
- [ ] nenhuma abstração intermediária existe sem responsabilidade concreta;
- [ ] query keys identificam o contexto necessário;
- [ ] token nunca participa de query key;
- [ ] cache e rascunhos são tratados corretamente em logout/troca de contexto;
- [ ] `AbortSignal` é propagado quando aplicável;
- [ ] mutations financeiras não possuem retry automático indevido;
- [ ] idempotência respeita o contrato do backend;
- [ ] estado financeiro não é confundido com estado de integração;
- [ ] loading, vazio, erro e sucesso foram tratados;
- [ ] `401` e `403` mantêm comportamentos distintos;
- [ ] chamadas autenticadas do browser protegem o Bearer no servidor;
- [ ] Server Components não atravessam Route Handler interno sem necessidade;
- [ ] nenhuma rota fictícia foi criada a partir de exemplo documental;
- [ ] dependências entre features não criam ciclos;
- [ ] lint foi executado;
- [ ] verificação TypeScript foi executada;
- [ ] testes pertinentes foram executados;
- [ ] build foi executado;
- [ ] resultados e eventuais limitações de teste foram registrados.

---

# 43. Critério final de aprovação de uma implementação

Uma implementação está aderente quando um desenvolvedor consegue responder rapidamente:

1. Em qual rota essa funcionalidade aparece?
2. Qual feature é responsável por ela?
3. Qual componente representa a tela?
4. A operação executa no client ou no server?
5. Qual hook é utilizado, quando aplicável?
6. Qual função da API da feature é executada?
7. Existe fronteira Next?
8. Qual responsabilidade concreta justifica essa fronteira?
9. Qual verbo HTTP é usado?
10. Qual URL é chamada pelo browser?
11. Qual endpoint real da API .NET é chamado?
12. Qual contrato de request/response é utilizado?

Se essas respostas exigirem investigação por uma cadeia de objetos que apenas encaminham a chamada, a implementação deve ser simplificada.

---

# 44. Regra arquitetural definitiva

> O frontend do Yalla Finance é organizado por **feature** e otimizado para **rastreabilidade**.
>
> `app/` representa rotas, layouts, composição e fronteiras de execução.
>
> Componentes específicos permanecem próximos de suas features.
>
> TanStack Query gerencia estado remoto no cliente.
>
> React Hook Form + Zod gerenciam formulários de negócio.
>
> Toda chamada HTTP de negócio é declarada explicitamente na API da feature.
>
> `services/api` contém somente infraestrutura HTTP compartilhada.
>
> Chamadas autenticadas originadas do browser utilizam a fronteira Next enquanto for necessário manter o Bearer token fora do navegador.
>
> Server Components e Server Actions utilizam diretamente o módulo server da feature quando não houver razão concreta para atravessar um Route Handler.
>
> O frontend não conhece a implementação interna do ERP.
>
> Estado financeiro e estado de processamento de integração são conceitos distintos.
>
> Nenhuma camada deve ser criada sem responsabilidade concreta.
>
> A partir de uma tela, deve ser possível identificar o endpoint real do backend com o menor número possível de etapas.

---

# 45. Resultado esperado

Ao abrir o projeto, o desenvolvedor deve conseguir seguir naturalmente:

```text
"Estou na tela de débitos."
        ↓
"Ela pertence à feature debts."
        ↓
"DebtsPage usa useDebts."
        ↓
"useDebts chama a API da feature."
        ↓
"Se autenticada no browser, a chamada atravessa a fronteira Next."
        ↓
"O módulo server chama a API .NET."
        ↓
"O endpoint real é GET /api/debts."
```

Sem precisar descobrir uma cadeia de:

```text
Manager
Service
Repository
Gateway
Adapter
Provider
```

que apenas encaminha a mesma operação.

---

# 46. Status deste contrato

**APROVADO.**

Este arquivo é a referência arquitetural oficial para o frontend do Yalla Finance.

A partir desta versão:

- novas features devem seguir este padrão;
- refatorações devem convergir para este padrão de forma incremental;
- Pull Requests devem ser avaliados por este padrão;
- o Codex deve utilizar este documento como contrato de implementação;
- qualquer exceção deve possuir justificativa técnica explícita e responsabilidade concreta.
