# Worker e criação de novos Jobs

Este guia descreve a engine implementada na Feature 07, Fase 1, e serve como base para conectar novas funcionalidades. Os exemplos abaixo são ilustrativos: não estão registrados nem executando em produção.

## Situação atual

O Worker inicia junto com API e frontend pelos scripts de restart. Ele funciona sem login de usuário, usa uma única conexão configurada e consulta Jobs `Pending`. A Fase 2 registra `FINANCIAL_CHARGE_CREATE_V1`: a criação local da cobrança agenda um Job e o Worker integra seus itens com o provider financeiro configurado.

## Acompanhamento pela tela — Fase 3

O menu **Jobs** abre `/jobs`, com listagem por tenant autenticado, busca parcial literal pela Key, tipo exato, status e paginação. O detalhe `/jobs/{id}` apresenta Key, tipo, status, datas e Payload formatado. Uma tabela mostra execuções com status derivado, início, término, duração e erro seguro; Ver detalhes abre Result e Details como JSON genérico, sem links ou interpretação de cobrança.

Payload, Result e Details passam por proteção recursiva no servidor para ocultar campos conhecidos de dados pessoais, credenciais e diagnóstico sensível. Texto JSON inválido não é exposto; essa proteção não é anonimização universal e não substitui o cuidado do handler com o conteúdo persistido.

A tela consulta os GETs `/api/jobs`, `/api/jobs/{id}` e `/api/jobs/{id}/executions` através da fronteira Next autenticada. Essa autenticação pertence à consulta do usuário e não muda a autonomia do Worker.

A atualização automática ocorre a cada 10 segundos com a página visível; o detalhe para após finalização. Há atualização manual. A consulta da cobrança mantém a busca exata pela Key; a consulta de acompanhamento é separada.

A Fase 3 entregou acompanhamento somente leitura. A Fase 4 adiciona o reprocessamento manual descrito abaixo. Um Job Processing com tempo elevado continua mostrando seu estado persistido, sem diagnóstico automático de interrupção. Consulte [entrega e validação da Fase 3](../../FEATURES/07%20-%20Uso%20da%20Integracao%20Financeira%20e%20Persistencia/FASE%203%20-%20VALIDACAO.md).

## Reprocessamento manual — Fase 4

Nos detalhes de um Job `Failed`, **Reprocessar** abre uma confirmação. O POST autenticado `/api/jobs/{id}/reprocess` usa o tenant da sessão e muda o mesmo Job de `Failed` para `Pending`, preservando tipo, Key, payload, criação e histórico. Atualiza somente status e UpdatedAt; a gravação compara também a versão de UpdatedAt lida para não aceitar uma transição baseada em dados antigos.

A API não chama o handler nem cria execução. O Worker encontra o Pending pelo claim existente e cria uma nova tentativa. A interface retoma o acompanhamento e permite consultar cada execução pelo histórico paginado; o recibo “Job enviado para reprocessamento.” confirma somente o agendamento.

Para `FINANCIAL_CHARGE_CREATE_V1`, a última execução precisa conter resumo consistente de rejeição ou resultado parcial sem itens incertos. Resultados incertos, ausentes, inválidos ou contraditórios bloqueiam a ação com orientação de conferência operacional. Uma falha após chamada externa, antes de salvar seu ID, não é tratada como rejeição conhecida. A regra não consulta o provider nem fornece conciliação ou execução exatamente uma vez.

Corrigir a causa antes de reenfileirar. No caso de token atualizado no arquivo financeiro, reiniciar o Worker pelo procedimento habitual para carregar a configuração, conforme orientação da Fase 2.

Outros tipos Failed seguem a elegibilidade genérica. Ao adicionar um handler, definir como uma segunda tentativa respeita os efeitos já confirmados: a engine não oferece idempotência externa universal. Não há reprocessamento de Success ou Processing, recuperação automática, edição de payload, cancelamento ou ação em lote. A criação original permanece como referência da ordem da fila. O histórico não registra a identidade do usuário que clicou.

