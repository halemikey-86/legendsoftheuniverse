import { useEffect, useMemo, useRef, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Download, Library, Pencil, Plus, Search, Trash2, X } from "lucide-react";
import { toast } from "sonner";
import {
  CARD_TYPES,
  COST_FILTERS,
  FRAMES,
  KEYWORDS,
  SERIES,
  SETS,
  SORTS,
  TYPE_LABEL,
  blankCard,
  createCard,
  deleteCard,
  emptySearch,
  fetchEngineExport,
  getLibraryStats,
  searchCards,
  toEngineJson,
  updateCard,
  type Card,
  type CardInput,
  type CardSearch,
  type CostFilter,
  type SortKey,
} from "@/lib/cards";
import { cn } from "@/lib/utils";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { CardForm } from "@/components/card-form";
import { GameCard, frameBadgeVariant, typeBadgeVariant } from "@/components/game-card";

const SORT_LABEL: Record<SortKey, string> = {
  name: "Name",
  will: "Will",
  worth: "Worth",
  newest: "Newest",
};

function useDebounced<T>(value: T, delay: number): T {
  const [debounced, setDebounced] = useState(value);
  useEffect(() => {
    const id = window.setTimeout(() => setDebounced(value), delay);
    return () => window.clearTimeout(id);
  }, [value, delay]);
  return debounced;
}

function Mark({ className }: { className?: string }) {
  return (
    <svg viewBox="0 0 32 32" className={cn("size-8", className)} aria-hidden>
      <rect
        x="4"
        y="4"
        width="24"
        height="24"
        rx="3"
        className="fill-card stroke-gold"
        strokeWidth="1.5"
      />
      <circle cx="16" cy="16" r="7" fill="none" className="stroke-gold" strokeWidth="1.5" />
      <circle cx="16" cy="16" r="2.5" className="fill-gold" />
    </svg>
  );
}

async function copyText(text: string) {
  await navigator.clipboard.writeText(text);
}

function downloadJson(filename: string, data: unknown) {
  const blob = new Blob([JSON.stringify(data, null, 2)], { type: "application/json" });
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = filename;
  a.click();
  URL.revokeObjectURL(url);
}

