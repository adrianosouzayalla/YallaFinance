import { DebtDetailClient } from "@/features/debts/components/DebtDetailClient";

export default async function DebtDetailPage(props: PageProps<"/debts/[id]">) {
  const { id } = await props.params;
  return <DebtDetailClient debtId={id} />;
}
