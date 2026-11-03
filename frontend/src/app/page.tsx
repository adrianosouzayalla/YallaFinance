import { redirect } from "next/navigation";

export default async function RootPage() {
  //TODO: Adriano Integrar a decisão da rota inicial à sessão autenticada; hoje a raiz sempre redireciona para /login.
  redirect("/login");
}
