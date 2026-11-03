import { UserFormPage } from "@/features/users/components/UserFormPage";
export default async function Page({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  return <UserFormPage userId={id} />;
}