Consulte os [requisitos](../../FEATURES/07%20-%20Uso%20da%20Integracao%20Financeira%20e%20Persistencia/FASE%204%20-%20REQUISITOS.md), [plano](../../FEATURES/07%20-%20Uso%20da%20Integracao%20Financeira%20e%20Persistencia/FASE%204%20-%20PLANO_EXECUCAO.md) e [validação da Fase 4](../../FEATURES/07%20-%20Uso%20da%20Integracao%20Financeira%20e%20Persistencia/FASE%204%20-%20VALIDACAO.md).

## Funcionalidade implementada — integração de cobrança

- Definição/payload/handler: [IntegrateCharge](../YallaFinance.Application/Features/Charges/IntegrateCharge).
- Um Job por cobrança, Key `charge:{ChargeId}`, payload somente com ChargeId.
- `ChargeCreatedNotificationHandler` agenda após o commit; não chama diretamente o provider financeiro.
- O handler do Job lê dados pelo tenant, processa pendentes na ordem dos itens e preserva cada vínculo externo confirmado.
- Success significa todos confirmados; Failed pode representar rejeição, conclusão parcial ou incerteza. Result guarda resumo e detalhe seguro por item.
- A consulta da cobrança retorna `processing` com JobId, Status, Result e código público. A tela oferece atualização manual; vínculos dos itens continuam sendo a fonte de confirmação externa.
- Credenciais são resolvidas no Worker. Em Development, ele aponta para os mesmos arquivos financeiros da WebApi, normalizando caminhos a partir de seu ContentRoot. Em outros ambientes, configurar FinancialIntegrations no host do Worker.
- Token atualizado no arquivo requer restart do Worker. Jobs Failed não voltam automaticamente para Pending.
- A cobrança e o Job não compartilham transação: falha ao agendar devolve a cobrança já criada com `NotScheduled`. Não existe outbox ou recovery nesta entrega.

O core está implementado e foi verificado com testes sem banco. A validação real de persistência, concorrência, atomicidade e restart com PostgreSQL permanece adiada. Os scripts V012/V013 e o scaffold foram aplicados/gerados manualmente pelo responsável.

## Responsabilidades e localização

| Camada | Responsabilidade | Código |
|---|---|---|
| Domain | Job, execução e estados | [Domain/Jobs](../YallaFinance.Domain/Jobs) |
| Application | Contratos, criação, registry, processamento e consultas | [Application/Jobs](../YallaFinance.Application/Jobs) |
| Infrastructure | SQL parametrizado, claim e finalização atômicos | [Infrastructure/Jobs](../YallaFinance.Infrastructure/Jobs) |
| Jobs | Consumo de Jobs, escopos DI, espera e cancelamento | [JobWorker.cs](./Processing/Jobs/JobWorker.cs) |
| Processamento contínuo | Sincronização periódica de recebimentos por provider | [PaymentReceiptWorker.cs](./Processing/Continuous/PaymentReceiptWorker.cs) |
| Composição do Worker | Serviços e conexão exigidos pelo host | [WorkerComposition.cs](./Composition/WorkerComposition.cs) |

A definição monta Type, Key e Payload. O handler executa a regra de negócio. O Processor coordena a execução. Os repositórios gravam os estados; o handler não altera Job ou JobExecution diretamente.

## Fluxo de uma execução

