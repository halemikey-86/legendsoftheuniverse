const STORAGE_KEY = "willbound-admin-token";

/** Suggested token from build env — shown in Admin access dialog, not sent until saved. */
export function getSuggestedAdminToken(): string {
  return import.meta.env.VITE_ADMIN_TOKEN?.trim() ?? "";
}

/** Token used on API requests — only values the user explicitly saved in this browser. */
export function getAdminToken(): string {
  if (typeof localStorage === "undefined") return "";
  return localStorage.getItem(STORAGE_KEY)?.trim() ?? "";
}

export function setAdminToken(token: string): void {
  const trimmed = token.trim();
  if (typeof localStorage === "undefined") return;
  if (!trimmed) {
    localStorage.removeItem(STORAGE_KEY);
    return;
  }
  localStorage.setItem(STORAGE_KEY, trimmed);
}

export function clearAdminToken(): void {
  if (typeof localStorage !== "undefined") localStorage.removeItem(STORAGE_KEY);
}

export function hasAdminToken(): boolean {
  return getAdminToken().length > 0;
}
