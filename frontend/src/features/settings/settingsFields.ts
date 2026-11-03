// Form definitions describe supported capabilities, never operational records or defaults.
export type SettingsField = {
  id: string;
  label: string;
  helper?: string;
  required?: boolean;
  kind?: "number" | "secret" | "multiline" | "select";
  options?: readonly { value: string; label: string }[];
  min?: number;
  max?: number;
  maxLength?: number;
  readOnly?: boolean;
  source?: { group: string; key: string };
};

export type SettingsSection = {
  title: string;
  description?: string;
  fields: readonly SettingsField[];
  companyGroup?: "payments" | "session" | "security" | "user-registration" | "regional-settings" | "financial-settlement";
};

export const companySections: readonly SettingsSection[] = [
  { title: "Pagamentos", companyGroup: "payments", fields: [
    { id: "EMP-01", label: "Prazo máximo do link (dias)", kind: "number", required: true, min: 1, max: 365, helper: "De 1 a 365 dias." },
    { id: "EMP-02", label: "Origem pública do checkout", maxLength: 2048, helper: "Endereço público utilizado nos links de pagamento." },
  ] },
  { title: "Sessão", companyGroup: "session", fields: [
    { id: "EMP-03", label: "Duração da sessão (minutos)", kind: "number", required: true, min: 1, max: 1440, helper: "De 1 a 1.440 minutos, para novas sessões." },
  ] },
  { title: "Segurança", companyGroup: "security",
    description: "Protege as contas contra tentativas repetidas de login. Quando um e-mail atinge o máximo de falhas dentro da janela, novas tentativas desse e-mail são recusadas até a janela expirar. Valores recomendados: 10 falhas em 15 minutos. Contas que não existem não são bloqueadas. Enquanto a empresa não gravar valores, o sistema usa 10 falhas em 15 minutos e registra um aviso no log. Observação: a contagem de falhas fica na memória da API; ao reiniciar a API, os bloqueios são zerados.",
    fields: [
    { id: "EMP-06", label: "Máximo de falhas de login por e-mail", kind: "number", required: true, min: 1, max: 100, helper: "De 1 a 100 falhas." },
    { id: "EMP-07", label: "Janela de contagem de falhas (minutos)", kind: "number", required: true, min: 1, max: 1440, helper: "De 1 a 1.440 minutos." },
  ] },
  { title: "Cadastro de usuários", companyGroup: "user-registration", fields: [
    { id: "EMP-04", label: "Senha inicial de usuários", kind: "secret", helper: "Utilizada no cadastro de novos usuários. Informe para substituir." },
  ] },
  { title: "Configuração regional", companyGroup: "regional-settings", fields: [
    { id: "EMP-05", label: "Fuso horário padrão", required: true, helper: "Usado para converter o horário do recebimento na data local da baixa financeira." },
  ] },
];
