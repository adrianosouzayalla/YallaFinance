import type { SettingsSection } from "../../settingsFields";

export const sicoobSections: readonly SettingsSection[] = [
  { title: "Ambiente", fields: [
    { id: "SIC-01", source: { group: "Connection", key: "Environment" }, label: "Ambiente", kind: "select", required: true,
      options: [{ value: "Production", label: "Produção" }], helper: "Somente o ambiente de produção é suportado." },
  ] },
  { title: "Conexão", fields: [
    { id: "SIC-02", source: { group: "Connection", key: "BaseUrl" }, label: "URL base", required: true, helper: "HTTPS em api.sicoob.com.br, caminho /pix/api/v2." },
  ] },
  { title: "Autenticação", fields: [
    { id: "SIC-03", source: { group: "Authentication", key: "ClientId" }, label: "Client ID", kind: "secret", required: true, helper: "Identificador da aplicação autorizada no Sicoob." },
    { id: "SIC-04", source: { group: "Authentication", key: "TokenUrl" }, label: "URL do token", required: true, helper: "HTTPS em auth.sicoob.com.br, caminho /auth/realms/cooperado/protocol/openid-connect/token." },
    { id: "SIC-05", source: { group: "Authentication", key: "Scopes" }, label: "Escopos", required: true, helper: "cob.read para consulta; cob.write também para emissão." },
  ] },
  { title: "Certificado e chave privada", fields: [
    { id: "SIC-06", source: { group: "Authentication", key: "CertificatePem" }, label: "Certificado PEM", kind: "multiline", required: true, helper: "Certificado persistido para esta conexão." },
    { id: "SIC-07", source: { group: "Authentication", key: "PrivateKeyPem" }, label: "Chave privada PEM", kind: "multiline", required: true, helper: "Chave privada persistida para esta conexão." },
    { id: "SIC-08", source: { group: "Authentication", key: "PrivateKeyPassword" }, label: "Senha da chave privada", kind: "secret", helper: "Opcional, conforme a proteção da chave privada." },
  ] },
  { title: "Pix e transporte", fields: [
    { id: "SIC-09", source: { group: "Pix", key: "ReceiverKey" }, label: "Chave Pix recebedora", maxLength: 77, helper: "Obrigatória para emissão; até 77 caracteres, sem espaços." },
    { id: "SIC-10", source: { group: "Pix", key: "ReceivedLookbackDays" }, label: "Período de consulta (dias)", kind: "number", min: 1, max: 31, helper: "De 1 a 31 dias, para consultas sem intervalo informado." },
    { id: "SIC-11", source: { group: "Transport", key: "RequestTimeoutSeconds" }, label: "Timeout (segundos)", kind: "number", required: true, min: 1, max: 60, helper: "De 1 a 60 segundos." },
  ] },
];
