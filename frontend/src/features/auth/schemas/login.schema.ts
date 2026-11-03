import { z } from "zod";
// The legacy email transport field carries core.user.login, without email-format validation.
export const loginSchema = z.object({
  email: z.string(),
  password: z.string(),
  tenantId: z.string().optional(),
});