export function WillboundApp({ initialCards }: { initialCards: Card[] }) {
  const queryClient = useQueryClient();
  const searchRef = useRef<HTMLInputElement>(null);
  const [draft, setDraft] = useState<CardSearch>(emptySearch);
  const debouncedQ = useDebounced(draft.q, 200);
  const filters: CardSearch = { ...draft, q: debouncedQ };

  const [selected, setSelected] = useState<Card | null>(null);
  const [formMode, setFormMode] = useState<"create" | "edit" | null>(null);
  const [formValue, setFormValue] = useState<Card>(blankCard);
  const [pendingDelete, setPendingDelete] = useState<Card | null>(null);

  const isDefaultSearch =
    filters.q === "" &&
    !filters.type &&
    !filters.set &&
    !filters.series &&
    !filters.frame &&
    !filters.keyword &&
    !filters.role &&
    !filters.cost &&
    filters.sort === "name";

  const cardsQuery = useQuery({
    queryKey: ["cards", filters],
    queryFn: () => searchCards(filters),
    initialData: isDefaultSearch ? initialCards : undefined,
  });

  const statsQuery = useQuery({
    queryKey: ["cards-stats"],
    queryFn: () => getLibraryStats(),
  });

  const cards = cardsQuery.data ?? [];
  const total = statsQuery.data?.total ?? initialCards.length;

  useEffect(() => {
    function onKey(event: KeyboardEvent) {
      const target = event.target as HTMLElement | null;
      const typing =
        target &&
        (target.tagName === "INPUT" ||
          target.tagName === "TEXTAREA" ||
          target.isContentEditable);
      if (event.key === "/" && !typing) {
        event.preventDefault();
        searchRef.current?.focus();
      }
    }
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, []);

  const createMutation = useMutation({
    mutationFn: (input: CardInput) => createCard(input),
    onSuccess: async (card) => {
      toast.success(`Printed ${card.name}`);
      setFormMode(null);
      setSelected(card);
      await queryClient.invalidateQueries({ queryKey: ["cards"] });
      await queryClient.invalidateQueries({ queryKey: ["cards-stats"] });
    },
    onError: (err: Error) => toast.error(err.message || "Could not add card"),
  });

  const updateMutation = useMutation({
    mutationFn: (input: CardInput & { originalId: string }) => updateCard(input),
    onSuccess: async (card) => {
      toast.success(`Updated ${card.name}`);
      setFormMode(null);
      setSelected(card);
      await queryClient.invalidateQueries({ queryKey: ["cards"] });
    },
    onError: (err: Error) => toast.error(err.message || "Could not save card"),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteCard(id),
    onSuccess: async () => {
      toast.success("Card removed");
      setPendingDelete(null);
      setSelected(null);
      await queryClient.invalidateQueries({ queryKey: ["cards"] });
      await queryClient.invalidateQueries({ queryKey: ["cards-stats"] });
    },
    onError: (err: Error) => toast.error(err.message || "Could not remove card"),
  });

  const filtersActive = Boolean(
    draft.q ||
      draft.type ||
      draft.set ||
      draft.series ||
      draft.frame ||
      draft.keyword ||
      draft.role ||
      draft.cost,
  );

  const resultLabel = useMemo(() => {
    if (cardsQuery.isFetching && !cardsQuery.data) return "Searching…";
    if (filtersActive) {
      return `${cards.length} match${cards.length === 1 ? "" : "es"} of ${total}`;
    }
    return `${total} card${total === 1 ? "" : "s"} in the Field`;
  }, [cards.length, cardsQuery.data, cardsQuery.isFetching, filtersActive, total]);

  function setFilter<K extends keyof CardSearch>(key: K, value: CardSearch[K]) {
    setDraft((prev) => ({ ...prev, [key]: value }));
  }

  function openCreate() {
    setSelected(null);
    setFormValue(blankCard());
    setFormMode("create");
  }

  function openEdit(card: Card) {
    setFormValue(card);
    setFormMode("edit");
  }

  return (
    <div className="flex min-h-dvh flex-col">
      <header className="border-b border-border">
        <div className="mx-auto flex w-full max-w-6xl flex-col gap-5 px-4 py-5 sm:px-6 sm:py-6">
          <div className="flex items-start justify-between gap-3">
            <div className="flex items-center gap-3">
              <Mark />
              <div>
                <p className="text-xs font-medium tracking-widest text-muted-foreground uppercase">
                  Root catalog · Engine B
                </p>
                <h1 className="font-display text-3xl leading-none tracking-tight sm:text-4xl">
                  WILLBOUND
                </h1>
              </div>
            </div>
            <div className="flex shrink-0 items-center gap-2">
              <Button
                variant="outline"
                className="hidden sm:inline-flex"
                onClick={async () => {
                  try {
                    const payload = await fetchEngineExport();
                    downloadJson("willbound-cards.json", payload);
                    toast.success("Catalog JSON downloaded");
                  } catch (err) {
                    toast.error(err instanceof Error ? err.message : "Export failed");
                  }
                }}
              >
                <Download />
                Export
              </Button>
              <Button onClick={openCreate}>
                <Plus />
                <span className="hidden sm:inline">New card</span>
                <span className="sm:hidden">New</span>
              </Button>
            </div>
          </div>

          <div className="relative">
            <Search className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              ref={searchRef}
              value={draft.q}
              onChange={(e) => setFilter("q", e.target.value)}
              placeholder="Search names, ids, abilities, sets…"
              className="h-12 rounded-lg border-border bg-card pl-10 pr-20"
              aria-label="Search cards"
            />
            <kbd className="pointer-events-none absolute top-1/2 right-3 hidden -translate-y-1/2 rounded-md border border-border bg-secondary px-1.5 py-0.5 text-xs text-muted-foreground sm:inline">
              /
            </kbd>
          </div>

          <div className="flex flex-col gap-3">
            <div className="-mx-4 flex gap-2 overflow-x-auto px-4 pb-1 sm:mx-0 sm:flex-wrap sm:overflow-visible sm:px-0">
              <FilterSelect
                label="Type"
                value={draft.type}
                allLabel="All types"
                options={CARD_TYPES.map((type) => ({ value: type, label: TYPE_LABEL[type] }))}
                onChange={(value) => setFilter("type", value)}
              />
              <FilterSelect
                label="Set"
                value={draft.set}
                allLabel="All sets"
                options={SETS.map((set) => ({ value: set, label: set }))}
                onChange={(value) => setFilter("set", value)}
              />
              <FilterSelect
                label="Series"
                value={draft.series}
                allLabel="All series"
                options={SERIES.map((series) => ({ value: series, label: series }))}
                onChange={(value) => setFilter("series", value)}
              />
              <FilterSelect
                label="Frame"
                value={draft.frame}
                allLabel="All frames"
                options={FRAMES.map((frame) => ({ value: frame, label: frame }))}
                onChange={(value) => setFilter("frame", value)}
              />
              <FilterSelect
                label="Keyword"
                value={draft.keyword}
                allLabel="All keywords"
                options={KEYWORDS.map((keyword) => ({ value: keyword, label: keyword }))}
                onChange={(value) => setFilter("keyword", value)}
              />
              <Select
                value={draft.sort}
                onValueChange={(value) => setFilter("sort", value as SortKey)}
              >
                <SelectTrigger className="h-10 w-auto min-w-32 shrink-0 bg-card" aria-label="Sort">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {SORTS.map((sort) => (
                    <SelectItem key={sort} value={sort}>
                      {SORT_LABEL[sort]}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              {filtersActive ? (
                <Button
                  variant="ghost"
                  size="sm"
                  className="h-10 shrink-0"
                  onClick={() => setDraft((prev) => ({ ...emptySearch(), sort: prev.sort }))}
                >
                  <X className="size-3.5" />
                  Clear
                </Button>
              ) : null}
            </div>

            <div className="flex items-center gap-1 overflow-x-auto pb-0.5">
              {COST_FILTERS.map((cost) => {
                const active = draft.cost === cost;
                const label = cost === "" ? "Any Will" : cost;
                return (
                  <button
                    key={cost || "any"}
                    type="button"
                    onClick={() => setFilter("cost", cost as CostFilter)}
                    className={cn(
                      "h-10 shrink-0 rounded-full px-3 text-sm transition-colors duration-150",
                      active
                        ? "bg-primary text-primary-foreground"
                        : "bg-secondary text-muted-foreground hover:text-foreground",
                    )}
                  >
                    {label}
                  </button>
                );
              })}
            </div>
          </div>
        </div>
      </header>

      <main className="mx-auto flex w-full max-w-6xl flex-1 flex-col px-4 py-6 sm:px-6">
        <div className="mb-4 flex items-center justify-between gap-3">
          <p className="text-sm text-muted-foreground tabular-nums">{resultLabel}</p>
          <Button
            variant="ghost"
            size="sm"
            className="sm:hidden"
            onClick={() => {
              downloadJson(
                "willbound-cards.json",
                cards.map((card) => toEngineJson(card)),
              );
              toast.success("Catalog JSON downloaded");
            }}
          >
            <Download className="size-3.5" />
            Export
          </Button>
        </div>

        {cardsQuery.isError ? (
          <div className="rounded-xl border border-border bg-card px-5 py-10 text-center">
            <p className="font-display text-lg">The catalog could not be read.</p>
            <p className="mt-1 text-sm text-muted-foreground">
              {(cardsQuery.error as Error).message}
            </p>
            <Button className="mt-4" variant="outline" onClick={() => void cardsQuery.refetch()}>
              Try again
            </Button>
          </div>
        ) : cardsQuery.isLoading ? (
          <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4">
            {Array.from({ length: 8 }).map((_, i) => (
              <Skeleton key={i} className="game-card rounded-xl" />
            ))}
          </div>
        ) : cards.length === 0 ? (
          <EmptyState
            filtered={filtersActive}
            onAdd={openCreate}
            onClear={() => setDraft(emptySearch())}
          />
        ) : (
          <ul className="stagger-in grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4">
            {cards.map((card) => (
              <li key={card.id}>
                <button
                  type="button"
                  aria-label={card.name}
                  onClick={() => setSelected(card)}
                  className="block w-full rounded-xl text-left transition-[transform,box-shadow] duration-150 ease-out hover:-translate-y-0.5 hover:shadow-[var(--shadow-border-hover)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring/70"
                >
                  <GameCard card={card} />
                </button>
              </li>
            ))}
          </ul>
        )}
      </main>

      <Dialog
        open={selected != null && formMode !== "edit"}
        onOpenChange={(open) => {
          if (!open) setSelected(null);
        }}
      >
        <DialogContent className="max-w-3xl p-4 pt-14 sm:p-6 sm:pt-10">
          {selected ? (
            <div className="grid gap-5 sm:grid-cols-[minmax(0,14rem)_1fr] sm:items-start">
              <GameCard card={selected} size="hero" className="mx-auto w-52 sm:w-full" />
              <div className="flex min-w-0 flex-col gap-4">
                <DialogHeader>
                  <DialogTitle>{selected.name}</DialogTitle>
                  <DialogDescription className="flex flex-wrap items-center gap-2">
                    <Badge variant={typeBadgeVariant(selected.type)}>
                      {TYPE_LABEL[selected.type]}
                    </Badge>
                    <Badge variant={frameBadgeVariant(selected.frame)}>{selected.frame}</Badge>
                    <span className="font-mono text-xs">{selected.id}</span>
                  </DialogDescription>
                </DialogHeader>
                <p className="text-sm text-muted-foreground">
                  {selected.set}
                  {selected.number ? ` · ${selected.number}` : ""}
                  {selected.series ? ` · ${selected.series}` : ""}
                  {selected.role ? ` · ${selected.role}` : ""}
                </p>
                <p className="text-sm tabular-nums text-foreground">
                  Will {selected.willCost} · Worth {selected.storeWorth}
                  {selected.honorCost ? ` · Honor ${selected.honorCost}` : ""}
                  {hasStats(selected)
                    ? ` · ${selected.strike}/${selected.guard}/${selected.health}`
                    : ""}
                </p>
                {selected.notes ? (
                  <p className="text-sm text-muted-foreground">{selected.notes}</p>
                ) : null}
                <pre className="max-h-40 overflow-auto rounded-md border border-border bg-background p-3 font-mono text-xs leading-relaxed text-muted-foreground">
                  {JSON.stringify(toEngineJson(selected), null, 2)}
                </pre>
                <div className="mt-auto flex flex-wrap gap-2 pt-2">
                  <Button
                    variant="outline"
                    onClick={() => {
                      void copyText(JSON.stringify(toEngineJson(selected), null, 2));
                      toast.success("Engine JSON copied");
                    }}
                  >
                    Copy JSON
                  </Button>
                  <Button variant="outline" onClick={() => openEdit(selected)}>
                    <Pencil />
                    Edit
                  </Button>
                  <Button variant="destructive" onClick={() => setPendingDelete(selected)}>
                    <Trash2 />
                    Remove
                  </Button>
                </div>
              </div>
            </div>
          ) : null}
        </DialogContent>
      </Dialog>

      <Dialog
        open={formMode != null}
        onOpenChange={(open) => {
          if (!open) setFormMode(null);
        }}
      >
        <DialogContent className="max-w-5xl">
          <DialogHeader>
            <DialogTitle>{formMode === "edit" ? "Edit card" : "New card"}</DialogTitle>
            <DialogDescription>
              Root print. Fields match Engine B JSON — Will, Worth, and Honor never mix.
            </DialogDescription>
          </DialogHeader>
          <CardForm
            value={formValue}
            onChange={setFormValue}
            submitting={createMutation.isPending || updateMutation.isPending}
            submitLabel={formMode === "edit" ? "Save to catalog" : "Print into catalog"}
            onCancel={() => setFormMode(null)}
            onSubmit={(input) => {
              if (formMode === "edit" && selected) {
                updateMutation.mutate({ ...input, originalId: selected.id });
              } else {
                createMutation.mutate(input);
              }
            }}
          />
        </DialogContent>
      </Dialog>

      <AlertDialog
        open={pendingDelete != null}
        onOpenChange={(open) => {
          if (!open) setPendingDelete(null);
        }}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Remove this card?</AlertDialogTitle>
            <AlertDialogDescription>
              {pendingDelete
                ? `${pendingDelete.name} (${pendingDelete.id}) leaves the catalog. This cannot be undone.`
                : null}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Keep it</AlertDialogCancel>
            <AlertDialogAction
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
              onClick={() => {
                if (pendingDelete) deleteMutation.mutate(pendingDelete.id);
              }}
            >
              Remove
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}

function hasStats(card: Card) {
  return card.type === "Icon" || card.type === "Companion";
}

function FilterSelect({
  label,
  value,
  allLabel,
  options,
  onChange,
}: {
  label: string;
  value: string;
  allLabel: string;
  options: { value: string; label: string }[];
  onChange: (value: string) => void;
}) {
  return (
    <Select
      value={value || "__all__"}
      onValueChange={(next) => onChange(next === "__all__" ? "" : next)}
    >
      <SelectTrigger className="h-10 w-auto min-w-32 shrink-0 bg-card" aria-label={label}>
        <SelectValue placeholder={allLabel} />
      </SelectTrigger>
      <SelectContent>
        <SelectItem value="__all__">{allLabel}</SelectItem>
        {options.map((option) => (
          <SelectItem key={option.value} value={option.value}>
            {option.label}
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  );
}

function EmptyState({
  filtered,
  onAdd,
  onClear,
}: {
  filtered: boolean;
  onAdd: () => void;
  onClear: () => void;
}) {
  return (
    <div className="flex flex-1 flex-col items-center justify-center rounded-xl border border-dashed border-border bg-card/40 px-6 py-16 text-center">
      <Library className="size-8 text-muted-foreground" />
      <h2 className="mt-4 font-display text-2xl">
        {filtered ? "Nothing matches" : "The Field is empty"}
      </h2>
      <p className="mt-2 max-w-sm text-sm text-muted-foreground">
        {filtered
          ? "Try a broader search, or clear the filters and look again."
          : "Print the first card. Engine B reads whatever Root writes here."}
      </p>
      <div className="mt-5 flex flex-wrap justify-center gap-2">
        {filtered ? (
          <Button variant="outline" onClick={onClear}>
            Clear filters
          </Button>
        ) : null}
        <Button onClick={onAdd}>
          <Plus />
          New card
        </Button>
      </div>
    </div>
  );
}
