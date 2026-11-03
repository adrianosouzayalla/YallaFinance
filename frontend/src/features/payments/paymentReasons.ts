const reasons: Record<string, string> = {
  OpenFinancialRisk: "Pagamento em andamento",
  ActiveRequestExists: "Aguardando pagamento",
  MissingItems: "Cobrança sem itens",
  MultipleContracts: "Cobrança com múltiplos contratos",
  InconsistentAmount: "Valor inconsistente",
  IneligibleDebts: "Débitos ou contrato indisponíveis",
};
export function paymentBlockLabel(reason: string | null): string { return reason ? reasons[reason] ?? "Indisponível" : "Disponível"; }
