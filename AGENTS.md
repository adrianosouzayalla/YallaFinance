# Regras core do Yalla Finance

Estas instruções se aplicam a todo o repositório: backend, frontend, scripts,
ferramentas, documentação e todas as features. Instruções locais complementam
estas regras e não podem criar exceções à política de testes.

## Alterações — comunicação e autorização prévias obrigatórias

- Antes de alterar qualquer arquivo ou comportamento, informar ao responsável
  o que será alterado, quais arquivos serão envolvidos e a finalidade da mudança.
- Aguardar autorização explícita do responsável antes de executar as alterações.
  A autorização cobre somente o escopo apresentado e aprovado.
- Nunca ampliar o escopo por iniciativa própria, mesmo para corrigir falhas,
  fazer melhorias ou deixar testes passando. Informar o problema e obter nova
  autorização antes de alterar qualquer item fora do escopo aprovado.
- Não alterar outras telas, fluxos ou regras de negócio como consequência de uma
  tarefa sem comunicar previamente e obter autorização específica.
- Inspeções e diagnósticos somente de leitura podem ser usados para apresentar
  uma proposta concreta; não autorizam alterações por si só.

## Git — criação de branches somente por ordem explícita

- **Nunca criar uma branch sem ordem explícita do responsável.** Trabalhar na
  branch atual, inclusive quando ela for `main` ou `master`.
- Autorizar uma feature, implementação, correção ou plano não autoriza criar
  branch. Não inferir essa autorização por conveniência, isolamento ou prática
  de desenvolvimento.
- Esta regra prevalece sobre skills e workflows que recomendem ou exijam criar
  branches. Não criar worktree que crie uma branch como forma de contorná-la.

## Nomenclatura e legibilidade do código

- Nomear métodos pela ação e responsabilidade que executam. O nome deve deixar
  claro o que o método faz e sobre qual conceito atua; evitar nomes genéricos
  como `Process`, `Handle` ou `Execute` quando o contexto não esclarecer a
  capacidade. Overrides e implementações de contratos mantêm os nomes exigidos
  pelo framework ou contrato.
- Nomear variáveis e parâmetros pelo conteúdo e papel que representam no fluxo.
  Evitar nomes ambíguos ou abreviações sem contexto, sem expandir
  mecanicamente nomes convencionais que já sejam claros no escopo imediato.
- Preservar nomes de membros públicos, parâmetros de APIs e construtores,
  propriedades ligadas a configuração, serialização ou persistência quando a
  renomeação puder alterar compatibilidade. Antes de renomear, localizar os
  consumidores e verificar esses vínculos; atualizar as referências somente
  quando a mudança estiver autorizada e segura.
- Melhorias de formatação devem ser localizadas, seguir `.editorconfig` e não
  reordenar instruções nem alterar expressões. Instruções independentes após
  `;` podem ocupar linhas separadas para facilitar a leitura.

## Services — contratos por responsabilidade de negócio

- Antes de criar um service ou uma interface de service, verificar se existe
  um contrato do mesmo domínio que possa receber a operação. Reutilizar e
  evoluir esse contrato quando a responsabilidade continuar coerente.
- Não criar um service por método, endpoint, consulta ou pequeno grupo de
  operações do mesmo domínio. Integrações com sistemas financeiros utilizam
  `IFinancialIntegrationService` como porta da Application; detalhes de cada
  fornecedor permanecem na Infrastructure.
- Expor métodos explícitos por operação, com entradas e resultados tipados.
  Não substituir esses métodos por dispatch genérico como `Execute(evento)`.
  Operações e eventos de domínio são conceitos distintos.
- Evitar proliferação de arquivos: contratos pequenos relacionados e auxiliares
  exclusivos de uma capacidade podem ficar agrupados em arquivos coesos.
  Não criar interfaces, wrappers, mappers ou arquivos por tipo mecanicamente.
- Separar services ou arquivos quando houver responsabilidade independente,
  reutilização concreta ou tamanho/complexidade que prejudique a leitura;
  justificar a separação no planejamento. Não concentrar domínios distintos
  em um service universal nem refatorar serviços existentes fora do escopo
  autorizado para aplicar esta regra retroativamente.

## Transações — atomicidade antes da localização do Unit of Work

- `IUnitOfWork` é um contrato válido para coordenar uma transação que precisa
  confirmar ou reverter em conjunto gravações relacionadas, inclusive entre
  repositórios ou múltiplas entidades.
