import { z } from "zod";
// The legacy email transport field carries core.user.login, without email-format validation.
export const loginSchema = z.object({
  email: z.string(),
  password: z.string(),
  tenantId: z.string().optional(),
  // Filled only when the login requires the password to be replaced (feature 28, phase 3).
  newPassword: z.string().optional(),
  confirmPassword: z.string().optional(),
});
