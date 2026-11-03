# Tabela compartilhada com seleção entre páginas

**Padrão obrigatório para todas as telas operacionais com tabelas**, aprovado pelo usuário em 03/10/2026 após validar seleção e paginação na tela de débitos. Novas telas e alterações nas tabelas existentes devem reutilizar este componente; necessidades adicionais devem evoluí-lo, mantendo a independência de regras de negócio. A regra está no [contrato arquitetural vigente, seção 10.1](ARQUITETURA_PADRAO_FRONTEND_YALLA_FINANCE_FINAL.md#101-padrão-obrigatório-de-tabelas-operacionais).

`src/components/ui/SelectablePaginatedTable.tsx` é uma tabela genérica, sem dependência de features, endpoints ou regras financeiras. `src/hooks/usePaginatedSelection.ts` guarda os registros selecionados independentemente da página visível.

## Contrato

- `rows`: registros da página atual no modo servidor; conjunto completo no modo cliente. A busca permanece na tela/hook da feature.
- `columns`: identificador estável, cabeçalho, renderização, alinhamento e chave opcional de ordenação. **Cabeçalhos de dados ordenam por padrão**, alternando crescente/decrescente. Colunas de ações usam `sortable: false`. Use `getSortValue` para valores brutos quando o identificador não corresponder ao campo, para datas e para conteúdo renderizado em componentes; dinheiro ordena pelo número, nunca pelo texto formatado.
- `getRowId`: identifica cada registro com string ou número único e estável em todas as páginas.
- `selectedRows` / `onSelectionChange`: seleção controlada contendo registros de todas as páginas.
- `isRowSelectable`: regra de elegibilidade fornecida pela tela; por padrão todos os registros são elegíveis.
- `pagination`: página, tamanho, quantidade total e callback de mudança; a tabela não busca dados. O modo cliente recorta o conjunto completo depois de ordenar.
- `sortBy`, `sortDirection`, `onSort`: ordenação controlada pela tela para listas paginadas no servidor. Todas as colunas de dados devem possuir uma chave aceita pela API; ordenar o conjunto filtrado antes de LIMIT/OFFSET, com desempate estável. Voltar à primeira página ao ordenar, preservando seleção.
- Sem `onSort`, a tabela mantém a ordenação local. Para dados integralmente carregados, usar `paginationMode="client"` e fornecer **todas as linhas**: o componente ordena antes de recortar a página e retorna à página 1 ao clicar no cabeçalho. Não passar somente a página atual como se fosse o conjunto completo.
- `selectionActions`: área de resumo/ações fornecida pela tela, mantida mesmo quando a página atual está vazia.
- `loading`: bloqueia seleção, ordenação e paginação enquanto os dados estão carregando.
- `onRowClick`: comportamento opcional por tela para abrir o registro ao clicar na linha; a tabela ignora cliques em botões, links, campos interativos e seletores. Linhas configuradas também aceitam Enter e Espaço pelo teclado. `getRowClickLabel` fornece um rótulo acessível quando necessário.
- Rótulos acessíveis e conteúdo vazio podem ser personalizados pela tela.

O checkbox do cabeçalho seleciona/desmarca somente os registros elegíveis da página atual, preservando os registros das outras páginas. IDs já selecionados não são duplicados. A tabela não limpa a seleção ao paginar ou ordenar.

## Exemplo de consumo

O exemplo abaixo recebe dados já paginados e não depende de débitos:

```tsx
type Contact = { code: number; name: string; active: boolean };

function ContactsTable({ rows, page, totalItems, companyId, onPageChange }: {
  rows: Contact[];
  page: number;
  totalItems: number;
  companyId: string;
  onPageChange(page: number): void;
}) {
  const selection = usePaginatedSelection<Contact>({ scopeKey: companyId });

  return (
    <SelectablePaginatedTable
      rows={rows}
      columns={[{ id: "name", header: "Nome", render: row => row.name }]}
      getRowId={row => row.code}
      isRowSelectable={row => row.active}
      selectedRows={selection.selectedRows}
      onSelectionChange={selection.setSelectedRows}
      pagination={{ page, pageSize: 20, totalItems, onPageChange }}
      selectionActions={
        <button onClick={selection.clearSelection}>
          Limpar {selection.selectedRows.length} selecionados
        </button>
      }
    />
  );
}
```

Importar os dois recursos dos caminhos indicados no início deste documento. O componente consumidor deve executar no cliente.

## Limpeza e atualização

A tela decide quando chamar `clearSelection`, por exemplo ao aplicar novos filtros. O `scopeKey` do hook limpa imediatamente a seleção quando o contexto muda, sem restaurar seleções antigas ao retornar ao contexto anterior. Usar uma chave que represente a empresa/sessão da tela; não incluir a página ou ordenação nessa chave.

Os registros selecionados são os dados capturados no momento da seleção. Não são uma autorização para executar uma ação: cada feature continua responsável por validar o conjunto e o backend valida o estado atual antes de gravar. A tabela não consulta detalhes nem atualiza silenciosamente os registros selecionados.

## Tela de débitos

`DebtTable` configura colunas, formatação, links e elegibilidade (`Open`) sobre a tabela genérica. `DebtsPageClient` guarda a seleção completa, valida cliente/status e monta o link de revisão e o resumo. Paginação/ordenação preservam seleção; aplicação/limpeza de filtros e mudança de empresa/sessão limpam seleção. `SelectionBar` oferece a ação explícita de limpar.

Os testes do componente compartilhado usam contatos com IDs numéricos. Os testes da tela cobrem seleção entre páginas, total/link completos, seleção da página, clientes diferentes, ordenação e limpeza.

A leitura do catálogo usado na revisão/confirmação de cobranças percorre todas as páginas da API, sem limitar a resolução aos primeiros 1.000 débitos. Usa uma chave de cache própria para não confundir o catálogo completo com uma página da consulta. A carga respeita o cancelamento da sessão; o teste de regressão resolve um débito localizado após os primeiros 1.000 registros.
