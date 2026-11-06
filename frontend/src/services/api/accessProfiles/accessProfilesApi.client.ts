import { directoryApi } from "@/services/api/directoryApi";
import type { DirectoryPage, FunctionalityOption, ProfileItem } from "@/services/api/users/types";

interface ProfileWriteInput {
  code: string;
  name: string;
  active: boolean;
  functionalityIds: string[];
}

export const accessProfilesApi = {
  list: (query: string, contextId: string, signal?: AbortSignal) =>
    directoryApi.get<DirectoryPage<ProfileItem>>(
      `/api/access-profiles?${query}`,
      contextId,
      signal,
    ),
  get: (profileId: string, contextId: string, signal?: AbortSignal) =>
    directoryApi.get<ProfileItem>(`/api/access-profiles/${profileId}`, contextId, signal),
  functionalities: (contextId: string, signal?: AbortSignal) =>
    directoryApi.get<FunctionalityOption[]>(
      "/api/access-profiles/functionalities",
      contextId,
      signal,
    ),
  create: (input: ProfileWriteInput, contextId: string) =>
    directoryApi.send<ProfileItem>("/api/access-profiles", "POST", input, contextId),
  update: (
    profileId: string,
    input: { name: string; functionalityIds: string[] },
    contextId: string,
  ) => directoryApi.send(`/api/access-profiles/${profileId}`, "PUT", input, contextId),
  setActive: (profileId: string, active: boolean, contextId: string) =>
    directoryApi.send(`/api/access-profiles/${profileId}/active`, "PUT", { active }, contextId),
};
