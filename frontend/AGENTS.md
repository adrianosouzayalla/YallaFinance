<!-- BEGIN:nextjs-agent-rules -->

# This is NOT the Next.js you know

This version has breaking changes — APIs, conventions, and file structure may all differ from your training data. Read the relevant guide in `node_modules/next/dist/docs/` (resolved from this file's directory; in monorepos the `next` package may not be visible from the repo root) before writing any code. Heed deprecation notices.

This block is written and re-added by `next dev` — verify at `node_modules/next/dist/server/lib/generate-agent-files.js`. Removing it from a diff only re-creates the uncommitted change; committing it with your work keeps the tree clean.

<!-- END:nextjs-agent-rules -->

## Contrato do projeto

A tela de débitos é a referência aprovada de layout para as demais telas
operacionais, incluindo componentes, tamanhos e espaçamentos dos filtros.
Adaptar as novas telas a esse modelo; não alterar o layout de débitos para
atender necessidades de outras features sem solicitação explícita do responsável.
O botão de aplicação dos filtros deve se chamar **Filtrar**, nunca **Buscar**.
Reutilizar a tabela compartilhada não dispensa seguir o layout completo da referência:
cabeçalho com título e ações, tokens de cor, tipografia, filtros compactos, alinhamentos,
espaçamentos, botões Filtrar/Limpar e apresentação da tabela. Não adicionar painéis de
filtros ou estilos próprios por feature. Novas telas e diálogos devem preservar essa
identidade; ícones de navegação e títulos devem representar a funcionalidade correspondente.
É obrigatório reutilizar os modelos de tela já existentes; não inventar layouts,
painéis, espaçamentos ou tamanhos de campo próprios por feature. Para formulários
de cobrança, usar `/charges/new` como referência de disposição, proporções dos
painéis, campos compactos e ações. Preservar o padrão de débitos nas tabelas.
Campos de valor e data usam `CurrencyField` e `DateField`, respectivamente.
Respeitar também os espaçamentos superiores e inferiores entre campos, painéis,
tabelas e ações, usando os gaps/paddings do modelo; nunca deixar componentes colados.
Não incluir avisos de protótipo, mock, simulação ou detalhes de implementação na
interface sem solicitação explícita. Os textos da tela devem ajudar o usuário
na operação, seguindo os modelos existentes.
No cadastro de usuários, a listagem oferece filtros, Novo usuário e a entrada Editar
na tabela. Não incluir Senha inicial da empresa nem Alterar minha senha na listagem.
Cadastro e edição usam a mesma tela/formulário em página própria, nunca modal.
Ativação/desativação global e redefinição de senha ficam na página de edição,
mantendo confirmações. Não oferecer inclusão de vínculo com outra empresa nem
ativação/desativação do vínculo nessa tela; a gestão de vínculos adicionais ocorre
somente via banco até a criação de uma tela específica. Preservar este padrão
até decisão posterior.

As ações de ativar e desativar cadastros seguem o padrão visual da tela de usuários:
usar `BlockOutlinedIcon` com `color="error"` para desativar um registro ativo e
`PersonAddAltOutlinedIcon` com `color="success"` para ativar um registro inativo.
Manter essa associação entre ação, ícone e cor nas telas de cadastro e edição.

Aplicar [ARQUITETURA_PADRAO_FRONTEND_YALLA_FINANCE_FINAL.md](docs/ARQUITETURA_PADRAO_FRONTEND_YALLA_FINANCE_FINAL.md).
Documentos V1–V4 são históricos; o contrato final é a referência oficial.
Todas as telas operacionais que apresentam dados em tabela devem usar
`src/components/ui/SelectablePaginatedTable.tsx` como componente padrão.
Para seleção entre páginas, usar `src/hooks/usePaginatedSelection.ts`.
As features fornecem colunas, elegibilidade, consultas e ações de negócio;
o componente compartilhado permanece genérico. Evoluir esse componente
para necessidades adicionais, evitando tabelas paralelas e duplicação da lógica.
Cabeçalhos de dados devem ordenar por padrão, alternando crescente/decrescente;
colunas de ações usam `sortable: false`. Em listas paginadas no servidor,
ordenar o conjunto filtrado antes da paginação. Para dados completos no cliente,
usar `paginationMode="client"` e `getSortValue` com valores brutos quando necessário.
Ordenar preserva a seleção e retorna à primeira página.
Consultar [TABELA_SELECAO_PAGINADA.md](docs/TABELA_SELECAO_PAGINADA.md).
Reutilizar `src/components/forms/CurrencyField.tsx` e `DateField.tsx` nos campos de
moeda/data; utilizar os utilitários de `src/utils/currency.ts` e `date.ts` para
formatação e conversão. Datas nativas são o padrão dos fluxos existentes;
máscara `dd/mm/aaaa` é uma opção explícita do consumidor. Regras de negócio,
schemas e contratos permanecem nas features. Consultar
[CAMPOS_MOEDA_DATA.md](docs/CAMPOS_MOEDA_DATA.md).

## Notificações operacionais

- Usar `src/providers/ToastProvider.tsx` para sucesso, falha, validação de uma ação e ausência de resultado em consultas, incluindo cliente não encontrado. Não criar alertas fixos na tela para essas notificações.
- Preservar os detalhes compreensíveis do erro, sem expor credenciais ou respostas sensíveis. Em falhas recuperáveis, manter os dados e oferecer nova tentativa pelos controles da tela.
- Mensagens persistentes de situação do negócio, como resultado da cobrança e andamento da integração, permanecem nos componentes contextuais existentes; não são notificações de uma ação.
- Consultas automáticas ao sair de um campo reutilizam o loading compartilhado, respeitam a entrada completa e evitam requisições simultâneas duplicadas. A regra de consulta fica na feature; o campo compartilhado permanece genérico.

## Padrão obrigatório de carregamento

- Usar `src/components/ui/LoadingState.tsx` como indicador compartilhado em toda a aplicação.
- Carga inicial de tela/dados, consultas iniciadas por ação explícita (Filtrar, ordenar, paginar,
  atualizar, abrir detalhes ou tentar novamente) e gravações iniciadas pelo usuário bloqueiam a
  interface com `mode="blocking"` e uma mensagem curta em português.
- O bloqueio cobre a viewport, impede clique e navegação por teclado enquanto ativo e mantém o
  conteúdo/dados já apresentados por trás do overlay. Agrupar operações simultâneas em um único
  overlay por tela; não empilhar modais nem manter outro spinner para a mesma operação.
- Quando uma atualização falhar, liberar o bloqueio e apresentar erro recuperável com opção de
  nova tentativa. Nunca manter a interface bloqueada após sucesso ou erro.
- Polling, atualização automática de cache e refetch em segundo plano não bloqueiam a tela; dados
  existentes permanecem utilizáveis e podem ser atualizados silenciosamente. Indicador local leve
  é permitido quando ajudar a explicar a atualização automática.
- Busca de sugestões de autocomplete mantém spinner inline no próprio campo. Progresso de uma tarefa
  de negócio em execução não é loading de tela e continua no componente contextual correspondente.
- `mode="page"` é reservado ao fallback da rota/segmento Next antes de montar a tela. Deve ser
  responsivo e compartilhar o spinner/mensagem visual do padrão.
Consultar [README.md](README.md) para organização e ambiente e
[VALIDACAO_MIGRACAO_FRONTEND.md](docs/VALIDACAO_MIGRACAO_FRONTEND.md) para evidências e exceções temporárias.
Preservar regras financeiras, payloads, status técnicos e algoritmo de idempotência.
APIs client autenticadas passam pela fronteira Next; Bearer e `API_URL` são server-only.
Não executar POST financeiro real como smoke da refatoração.
A consulta de cobrança usa o GET autorizado da API .NET. Conforme a regra core atual, dados mestres e parâmetros de operação devem vir do banco; não carregar mocks, JSONs ou listas fixas como cadastros de operação, nem fabricar resultados financeiros no frontend. Fixtures ficam restritas a testes unitários isolados.