- A presença de `IUnitOfWork` na Application não é, por si só, acoplamento
  indevido nem pendência de organização. Antes de propor removê-lo ou movê-lo,
  verificar qual atomicidade e quais garantias de concorrência/idempotência ele
  protege.
- Se a coordenação transacional for movida para um repositório ou serviço,
  preservar a mesma fronteira atômica e o comportamento de falha/replay. Não
  tratar seu uso como acesso direto ao banco.
- Em particular, `ReceiveInboundDebitHandler` mantém atômicos o registro da
  operação inbound e a criação do job; `ChargeCreationService` confirma a
  persistência da cobrança e seus itens (e, na cobrança avulsa, os débitos)
  antes de agendar o job. Não listar esses usos como ajustes necessários sem
  identificar uma falha concreta nessa responsabilidade.

## Testes — somente unitários

- É proibido criar, manter ou executar testes integrados e testes ponta a ponta
  que utilizem aplicações, bancos, serviços ou infraestrutura reais.
- Usar somente testes unitários isolados, sem acesso a banco real, rede externa,
  APIs/providers reais, credenciais reais ou dados operacionais.
- Não usar PostgreSQL, SQL Server, SQLite real, Testcontainers, Docker ou bases
  temporárias como infraestrutura de testes.
- Não criar projetos, fixtures, gates, smoke tests ou scripts de validação real
  como alternativa à proibição, inclusive em `.tmp`, `Temp`, `FEATURES` ou tools.
- Mocks, doubles, transportes simulados, armazenamento em memória e
  interceptadores que suprimem conexões/comandos são permitidos nos unitários.
- Inspeção do modelo EF e geração de SQL com `ToQueryString()` são permitidas,
  desde que nenhum comando seja executado em banco real.
- A suíte deve funcionar sem `.env`, banco, containers ou credenciais. Falta de
  infraestrutura não deve provocar skips nem exigir instalação de serviços.
- Evidências históricas ou planos de homologação não autorizam recriar testes
  integrados. Aceite manual pelo responsável não pertence à suíte automatizada.

Consultar também [tests/AGENTS.md](tests/AGENTS.md) e
[tests/README.md](tests/README.md). Esta é uma regra core, independente da fase
ou feature em desenvolvimento.

## Providers de pagamento — recursos remotos ficam no provider

- Nunca criar objetos de persistência no Yalla para atender rotinas ou recursos internos específicos de um provider, incluindo tabelas, entidades, colunas, índices, enums ou registros genéricos que representem cobranças remotas do fornecedor.
- Criação, consulta, atualização, cancelamento, referência externa e recuperação de recursos remotos são responsabilidade da implementação específica do provider na Infrastructure.
- Persistir somente dados próprios do fluxo e do domínio Yalla em estruturas já existentes, como tentativa e transação, quando necessários às regras financeiras, idempotência, auditoria ou conciliação.
- Reutilizar contratos de capacidade existentes sem vazar modelos, estados ou identificadores específicos do provider para Application ou domínio.

## Jobs — cada tentativa executa o evento desde o início

- Esta regra vale para todos os Jobs, atuais e novos. O motor executa o evento
  e registra o resultado final da tentativa como sucesso ou erro, com diagnóstico.
- Reprocessar um Job com erro cria uma nova tentativa e executa novamente o
  evento desde o início. Não retomar etapas nem herdar bloqueios de tentativas
  anteriores, sejam eles mantidos em memória ou no histórico de execuções.
- O evento é responsável por consultar o estado atual, validar as regras de
  negócio e garantir idempotência: se a operação já foi concluída, confirmar o
  resultado existente; se não foi, executar somente o que ainda é necessário.
- Recarregar os dados e configurações vigentes em cada tentativa, inclusive
  conexões e mapeamentos. Nunca usar resultados em cache ou snapshots de outra
  execução como fonte oficial desses dados.
- Histórico, resultados parciais e checkpoints de execução servem para consulta
  e diagnóstico. Não podem autorizar, bloquear ou decidir ações de uma tentativa
  posterior. Detalhes técnicos de uma falha não criam um terceiro resultado do Job.
- Consultas ao estado atual e locks de concorrência continuam obrigatórios quando
  necessários para impedir duplicidade. Se a consulta falhar ou indicar conflito,
  o evento retorna erro nesta tentativa; uma nova tentativa refaz as verificações.
- Preservar as transações de negócio, inclusive as garantias de atomicidade do
  inbound e da criação/baixa de cobranças. Não confundir idempotência de domínio
  com controle de retomada do Job.

## Configurações gerais por tenant

