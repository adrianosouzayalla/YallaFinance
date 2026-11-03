import { JobDetailClient } from "@/features/jobs/components/JobDetailClient";
export default async function JobPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  return <JobDetailClient jobId={id} />;
}
