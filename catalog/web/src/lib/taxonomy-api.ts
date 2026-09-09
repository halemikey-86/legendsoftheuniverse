import { getAdminToken } from "@/lib/admin-token";
import { API_BASE } from "@/lib/api-base";

export type TaxonomyKind = "set" | "series";

export type TaxonomyEntry = {
  id: number;
  kind: TaxonomyKind;
  name: string;
  code: string | null;
  createdAt: string;
};

function authHeaders(json = true): HeadersInit {
  const headers: Record<string, string> = {};
  if (json) headers["Content-Type"] = "application/json";
  const token = getAdminToken();
  if (token) headers.Authorization = `Bearer ${token}`;
  return headers;
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, init);
  const body = (await res.json().catch(() => ({}))) as T & { error?: string };
  if (!res.ok) throw new Error(body.error ?? `Request failed (${res.status})`);
  return body;
}

export async function fetchTaxonomy(): Promise<{
  sets: string[];
  series: string[];
  entries: TaxonomyEntry[];
}> {
  return request("/api/taxonomy");
}

export async function createTaxonomyEntry(input: {
  kind: TaxonomyKind;
  name: string;
  code?: string | null;
}): Promise<{ entry: TaxonomyEntry; sets: string[]; series: string[] }> {
  return request("/api/taxonomy", {
    method: "POST",
    headers: authHeaders(),
    body: JSON.stringify(input),
  });
}

export async function removeTaxonomyEntry(id: number): Promise<{
  sets: string[];
  series: string[];
}> {
  return request(`/api/taxonomy/${id}`, {
    method: "DELETE",
    headers: authHeaders(false),
  });
}