- Parâmetros gerais da empresa usam `core.tenant_setting`, com chave única
  `(tenant_id, group, key)` e auditoria de criação/alteração.
- Reutilizar `Application/Repositories/Settings/ITenantSettingRepository`; o módulo proprietário
  define chaves canônicas, tipos, limites e autorização. Não oferecer gravação
  arbitrária de chaves em endpoints públicos.
- Tenant sempre vem do contexto autenticado. Todas as leituras e escritas incluem
  seu identificador e usam SQL parametrizado.
- Cada tenant possui seus próprios parâmetros. Payments usa exclusivamente
  core.tenant_setting; não criar tabela de sistema nem herança/liberação global
  para MaximumLinkDays ou PublicOrigin. Valor invalido ou
  configuração obrigatória ausente não deve ser mascarado por fallback silencioso.
- Configurações específicas de providers continuam nas tabelas das conexões externas.
- Cadastro de solicitação/token independe de URL, checkout e provider. A habilitação
  do checkout é da empresa selecionada e não depende de uma liberação global.

## Configurações — banco de dados como fonte oficial

- Todas as configurações persistentes do sistema devem ficar no banco de dados,
  incluindo parâmetros gerais, URLs, habilitações, permissões de configuração,
  credenciais e chaves de proteção dos tokens, com suas versões históricas.
- Esta é a regra padrão para todas as features e tenants. Não criar soluções
  baseadas em User Secrets do .NET, appsettings, variáveis de ambiente ou arquivos
  locais como fonte oficial desses parâmetros, nem como fallback que mascare
  configuração ausente no banco.
- Configurações por empresa devem respeitar tenant, grupo e chave. Não converter
  parâmetros da empresa em configuração global ou criar tabela de sistema sem
  solicitação explícita.
- Chaves e credenciais exigem acesso restrito e recuperação autorizada; nunca
  expor valores em logs, respostas de listagem, telas públicas ou arquivos versionados.
- A chave de proteção dos tokens deve ser lida da configuração persistida no banco.
  Não inserir uma chave fictícia ou temporária para contornar ausência de configuração.
- Ao migrar uma configuração anterior externa ao banco, preservar a chave original
  e as versões históricas antes de remover a fonte antiga. Não trocar silenciosamente
  a chave e tornar tokens existentes irrecuperáveis.
- Esta decisão substitui orientações históricas que exigiam manter chaves de
  pagamentos fora do banco. Novas solicitações usam GUID público no banco, sem
  chave de proteção. As chaves citadas acima aplicam-se aos tokens protegidos
  legados e a outros usos criptográficos. A carga base é somente database/insert_base.sql,
  com inserts básicos, sem PowerShell, importação ou criação de chaves.

## Dados mestres — somente banco de dados

- Dados mestres de operação, como empresas, clientes, contratos, categorias,
  formas de pagamento, acessos e cadastros de frota, devem ter o banco como fonte oficial.
- Não carregar JSONs, mocks, listas fixas ou arquivos locais como cadastro de operação,
  nem mesclar registros dessas fontes com consultas reais do banco.
- Seeds e scripts de carga servem para inserir registros no banco; não devem atuar
  como repositórios consultados durante a execução da aplicação.
- Dados simulados e arquivos de fixture podem existir exclusivamente em testes
  unitários isolados, sem participação no runtime da aplicação.
- Parametrizações operacionais também devem ser persistidas no banco; não criar
  novas dependências de arquivos/ambiente ou exceções implícitas à regra.
- A revisão estática dos usos atuais está registrada em
  REVISAO_CONFIGURACOES_DADOS_MESTRES_2026-10-09.md. Pendências listadas não são
  autorização para manter a arquitetura anterior.

## Criação de tabelas novas — aprovação prévia obrigatória

- Nunca criar uma tabela nova em migration, script, modelo EF ou código sem antes
  apresentar a proposta ao responsável e obter aprovação explícita.
- Antes de pedir aprovação, informar a finalidade, os campos, chaves, relacionamentos,
  constraints e índices previstos, além de avaliar se uma estrutura existente atende.
- Uma solicitação genérica para implementar uma fase ou funcionalidade não autoriza,
  por si só, a criação de tabelas. A aprovação deve cobrir a tabela proposta.
- Não preparar nem executar o SQL de criação antes dessa aprovação.

## Auditoria — decisão obrigatória para toda tabela nova

- Toda proposta de tabela nova deve responder explicitamente ao responsável:
  **“Esta tabela precisa de auditoria de criação e alteração?”** Apresentar a
  recomendação e sua justificativa, e obter a decisão junto com a aprovação da
  tabela, antes de preparar o SQL. Nunca presumir inclusão ou dispensa silenciosa.
