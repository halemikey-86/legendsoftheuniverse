import { useRef, useState } from "react";
import { deleteCardImage, uploadCardImage } from "@/lib/api-client";
import type { Card } from "@/lib/cards";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";

const ACCEPT = "image/jpeg,image/png,.jpg,.jpeg,.png";

export function CardImageUpload({
  card,
  slot,
  label,
  onUpdated,
}: {
  card: Card;
  slot: "front" | "back";
  label: string;
  onUpdated: (card: Card) => void;
}) {
  const inputRef = useRef<HTMLInputElement>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const previewUrl = slot === "front" ? card.frontImageUrl : card.backImageUrl;
  const canUpload = Boolean(card.id?.trim());

  async function onPick(file: File | undefined) {
    if (!file || !canUpload) return;
    setBusy(true);
    setError(null);
    try {
      const { card: updated } = await uploadCardImage(card.id, slot, file);
      onUpdated(updated);
    } catch (err) {
      const message = err instanceof Error ? err.message : "Upload failed";
      if (message === "Unauthorized") {
        setError(
          "Unauthorized — click Admin access in the header and paste the shared admin token from your team lead.",
        );
      } else if (message === "Failed to fetch" || message.includes("NetworkError")) {
        setError(
          "Could not reach the catalog API — check your connection. If this is a preview (.pages.dev) link, wait a minute and retry after the API update.",
        );
      } else {
        setError(message);
      }
    } finally {
      setBusy(false);
      if (inputRef.current) inputRef.current.value = "";
    }
  }

  async function onRemove() {
    if (!canUpload || !previewUrl) return;
    setBusy(true);
    setError(null);
    try {
      const { card: updated } = await deleteCardImage(card.id, slot);
      onUpdated(updated);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Remove failed");
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="flex flex-col gap-2 rounded-lg border border-border bg-secondary/40 p-3">
      <Label>{label}</Label>
      <p className="text-xs text-muted-foreground">JPG or PNG, max 8 MB.</p>

      {previewUrl ? (
        <img
          src={previewUrl}
          alt={`${card.name} ${slot}`}
          className="mx-auto max-h-40 rounded-md border border-border object-contain"
        />
      ) : (
        <div className="flex h-28 items-center justify-center rounded-md border border-dashed border-border text-xs text-muted-foreground">
          No {slot} image
        </div>
      )}

      {!canUpload ? (
        <p className="text-xs text-amber-700">Save the card first (needs an engine id).</p>
      ) : null}

      <div className="flex flex-wrap gap-2">
        <input
          ref={inputRef}
          type="file"
          accept={ACCEPT}
          className="hidden"
          onChange={(e) => onPick(e.target.files?.[0])}
        />
        <Button
          type="button"
          variant="secondary"
          size="sm"
          disabled={busy || !canUpload}
          onClick={() => inputRef.current?.click()}
        >
          {busy ? "Uploading…" : previewUrl ? "Replace" : "Upload"}
        </Button>
        {previewUrl ? (
          <Button type="button" variant="ghost" size="sm" disabled={busy} onClick={onRemove}>
            Remove
          </Button>
        ) : null}
      </div>

      {error ? <p className="text-xs text-destructive">{error}</p> : null}
    </div>
  );
}