1. A funcionalidade chama `JobService.CreateAsync` com tenant e definição tipada.
2. O serviço valida tenant, Type registrado com contrato compatível e Key; serializa o payload e grava `core.job` como `Pending`. Não executa o handler.
3. O Worker abre um novo escopo DI e chama `JobProcessor.ProcessNextAsync`.
4. O claim não filtra por `Type`: escolhe o próximo job `Pending`, ordenado por CreatedAt e Id, com `FOR UPDATE SKIP LOCKED`. Uma transação curta muda o Job para `Processing` e cria uma JobExecution aberta.
5. O Processor inicializa o contexto com `Job.TenantId`. O registry usa o `Type` para selecionar o registro, que desserializa o `Payload` e resolve o handler scoped por DI.
6. O registry chama `HandleAsync` diretamente; essa execução não passa pelo MediatR. O handler executa a regra fora da transação da engine e retorna `JobHandlerResult`.
7. O Processor valida o retorno e os JSONs. Uma segunda transação curta grava o resultado, encerra a execução e muda o Job para `Success` ou `Failed`.
8. O escopo é descartado e o Worker busca o próximo Job.

Uma tentativa processa um Job; não há execução paralela dentro de uma instância do Worker. O claim foi implementado para concorrência entre instâncias, mas essa garantia ainda precisa da comprovação PostgreSQL pendente.

## Como adicionar uma funcionalidade

### 1. Definir o contrato persistido

Coloque payload, definição e handler na área da funcionalidade em Application, seguindo a organização já usada pelo projeto. Não coloque a regra de negócio em `JobWorker` ou `JobProcessor`.

Escolha um Type estável e explícito, com até 100 caracteres. Não use automaticamente o nome da classe: renomear uma classe não deve tornar Jobs persistidos ilegíveis. O exemplo usa uma operação fictícia de processamento de documento.

```csharp
using YallaFinance.Application.Jobs;

namespace YallaFinance.Application.Features.Documents.Process;

public sealed record ProcessDocumentPayload(Guid DocumentId);

public sealed record ProcessDocumentJob(Guid DocumentId)
    : IJobDefinition<ProcessDocumentPayload>
{
    public const string JobType = "DOCUMENT_PROCESS_V1";
    public const string JobTitle = "Processar documento";

    public string Type => JobType;
    public string BuildKey() => $"document:{DocumentId:D}";
    public ProcessDocumentPayload BuildPayload() => new(DocumentId);
}
```

A Key tem até 250 caracteres e serve para correlação e consulta. Ela não é única: criar duas vezes a mesma definição cria dois Jobs. A engine não oferece deduplicação ou idempotência por Key.

O payload deve ser serializável e conter somente os dados necessários para reconstruir a operação posteriormente. A engine usa `JsonSerializerOptions.Web`. Identificadores permitem ao handler buscar o estado atual; se a operação precisar de um retrato do momento da criação, isso deve ser definido no contrato da funcionalidade.

Não persista senhas, tokens ou credenciais no payload. Mudanças de contrato precisam considerar os Jobs já gravados: preserve compatibilidade ou mantenha o tipo/handler antigo enquanto houver Jobs que dependam dele. O sufixo V1 do exemplo é uma convenção ilustrativa, não um mecanismo automático de versionamento.

### 2. Implementar o handler

```csharp
using System.Text.Json;
using YallaFinance.Application.Jobs;

namespace YallaFinance.Application.Features.Documents.Process;

public sealed class ProcessDocumentHandler : IJobHandler<ProcessDocumentPayload>
{
    public Task<JobHandlerResult> HandleAsync(
        ProcessDocumentPayload payload,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (payload.DocumentId == Guid.Empty)
            return Task.FromResult(new JobHandlerResult(
                false,
                Error: new JobError("InvalidDocumentId", "Identificador obrigatório.")));

        // Implementar aqui a operação real por serviços/repositórios injetados.
        // Este exemplo demonstra somente o contrato de retorno.
        return Task.FromResult(new JobHandlerResult(
            true,
            Result: JsonSerializer.SerializeToElement(new
            {
                documentId = payload.DocumentId
            })));
    }
}
```

O handler deve validar os dados de negócio, acessar apenas dados do tenant atual e propagar o CancellationToken a operações assíncronas. Uma desserialização bem-sucedida não garante valores de negócio válidos; validações como identificador obrigatório pertencem ao handler.

