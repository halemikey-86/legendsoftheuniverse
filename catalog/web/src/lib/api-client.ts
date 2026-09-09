const API_BASE = import.meta.env.VITE_API_BASE ?? "";
const ADMIN_TOKEN = import.meta.env.VITE_ADMIN_TOKEN ?? "";

function authHeaders(): HeadersInit {
  const headers: Record<string, string> = {};
  if (ADMIN_TOKEN) headers.Authorization = `Bearer ${ADMIN_TOKEN}`;
  return headers;
}

export async function uploadCardImage(
  cardId: string,
  slot: "front" | "back",
  file: File,
): Promise<{ card: import("./card-schema.js").Card }> {
  const form = new FormData();
  form.append("file", file, file.name);

  const res = await fetch(`${API_BASE}/api/cards/${encodeURIComponent(cardId)}/images/${slot}`, {
    method: "POST",
    headers: authHeaders(),
    body: form,
  });

  const body = await res.json();
  if (!res.ok) throw new Error(body.error ?? "Upload failed");
  return body;
}

export async function deleteCardImage(
  cardId: string,
  slot: "front" | "back",
): Promise<{ card: import("./card-schema.js").Card }> {
  const res = await fetch(`${API_BASE}/api/cards/${encodeURIComponent(cardId)}/images/${slot}`, {
    method: "DELETE",
    headers: authHeaders(),
  });

  const body = await res.json();
  if (!res.ok) throw new Error(body.error ?? "Delete failed");
  return body;
}

export { API_BASE };
