import type { SettingsSection } from "../../settingsFields";

export const niboSections: readonly SettingsSection[] = [
  { title: "Conexão", fields: [
    { id: "NIB-01", source: { group: "Connection", key: "BaseUrl" }, label: "URL base", required: true, helper: "Endereço absoluto HTTP ou HTTPS, sem usuário, parâmetros ou fragmento." },
    { id: "NIB-02", source: { group: "Connection", key: "CompanyId" }, label: "Identificador da empresa no Nibo", helper: "Opcional nas consultas atuais. Não é o identificador da empresa no Yalla." },
  ] },
  { title: "Autenticação", fields: [
    { id: "NIB-03", source: { group: "Authentication", key: "ApiToken" }, label: "Token de API", kind: "secret", required: true, helper: "Informe um novo token para substituir a credencial." },
  ] },
];