`JobHandlerResult` recebe `Success`, `Result`, `Error` e `Details`. Result e Details são `JsonElement?`; Error é `JobError(Code, Message)`.

- Sucesso: `Success = true`; Error é descartado pelo Processor.
- Falha de negócio: `Success = false` e Error com código não vazio e mensagem pública segura.
- Result e Details são opcionais. Use `JsonSerializer.SerializeToElement` ou um elemento clonado; não retorne elementos de um JsonDocument já descartado.
- Não inclua conteúdo sensível ou exceções brutas em Error, Result ou Details. A engine não faz sanitização geral desses JSONs; o conteúdo é responsabilidade da funcionalidade.

### 3. Registrar o tipo nos hosts envolvidos

Use o mesmo registro no host que cria o Job e no Worker que o executa:

```csharp
services.AddJob<ProcessDocumentPayload, ProcessDocumentHandler>(
    ProcessDocumentJob.JobType, ProcessDocumentJob.JobTitle);
```

`AddJob` registra o contrato e o handler scoped. Type duplicado na mesma coleção de serviços é rejeitado. Na criação, o registry também verifica se o Type corresponde ao tipo de payload informado.

`JobTitle` é o nome amigável apresentado ao usuário. Defina-o junto de `JobType` na classe do Job e passe ambos para `AddJob`. O título não é persistido: pode ser ajustado sem mudar Jobs já gravados. Listagem, detalhe e seletor de tipos recebem `jobTitle` da API; o valor do filtro, consultas, logs e execução continuam usando o código técnico `type`.

O registro aceita apenas Type para compatibilidade com definições anteriores; nesse caso, o título é o próprio código. Tipos desconhecidos também usam esse fallback. O catálogo do filtro continua restrito aos tipos presentes nas tabelas do tenant, enriquecidos pelo registro de metadados, sem adicionar tipos que ainda não possuem Jobs.

Exemplo atual: `IntegrateChargeJob.JobType` é `FINANCIAL_CHARGE_CREATE_V1`; `IntegrateChargeJob.JobTitle` é “Criar cobrança no sistema financeiro externo”. A API retorna opções de tipo como `{ type, jobTitle }`, preservando a identidade técnica em todos os contratos.

Para uma funcionalidade criada pela API, adicione esse registro à composição Application usada pela API e à composição do Worker. Uma extensão compartilhada da funcionalidade pode concentrar o registro, sendo chamada pelos dois hosts. Registre cada tipo uma única vez por host.

O Worker chama `AddJobEngine`, `AddInfrastructure` e `AddJobWorker`; ele não chama `AddApplication`. Portanto, não presuma que dependências registradas apenas na composição da API estejam disponíveis no Worker. Registre explicitamente os serviços exigidos pelo novo handler. Se precisar de MediatR, validators ou outros serviços, revise sua composição para o host sem HTTP.

O host criador precisa do contrato registrado, mas não executa o handler durante CreateAsync. O handler e suas dependências de execução devem ser resolvidos corretamente no Worker; também considere validações de DI dos hosts envolvidos.

### 4. Criar o Job na funcionalidade de origem

Injete `JobService` no serviço ou command handler de origem:

```csharp
Guid jobId = await jobService.CreateAsync(
    tenantId,
    new ProcessDocumentJob(documentId),
    cancellationToken);
```

`tenantId` deve vir do contexto autorizado da funcionalidade. A validação da engine rejeita Guid.Empty, mas não substitui autorização de acesso ao tenant. CreateAsync retorna o Id após persistir o Job, não após concluir o processamento.

A gravação do Job usa uma conexão própria e não participa automaticamente da transação de negócio do chamador. Se a funcionalidade precisar salvar uma entidade e criar um Job como uma operação indivisível, essa garantia precisa de uma decisão específica antes de implementar o fluxo; a Fase 1 não oferece outbox ou transação conjunta.

### 5. Consultar o andamento

Injete `JobQueries`. Todas as consultas exigem tenant:

