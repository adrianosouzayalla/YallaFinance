import { ProfileFormPage } from "@/features/access-profiles/components/ProfileFormPage";

export default async function EditProfilePage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  return <ProfileFormPage profileId={id} />;
}
