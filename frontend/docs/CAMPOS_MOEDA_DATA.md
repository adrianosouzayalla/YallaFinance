# Campos compartilhados de moeda e data

Os componentes em `src/components/forms/` são independentes de features, endpoints e regras de negócio. Utilizam Material UI e `react-number-format` para edição mascarada. Cada tela continua responsável por obrigatoriedade, valores permitidos, datas de negócio e schemas Zod.

## CurrencyField

Importar `CurrencyField` de `@/components/forms/CurrencyField`.

- `value`: `number | null`; `onChange` recebe o número normalizado ou `null` ao limpar.
- Exibição brasileira: `1.234,56`, com duas casas, sem acrescentar símbolo monetário ao campo existente.
- A digitação preenche pelos centavos, sem exigir vírgula: `1` → `0,01`, `12` → `0,12`, `123` → `1,23`. Apagar o último dígito desloca o valor de volta.
- Colar `123456` ou `1.234,56` resulta em `1.234,56`. Os separadores são ignorados na entrada; o valor externo continua numérico em reais, e não em centavos.
- Permite valores negativos no componente genérico; a feature define sua aceitação.
- Campo vazio permanece vazio. A cobrança avulsa converte `null` em zero apenas no seu draft existente para preservar a validação e o contrato atuais.
- Aceita propriedades do `TextField` como `name`, `label`, `size`, `disabled`, `required`, `error`, `helperText`, `onBlur`, `inputRef` e `sx`.
- A máscara limita a entrada a duas casas decimais. A feature/backend continuam responsáveis por precisão financeira, limites e cálculos; o componente não altera arredondamento ou idempotência.

```tsx
<CurrencyField
  label="Valor"
  value={amount}
  onChange={setAmount}
  error={Boolean(errors.amount)}
  helperText={errors.amount}
/>
```

## DateField

Importar `DateField` de `@/components/forms/DateField`.

- `value`: data civil ISO `aaaa-mm-dd` ou `null`; `onChange` recebe ISO ou `null`.
- `mode="native"` é o padrão: conserva o calendário nativo utilizado pelas telas atuais.
- `mode="masked"` permite digitar `dd/mm/aaaa`; a formatação automática preserva digitação parcial e valida datas reais, inclusive anos bissextos.
- Datas incompletas ou impossíveis no modo mascarado produzem `null`, mantendo o texto para correção. A máscara não transforma `31/02` em uma data de março.
- `min` e `max` recebem ISO; no modo nativo viram atributos do input, no mascarado geram feedback de erro. A validação final do período continua no formulário/backend.
- As conversões não utilizam fuso horário nem transformam uma data civil em timestamp.
- Limpar o campo produz `null`. Nas features existentes, o consumidor converte `null` para a string vazia definida nos seus DTOs/drafts.
- A entrada nativa mantém o valor HTML em ISO; sua apresentação visual depende da configuração do navegador.

```tsx
<DateField label="Data" value={date} onChange={setDate} />
<DateField label="Data" mode="masked" value={date} onChange={setDate} />
```

## React Hook Form

Para contratos que precisam preservar toda a faixa de `numeric(18,2)`, usar o modo opcional `exactDecimal` de `CurrencyField`. Nesse modo `value` e `onChange` usam texto decimal com ponto (por exemplo, `"123.45"`) ou null; apresentação e digitação continuam brasileiras. Os utilitários `decimalToCents`, `centsToDecimal` e `formatExactCurrencyInput` não convertem o valor para Number. O cadastro manual de débito utiliza esse modo e envia o valor decimal como string JSON, convertido para `decimal` no backend. O modo numérico existente permanece padrão para os consumidores atuais.

```tsx
<CurrencyField exactDecimal label="Valor" value={amountText} onChange={setAmountText} />
```

Os componentes funcionam sem React Hook Form. Para integrá-los com `Controller`, encaminhar `ref` como `inputRef` para o elemento de entrada:

```tsx
<Controller
  name="amount"
  control={form.control}
  render={({ field: { ref, ...field }, fieldState }) => (
    <CurrencyField
      {...field}
      inputRef={ref}
      label="Valor"
      error={Boolean(fieldState.error)}
      helperText={fieldState.error?.message}
    />
  )}
/>
```

Usar valores iniciais explícitos (`null` para campos vazios). O formulário define como mapear o valor normalizado para seu contrato e como tratar erros de negócio. Para reiniciar também uma edição parcial inválida de data mascarada cujo valor normalizado já era `null`, remontar o campo com uma `key` do ciclo de reset; mudar para outra data válida atualiza o campo normalmente.

## Utilitários

- `@/utils/currency`: `formatCurrency` para exibição com `R$` e `formatCurrencyInput` para representação decimal brasileira sem símbolo.
- `@/utils/date`: `formatDate` (ISO para `dd/mm/aaaa`), `parseDateInput` (máscara completa para ISO ou `null`) e `isValidDateOnly`.
- Moeda/data vazias são formatadas como string vazia. Utilitários de data civil não devem receber timestamps.

## Integração entregue

`NewChargeClient` utiliza `CurrencyField` e `DateField`; `ChargeReviewClient` e `DebtFilters` utilizam `DateField`. Datas continuam nativas por padrão. Valores monetários e datas de exibição nas telas foram centralizados nos utilitários, preservando o formato brasileiro. Regras, schemas, DTOs, payloads, seleção da tabela e idempotência permanecem nas features existentes.

Referência da biblioteca de máscara: [React Number Format](https://s-yadav.github.io/react-number-format/docs/intro/).