- Registrar a decisão nos requisitos e no plano da feature. Se a tabela ficar
  fora da auditoria, registrar a justificativa e a aprovação explícita da exceção.
  A dispensa de uma tabela, como `core.database_version`, não se estende a outras.
- Quando aprovada para auditoria, usar obrigatoriamente estes quatro campos,
  sem nomes alternativos ou campos duplicados com a mesma finalidade:

  | Campo | Tipo PostgreSQL | Regra |
  |---|---|---|
  | `created_at` | `timestamptz` | UTC, `NOT NULL`, imutável após criação |
  | `created_by` | `uuid` | `NOT NULL`, FK para `core.user(id)`, imutável após criação |
  | `updated_at` | `timestamptz` | UTC, nullable até a primeira alteração material |
  | `updated_by` | `uuid` | Nullable até a primeira alteração material, FK para `core.user(id)` |

- As duas FKs usam `ON DELETE RESTRICT` e `ON UPDATE RESTRICT`. Incluir CHECK
  para que `updated_at` e `updated_by` sejam ambos nulos ou ambos preenchidos.
- INSERT preenche somente `created_*`; `updated_*` começam nulos. UPDATE material
  preserva `created_*` e preenche o par `updated_*`. Consulta, replay e gravação
  sem mudança material não avançam a auditoria.
- Reutilizar o mecanismo central da Feature 22 na Infrastructure e o contexto
  confiável de ator. Autoria usa UUID do usuário validado da sessão ou do ator
  operacional autorizado; nunca e-mail, texto livre ou metadados enviados pelo
  cliente. Não usar SYSTEM como fallback para ausência de ator autenticado.
- A entrega da tabela auditada inclui a adequação de todos os seus escritores:
  EF rastreado, SQL direto, UPSERT, operações em lote e `ExecuteUpdate`, conforme
  aplicável. Campos e carimbos pertencem à mesma gravação/transação de negócio.
  Não considerar a auditoria concluída apenas por criar as colunas.
- Seguir o padrão e a implementação da
  [Feature 22](FEATURES/22%20-%20Auditoria%20por%20tabela/YALLA_FINANCE_PADRAO_AUDITORIA_METADADOS.md).
  DDL permanece em scripts versionados; atualizações administrativas de dados
  existentes seguem o procedimento manual aprovado pelo responsável.

## Padrão geral de telas do frontend

- Loading de tela é centralizado em `frontend/src/components/ui/LoadingState.tsx`.
  Toda carga inicial de tela/dados, consulta solicitada pelo usuário (filtros,
  ordenação, paginação, atualização, detalhes e retry) e gravação iniciada pelo
  usuário deve bloquear a viewport com `mode="blocking"` e mensagem curta em
  português.
- O overlay impede interação por ponteiro e teclado, mantém a tela/dados montados
  por trás e é único por tela, agregando operações simultâneas. Não criar spinners,
  barras ou modais de loading paralelos para a mesma operação.
- Sucesso ou falha encerra o bloqueio. Falhas mostram erro compreensível e retry
  quando aplicável; nunca deixar a interface bloqueada após a resposta.
- Polling, atualização automática de cache e refetch em segundo plano não bloqueiam.
  Preservar os dados atuais; indicador discreto é opcional somente quando ajudar a
  explicar uma atualização automática.
- Sugestões de autocomplete usam spinner inline no campo. Progresso de tarefa de
  negócio (por exemplo, Job em execução) continua contextual, não é loading da tela.
- Fallbacks de rota Next usam `mode="page"` do mesmo componente, responsivo e com
  a mesma identidade visual.
- Tabelas de listas operacionais usam
  `frontend/src/components/ui/SelectablePaginatedTable.tsx`. Features podem
  fornecer colunas, seleção, elegibilidade e ações por wrappers finos; não criar
  outra implementação de tabela, ordenação, paginação ou seleção por tela.
- Buscas/atualizações manuais não desenham indicadores locais dentro ou acima da
  tabela; usam o `LoadingState` compartilhado. Indicadores discretos só podem ser
  usados para atualizações automáticas sem bloquear a interação.
- O contrato detalhado está em [frontend/AGENTS.md](frontend/AGENTS.md) e
  [frontend/docs/ARQUITETURA_PADRAO_FRONTEND_YALLA_FINANCE_FINAL.md](frontend/docs/ARQUITETURA_PADRAO_FRONTEND_YALLA_FINANCE_FINAL.md).
