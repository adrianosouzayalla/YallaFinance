import { directoryApi } from "@/services/api/directoryApi";
import type { DirectoryPage, UserItem } from "@/services/api/users/types";
import type { ProfileItem } from "@/services/api/users/types";

interface UserWriteInput {
  name: string;
  email: string | null;
  profileId: string | null;
}

export const usersApi = {
  list: (query: string, contextId: string, signal?: AbortSignal) =>
    directoryApi.get<DirectoryPage<UserItem>>(`/api/users?${query}`, contextId, signal),
  get: (userId: string, contextId: string, signal?: AbortSignal) =>
    directoryApi.get<UserItem>(`/api/users/${userId}`, contextId, signal),
  activeProfiles: (page: number, contextId: string, signal?: AbortSignal) =>
    directoryApi.get<DirectoryPage<ProfileItem>>(
      `/api/access-profiles?active=true&page=${page}&pageSize=100`,
      contextId,
      signal,
    ),
  initialPassword: (contextId: string, signal?: AbortSignal) =>
    directoryApi.get<{ password: string | null }>("/api/users/initial-password", contextId, signal),
  create: (input: unknown, contextId: string) =>
    directoryApi.send<UserItem>("/api/users", "POST", input, contextId),
  update: (userId: string, input: UserWriteInput, contextId: string) =>
    directoryApi.send(`/api/users/${userId}`, "PUT", input, contextId),
  setActive: (userId: string, active: boolean, contextId: string) =>
    directoryApi.send(
      `/api/users/${userId}/active`,
      "PUT",
      { active, confirmGlobalImpact: true },
      contextId,
    ),
  resetPassword: (userId: string, contextId: string) =>
    directoryApi.send(
      `/api/users/${userId}/password`,
      "POST",
      { confirmGlobalImpact: true },
      contextId,
    ),
};
