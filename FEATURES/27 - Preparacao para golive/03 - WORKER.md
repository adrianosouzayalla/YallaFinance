# 03 — Worker

## Requisitos

- .NET 10 (runtime).
- Acesso ao mesmo PostgreSQL da API. Ver [01 - BANCO.md](01%20-%20BANCO.md).
- Saída de rede para os provedores (Nibo, Asaas e Sicoob).

## Publicação

```powershell
dotnet publish .\src\YallaFinance.Worker -c Release -o <pasta-de-publicacao>
```

Execução:

```powershell
dotnet <pasta-de-publicacao>\YallaFinance.Worker.dll
```

## Variáveis de ambiente

| Variável | Valor | Obrigatória |
|---|---|---|
| `ConnectionStrings__YallaFinance` | Mesma connection string da API | Sim |
| `DOTNET_ENVIRONMENT` | `Production` | Sim |

## Configurações padrão

| Chave | Padrão | Observação |
|---|---|---|
| `Jobs:BatchSize` | 10 | Jobs por ciclo |
| `Jobs:PollingIntervalSeconds` | 5 | Intervalo de consulta à fila |

Sincronização de recebimentos Pix roda a cada 15 segundos, com janela de 7 dias por conexão.

## Instâncias

- Pode haver mais de uma instância. O consumo de Jobs usa bloqueio por linha, então um Job não é processado duas vezes.

## Não definir em produção

- `FinancialIntegrations:EvidenceDirectory`. Quando definido, o Worker grava em disco o corpo das requisições ao Nibo. O token é redigido, mas os dados do cliente ficam no arquivo. Só ativar com decisão formal.

## Verificação

- Logs de início sem erro no console.
- Jobs com status `Pending` saem de `Pending` após alguns ciclos.
