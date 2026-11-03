import type { SettingsSection } from "../../settingsFields";

export const asaasSections: readonly SettingsSection[] = [
  { title: "Conexão", fields: [
    { id: "ASA-01", source: { group: "Connection", key: "BaseUrl" }, label: "URL base", required: true,
      helper: "https://api-sandbox.asaas.com/v3 ou https://api.asaas.com/v3, conforme a conta utilizada." },
  ] },
  { title: "Autenticação", fields: [
    { id: "ASA-02", source: { group: "Authentication", key: "ApiToken" }, label: "Token de API", kind: "secret", required: true,
      helper: "Informe o token de API da conta Asaas correspondente à URL configurada." },
  ] },
  { title: "Pix e conciliação", fields: [
    { id: "ASA-05", source: { group: "Pix", key: "ReceivedLookbackDays" }, label: "Período de consulta (dias)", kind: "number",
      min: 1, max: 31, helper: "De 1 a 31 dias, para consultar recebimentos Pix sem intervalo informado." },
  ] },
  { title: "Transporte", fields: [
    { id: "ASA-06", source: { group: "Transport", key: "RequestTimeoutSeconds" }, label: "Timeout (segundos)", kind: "number", required: true,
      min: 60, max: 300, helper: "De 60 a 300 segundos." },
  ] },
];