```csharp
var job = await jobQueries.GetByIdAsync(tenantId, jobId, cancellationToken);

var jobs = await jobQueries.ListAsync(
    tenantId,
    new JobListFilter(Key: $"document:{documentId:D}",
        Type: ProcessDocumentJob.JobType),
    page: 1,
    pageSize: 20,
    cancellationToken: cancellationToken);

var executions = await jobQueries.ListExecutionsAsync(
    tenantId, jobId,
    cancellationToken: cancellationToken);
```

Page começa em 1; PageSize padrão é 20 e o máximo é 100. A resposta `JobPage<T>` contém Items, Page e PageSize, sem contagem total. A engine não expõe endpoints administrativos; eventual API de consulta pertence à funcionalidade que a utilizar.

## Tenant e dependências

No Worker, `ICurrentTenantContext` e `IJobTenantContext` apontam para a mesma instância scoped. O Processor inicializa esse contexto antes de resolver o handler; serviços de negócio podem receber `ICurrentTenantContext` por DI. TenantId nesse contrato é uma string que representa o Guid do Job.

O Worker não possui HttpContext, login, JWT de usuário ou contexto de usuário autenticado. Dependências que exigem esses elementos precisam de adaptação explícita antes de serem utilizadas em um handler.

O contexto de tenant não filtra automaticamente todo SQL. A funcionalidade e seus repositórios precisam aplicar o tenant nas operações de negócio. A persistência da engine mantém tenant em Job e JobExecution e exige tenant nas consultas e na finalização.

Não há seleção de bancos por tenant nesta fase. Todos os Jobs usam `ConnectionStrings:YallaFinance`.

## Estados, falhas e interrupções

| Situação | Comportamento |
|---|---|
| Criado | Pending, aguardando claim |
| Claim confirmado | Processing e uma execução aberta |
| Handler retorna sucesso | Success e execução finalizada |
| Handler retorna falha de negócio | Failed e execução finalizada |
| Type desconhecido | Failed com UnknownJobType |
| Payload não reconstruível | Failed com InvalidJobPayload |
| Exceção comum na execução | Failed com JobHandlerError e mensagem fixa |
| Retorno inválido/JSON inválido do handler | Failed com InvalidHandlerResult |
| Cancelamento do host ou falha de infraestrutura após o claim | Pode permanecer Processing com execução aberta |

Success encerra o processamento. Failed não volta automaticamente para Pending; a Fase 4 oferece somente a ação manual elegível. Jobs em Processing não são retomados pelo Worker; sua recuperação continua para avaliação posterior.

As transações da engine não envolvem o handler. Encerrar o host não desfaz efeitos de negócio que o handler já confirmou. Se a operação terminar e a gravação da finalização falhar, o Job pode permanecer Processing mesmo com o efeito realizado. Considere isso ao definir a operação e qualquer futura estratégia de reprocessamento; uma execução aberta por Job não garante efeitos exatamente uma vez.

`core.job` armazena o estado atual; `core.job_execution` armazena StartedAt, FinishedAt, Result, Error e Details, sem Status próprio. Datas representam instantes UTC. O histórico comporta várias execuções, mas a Fase 1 não oferece um comando para criar novas tentativas de um Job já processado.

## Inicialização e configuração

O executável local se chama **Yalla - Worker.exe**. Depois de iniciar pelo script habitual, localizar esse nome na janela Attach to Process do depurador. A alteração passa a valer na próxima compilação/inicialização; uma instância já aberta conserva seu nome anterior. Os scripts de parada reconhecem os dois nomes durante a transição. Não anexar ao processo `dotnet.exe` que apenas inicia a aplicação: o executável do Worker é o alvo da depuração.

Execute da raiz do repositório:

```powershell
# Somente Worker
.\scriptsPS\suporte\Start-Worker.ps1

# API, frontend e Worker com terminais visíveis
.\scriptsPS\00-Restart-Backend-Frontend_terminal_opened.ps1
```

