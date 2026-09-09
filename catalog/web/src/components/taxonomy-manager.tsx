import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Plus, Tags, Trash2 } from "lucide-react";
import { toast } from "sonner";
import {
  createTaxonomyEntry,
  fetchTaxonomy,
  removeTaxonomyEntry,
} from "@/lib/taxonomy-api";
import type { TaxonomyEntry } from "@/lib/taxonomy-api";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

type Tab = "set" | "series";

function EntryList({
  entries,
  onRemove,
  busyId,
}: {
  entries: TaxonomyEntry[];
  onRemove: (id: number) => void;
  busyId: number | null;
}) {
  if (entries.length === 0) {
    return (
      <p className="rounded-md border border-dashed border-border px-3 py-6 text-center text-sm text-muted-foreground">
        No entries yet. Add one below.
      </p>
    );
  }

  return (
    <ul className="max-h-56 space-y-1 overflow-y-auto rounded-md border border-border bg-background/40 p-2">
      {entries.map((entry) => (
        <li
          key={entry.id}
          className="flex items-center justify-between gap-2 rounded-md px-2 py-1.5 hover:bg-secondary/60"
        >
          <div className="min-w-0">
            <p className="truncate text-sm text-foreground">{entry.name}</p>
            {entry.code ? (
              <p className="text-xs text-muted-foreground">Code: {entry.code}</p>
            ) : null}
          </div>
          <Button
            type="button"
            variant="ghost"
            size="icon"
            className="size-8 shrink-0 text-muted-foreground hover:text-destructive"
            disabled={busyId === entry.id}
            onClick={() => onRemove(entry.id)}
            aria-label={`Remove ${entry.name}`}
          >
            <Trash2 className="size-4" />
          </Button>
        </li>
      ))}
    </ul>
  );
}

export function TaxonomyManager({
  open,
  onOpenChange,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}) {
  const queryClient = useQueryClient();
  const [tab, setTab] = useState<Tab>("set");
  const [name, setName] = useState("");
  const [code, setCode] = useState("");

  const taxonomyQuery = useQuery({
    queryKey: ["taxonomy"],
    queryFn: fetchTaxonomy,
    enabled: open,
  });

  const createMutation = useMutation({
    mutationFn: createTaxonomyEntry,
    onSuccess: async (result) => {
      toast.success(`Added ${result.entry.name}`);
      setName("");
      setCode("");
      await queryClient.invalidateQueries({ queryKey: ["taxonomy"] });
    },
    onError: (err: Error) => toast.error(err.message || "Could not add entry"),
  });

  const deleteMutation = useMutation({
    mutationFn: removeTaxonomyEntry,
    onSuccess: async () => {
      toast.success("Removed");
      await queryClient.invalidateQueries({ queryKey: ["taxonomy"] });
    },
    onError: (err: Error) => toast.error(err.message || "Could not remove entry"),
  });

  const entries = taxonomyQuery.data?.entries ?? [];
  const setEntries = entries.filter((entry) => entry.kind === "set");
  const seriesEntries = entries.filter((entry) => entry.kind === "series");
  const activeEntries = tab === "set" ? setEntries : seriesEntries;

  function handleAdd(event: React.FormEvent) {
    event.preventDefault();
    const trimmed = name.trim();
    if (!trimmed) {
      toast.error("Enter a name");
      return;
    }
    createMutation.mutate({
      kind: tab,
      name: trimmed,
      code: tab === "set" ? code.trim() || null : null,
    });
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-lg">
        <DialogHeader>
          <DialogTitle>Sets &amp; series</DialogTitle>
          <DialogDescription>
            Add set and series names for card forms and filters. You can also type any new name
            directly when editing a card.
          </DialogDescription>
        </DialogHeader>

        <div className="flex gap-2">
          {(["set", "series"] as const).map((kind) => (
            <Button
              key={kind}
              type="button"
              variant={tab === kind ? "default" : "outline"}
              size="sm"
              onClick={() => setTab(kind)}
            >
              {kind === "set" ? "Sets" : "Series"}
            </Button>
          ))}
        </div>

        {taxonomyQuery.isLoading ? (
          <p className="text-sm text-muted-foreground">Loading…</p>
        ) : taxonomyQuery.isError ? (
          <p className="text-sm text-destructive">{(taxonomyQuery.error as Error).message}</p>
        ) : (
          <EntryList
            entries={activeEntries}
            onRemove={(id) => deleteMutation.mutate(id)}
            busyId={deleteMutation.isPending ? (deleteMutation.variables ?? null) : null}
          />
        )}

        <form className="grid gap-3 border-t border-border pt-4" onSubmit={handleAdd}>
          <div className="grid gap-1.5">
            <Label htmlFor="taxonomy-name">{tab === "set" ? "Set name" : "Series name"}</Label>
            <Input
              id="taxonomy-name"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder={tab === "set" ? "The Goblin King" : "GK"}
              maxLength={60}
              required
            />
          </div>
          {tab === "set" ? (
            <div className="grid gap-1.5">
              <Label htmlFor="taxonomy-code">Set code (optional)</Label>
              <Input
                id="taxonomy-code"
                value={code}
                onChange={(e) => setCode(e.target.value.toUpperCase())}
                placeholder="GK"
                maxLength={12}
              />
            </div>
          ) : null}
          <Button type="submit" disabled={createMutation.isPending}>
            <Plus />
            Add {tab === "set" ? "set" : "series"}
          </Button>
        </form>
      </DialogContent>
    </Dialog>
  );
}

export function TaxonomyManagerButton({ onClick }: { onClick: () => void }) {
  return (
    <Button type="button" variant="outline" size="sm" onClick={onClick}>
      <Tags />
      Sets &amp; series
    </Button>
  );
}
