export interface UserItem {
  id: string; login: string; name: string; email: string | null; active: boolean;
  accessActive: boolean; profileId: string | null; profileName: string | null;
}
export interface FunctionalityOption { id: string; code: string; name: string; description: string; }
export interface ProfileItem { id: string; code: string; name: string; active: boolean; functionalityIds?: string[]; }
export interface DirectoryPage<T> { items: T[]; totalCount: number; page: number; pageSize: number; }
export interface DirectoryFilters { search: string; page: number; pageSize: number; sortBy: string; sortDirection: "asc" | "desc"; }