O script do Worker carrega a configuração local de `.env`. A composição exige `ConnectionStrings:YallaFinance`; conexão ausente impede a inicialização.

```json
{
  "Jobs": {
    "BatchSize": 10,
    "PollingIntervalSeconds": 5
  }
}
```

BatchSize limita as tentativas de busca por ciclo, sem criar paralelismo. Com fila vazia ou falha de infraestrutura no ciclo, o Worker espera PollingIntervalSeconds antes de consultar novamente. Ambos os valores devem ser positivos. Sem Jobs, o processo permanece ativo; não é necessário autenticar ou disparar uma execução manualmente.

Os logs correlacionam tenant, Job, execução e Type; a conclusão inclui duração e sucesso. Payload, Key, Result e Details não são incluídos automaticamente nos logs da engine. Logs adicionais do handler devem seguir o mesmo cuidado com dados sensíveis.

Aplicação de SQL e scaffold seguem o procedimento manual do projeto; os scripts de inicialização não os executam.

## Testes para uma nova funcionalidade

Comece com testes unitários sem banco, conforme o escopo atual:

- Definição: Type, Key e payload esperados.
- Handler: sucesso, validação de negócio, tenant e cancelamento.
- Composição: tipo disponível no criador e Worker, dependências resolvidas no contexto correto.
- Fluxo: criação Pending sem executar handler e processamento com persistência substituta de teste.
- Segurança: retorno e logs sem credenciais ou dados indevidos.

Exemplos da engine estão em [tests/YallaFinance.UnitTests/Jobs](../../tests/YallaFinance.UnitTests/Jobs). Para testes novos sem banco, selecione explicitamente suas classes pelo filtro:

```powershell
dotnet test tests/YallaFinance.UnitTests --filter FullyQualifiedName~ProcessDocumentHandlerTests
```

O nome acima é ilustrativo e deve corresponder à classe de teste criada.

Por decisão do responsável em 2026-10-05, todos os testes dependentes de banco e suas fixtures/gates foram removidos. A verificação desta entrega usa somente testes unitários sem banco. Locks, transações e comportamento real da Nibo não são comprovados por esses testes; uma validação futura exigirá planejamento próprio.

## Checklist antes de conectar o fluxo real

- [ ] Type estável, contrato persistido e Key definidos.
- [ ] Handler implementado com validações e dependências disponíveis no Worker.
- [ ] Tipo registrado uma vez em cada host envolvido.
- [ ] Tenant autorizado na origem e aplicado nas operações do handler.
- [ ] Ponto de criação definido, considerando a transação de negócio.
- [ ] Result, Error e Details seguros e compatíveis com JSON.
- [ ] Comportamento de cancelamento e efeitos já confirmados compreendido.
- [ ] Testes unitários do fluxo e da composição aprovados.
- [ ] Validação PostgreSQL pendente considerada para a adoção operacional.

## Documentação relacionada

- [Operação e scripts](../../scriptsPS/README.md)
- [Requisitos da Fase 1](../../FEATURES/07%20-%20Uso%20da%20Integracao%20Financeira%20e%20Persistencia/FASE%201%20-%20REQUISITOS.md)
- [Plano e situação da implementação](../../FEATURES/07%20-%20Uso%20da%20Integracao%20Financeira%20e%20Persistencia/FASE%201%20-%20PLANO_EXECUCAO.md)
- [Validação e resultados registrados](../../FEATURES/07%20-%20Uso%20da%20Integracao%20Financeira%20e%20Persistencia/FASE%201%20-%20VALIDACAO.md)
- [Diagrama da Fase 1](../../FEATURES/07%20-%20Uso%20da%20Integracao%20Financeira%20e%20Persistencia/FASE%201%20-%20DIAGRAMA.png)
- [Pendências para avaliação posterior](../../FEATURES/07%20-%20Uso%20da%20Integracao%20Financeira%20e%20Persistencia/PENDENCIAS%20-%20FASE%202.md)
