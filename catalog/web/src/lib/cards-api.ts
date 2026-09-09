import { getAdminToken } from "@/lib/admin-token";
import { API_BASE } from "@/lib/api-base";
import type { Card, CardInput, CardSearch } from "./card-schema";

/** Serve uploads from the same host the UI uses for API calls (Fly URL until custom DNS is live). */
function normalizeImageUrl(url: string | null | undefined): string | null {
  if (!url) return null;
  if (!API_BASE) return url;
  const uploadsIndex = url.indexOf("/uploads/");
  if (uploadsIndex === -1) return url;
  return `${API_BASE.replace(/\/$/, "")}${url.slice(uploadsIndex)}`;
}

function withImageUrls<T extends Card>(card: T): T {
  return {
    ...card,
    frontImageUrl: normalizeImageUrl(card.frontImageUrl),
    backImageUrl: normalizeImageUrl(card.backImageUrl),
  };
}

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
  if (!res.ok) {
    throw new Error(body.error ?? `Request failed (${res.status})`);
  }
  return body;
}

function toQuery(filters: CardSearch): string {
  const params = new URLSearchParams();
  for (const [key, value] of Object.entries(filters)) {
    if (value != null && value !== "") params.set(key, String(value));
  }
  const qs = params.toString();
  return qs ? `?${qs}` : "";
}

export async function searchCards(filters: CardSearch): Promise<Card[]> {
  const cards = await request<Card[]>(`/api/cards${toQuery(filters)}`);
  return cards.map(withImageUrls);
}

export async function getLibraryStats(): Promise<{ total: number }> {
  return request<{ total: number }>("/api/cards/stats");
}

export async function createCard(input: CardInput): Promise<Card> {
  const result = await request<{ card: Card }>("/api/cards", {
    method: "POST",
    headers: authHeaders(),
    body: JSON.stringify(input),
  });
  return withImageUrls(result.card);
}

export async function updateCard(input: CardInput & { originalId: string }): Promise<Card> {
  const { originalId, ...data } = input;
  const result = await request<{ card: Card }>(
    `/api/cards/${encodeURIComponent(originalId)}`,
    {
      method: "PUT",
      headers: authHeaders(),
      body: JSON.stringify(data),
    },
  );
  return withImageUrls(result.card);
}

export async function deleteCard(id: string): Promise<void> {
  await request(`/api/cards/${encodeURIComponent(id)}`, {
    method: "DELETE",
    headers: authHeaders(false),
  });
}

export async function fetchEngineExport(): Promise<{ cards: Record<string, unknown>[] }> {
  return request("/api/cards/export");
}

export { API_BASE };
