// @vitest-environment node
import { expect, it, vi } from "vitest";
import { parseSessionProfile, assertCookieToken } from "./session-cookie";
vi.mock("server-only", () => ({}));
const token = Buffer.alloc(32, 3).toString("base64url");
const profile = { authenticated: true, userId: "user-a", displayName: "Administrador", tenantId: "tenant-a", contextId: "session-a",
  organization: "Yalla", expiresAt: "2099-01-01T00:00:00Z" };
it("reads presentation data from the validated API response without decoding a JWT", () => {
  expect(parseSessionProfile(token, profile)).toEqual(expect.objectContaining({
    token, userId: "user-a", displayName: "Administrador", contextId: "session-a", tenantId: "tenant-a", organization: "Yalla",
  }));
});
it("normalizes API grant codes and safely defaults missing or malformed lists to none", () => {
  expect(parseSessionProfile(token, { ...profile, functionalityCodes: ["home", "DEBTS", 2] })?.functionalityCodes)
    .toEqual(["HOME", "DEBTS"]);
  expect(parseSessionProfile(token, { ...profile, functionalityCodes: "HOME" })?.functionalityCodes).toEqual([]);
  expect(parseSessionProfile(token, profile)?.functionalityCodes).toEqual([]);
});
it("rejects JWTs, plain IDs, malformed tokens and missing or expired API metadata", () => {
  for (const value of ["header.payload.signature", "session-a", "../secret", "a".repeat(4000)])
    expect(() => assertCookieToken(value)).toThrow();
  for (const data of [{ ...profile, contextId: undefined }, { ...profile, expiresAt: "2000-01-01" }, { ...profile, authenticated: false }])
    expect(parseSessionProfile(token, data)).toBeNull();
  expect(() => assertCookieToken(token)).not.toThrow();
});
