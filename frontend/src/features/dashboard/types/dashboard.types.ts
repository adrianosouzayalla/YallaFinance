export interface DashboardMetric {
  count: number;
  amount: number;
}

export interface DashboardSummary {
  awaitingCharge: DashboardMetric;
  activeCharges: DashboardMetric;
  overdue: DashboardMetric;
  staleCharges: DashboardMetric;
}
