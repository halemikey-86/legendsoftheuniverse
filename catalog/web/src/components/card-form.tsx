import { useMemo, useState, type FormEvent } from "react";
import {
  CARD_TYPES,
  FRAMES,
  KEYWORDS,
  ROLES,
  SERIES,
  SETS,
  TIMINGS,
  TYPE_LABEL,
  blankAbility,
  blankSpell,
  suggestId,
  toCardInput,
  type Ability,
  type Card,
  type CardInput,
  type CardType,
  type Frame,
  type Keyword,
  type Role,
  type Spell,
  type Timing,
} from "@/lib/cards";
import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { GameCard } from "@/components/game-card";
import { CardImageUpload } from "@/components/card-image-upload";

const EDITOR_TABS = ["Face", "Powers", "Engine"] as const;
type EditorTab = (typeof EDITOR_TABS)[number];

function NumberField({
  id,
  label,
  value,
  onChange,
  min = 0,
  max = 20,
}: {
  id: string;
  label: string;
  value: number;
  onChange: (n: number) => void;
  min?: number;
  max?: number;
}) {
  return (
    <div className="flex flex-col gap-1.5">
      <Label htmlFor={id}>{label}</Label>
      <Input
        id={id}
        inputMode="numeric"
        value={String(value)}
        onChange={(e) => {
          const raw = e.target.value;
          if (raw === "") {
            onChange(0);
            return;
          }
          const n = Number(raw);
          if (Number.isInteger(n) && n >= min && n <= max) onChange(n);
        }}
      />
    </div>
  );
}

function SuggestionInput({
  id,
  label,
  value,
  onChange,
  suggestions,
  placeholder,
  maxLength = 60,
  required,
}: {
  id: string;
  label: string;
  value: string;
  onChange: (value: string) => void;
  suggestions: string[];
  placeholder?: string;
  maxLength?: number;
  required?: boolean;
}) {
  const listId = `${id}-suggestions`;
  return (
    <div className="flex flex-col gap-1.5">
      <Label htmlFor={id}>{label}</Label>
      <Input
        id={id}
        list={listId}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={placeholder}
        maxLength={maxLength}
        required={required}
      />
      <datalist id={listId}>
        {suggestions.map((item) => (
          <option key={item} value={item} />
        ))}
      </datalist>
    </div>
  );
}

function ToggleChip({
  active,
  onClick,
  children,
}: {
  active: boolean;
  onClick: () => void;
  children: string;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={cn(
        "h-10 shrink-0 rounded-full px-3 text-sm transition-colors duration-150",
        active
          ? "bg-primary text-primary-foreground"
          : "bg-secondary text-muted-foreground hover:text-foreground",
      )}
    >
      {children}
    </button>
  );
}

export function CardForm({
  value,
  onChange,
  submitting,
  submitLabel,
  onSubmit,
  onCancel,
  setSuggestions = [...SETS],
  seriesSuggestions = [...SERIES],
}: {
  value: Card;
  onChange: (next: Card) => void;
  submitting?: boolean;
  submitLabel: string;
  onSubmit: (input: CardInput) => void;
  onCancel: () => void;
  setSuggestions?: string[];
  seriesSuggestions?: string[];
}) {
  const [tab, setTab] = useState<EditorTab>("Face");
  const [error, setError] = useState<string | null>(null);
  const [importText, setImportText] = useState("");

  function set<K extends keyof Card>(key: K, next: Card[K]) {
    onChange({ ...value, [key]: next });
  }

  function setType(type: CardType) {
    onChange({
      ...value,
      type,
      role: type === "Companion" ? value.role : null,
      startsInPlay: type === "Icon" ? true : value.type === "Icon" ? false : value.startsInPlay,
    });
  }

  const suggested = useMemo(
    () => suggestId(value.set, value.number),
    [value.set, value.number],
  );

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    if (!value.name.trim()) {
      setError("Give the card a name.");
      setTab("Face");
      return;
    }
    if (!value.set.trim()) {
      setError("Set is required.");
      setTab("Face");
      return;
    }
    setError(null);
    onSubmit(toCardInput({ ...value, id: value.id.trim() || suggested }));
  }

  function importJson() {
    try {
      const parsed = JSON.parse(importText) as Card;
      if (!parsed || typeof parsed !== "object") throw new Error("Not an object");
      onChange({
        ...value,
        ...parsed,
        keywords: Array.isArray(parsed.keywords) ? parsed.keywords : [],
        abilities: Array.isArray(parsed.abilities) ? parsed.abilities : [],
        spells: Array.isArray(parsed.spells) ? parsed.spells : [],
      });
      setImportText("");
      setError(null);
      setTab("Face");
    } catch {
      setError("Could not read that JSON.");
    }
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-4">
      <div className="grid gap-5 md:grid-cols-[minmax(0,13rem)_1fr] md:items-start">
        <GameCard card={value} className="mx-auto w-44 md:w-full" />

        <div className="flex min-w-0 flex-col gap-4">
          <div className="flex gap-1 rounded-lg bg-secondary p-1">
            {EDITOR_TABS.map((item) => (
              <button
                key={item}
                type="button"
                onClick={() => setTab(item)}
                className={cn(
                  "h-10 flex-1 rounded-md text-sm font-medium transition-colors duration-150",
                  tab === item
                    ? "bg-card text-foreground shadow-[var(--shadow-border)]"
                    : "text-muted-foreground hover:text-foreground",
                )}
              >
                {item}
              </button>
            ))}
          </div>

          {tab === "Face" ? (
            <div className="grid gap-3 sm:grid-cols-2">
              <div className="flex flex-col gap-1.5 sm:col-span-2">
                <Label htmlFor="card-name">Name</Label>
                <Input
                  id="card-name"
                  value={value.name}
                  onChange={(e) => set("name", e.target.value)}
                  placeholder={'James "The Endless"'}
                  maxLength={80}
                  autoFocus
                />
              </div>

              <div className="flex flex-col gap-1.5">
                <Label>Type</Label>
                <Select value={value.type} onValueChange={(v) => setType(v as CardType)}>
                  <SelectTrigger aria-label="Type">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {CARD_TYPES.map((type) => (
                      <SelectItem key={type} value={type}>
                        {TYPE_LABEL[type]}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              <div className="flex flex-col gap-1.5">
                <Label htmlFor="card-subtype">Subtype</Label>
                <Input
                  id="card-subtype"
                  value={value.subtype}
                  onChange={(e) => set("subtype", e.target.value)}
                  placeholder="Endless, Wall, Now…"
                  maxLength={60}
                />
              </div>

              {value.type === "Companion" ? (
                <div className="flex flex-col gap-1.5">
                  <Label>Role</Label>
                  <Select
                    value={value.role ?? "__none__"}
                    onValueChange={(v) => set("role", v === "__none__" ? null : (v as Role))}
                  >
                    <SelectTrigger aria-label="Role">
                      <SelectValue placeholder="None" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="__none__">None</SelectItem>
                      {ROLES.map((role) => (
                        <SelectItem key={role} value={role}>
                          {role}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              ) : null}

              <div className="flex flex-col gap-1.5">
                <Label>Frame</Label>
                <Select value={value.frame} onValueChange={(v) => set("frame", v as Frame)}>
                  <SelectTrigger aria-label="Frame">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {FRAMES.map((frame) => (
                      <SelectItem key={frame} value={frame}>
                        {frame}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              <SuggestionInput
                id="card-set"
                label="Set"
                value={value.set}
                onChange={(v) => set("set", v)}
                suggestions={setSuggestions}
                placeholder="James The Endless"
                required
              />

              <div className="flex flex-col gap-1.5">
                <Label htmlFor="card-number">Number</Label>
                <Input
                  id="card-number"
                  value={value.number}
                  onChange={(e) => set("number", e.target.value)}
                  placeholder="01/50"
                  maxLength={20}
                />
              </div>

              <SuggestionInput
                id="card-series"
                label="Series"
                value={value.series}
                onChange={(v) => set("series", v)}
                suggestions={seriesSuggestions}
                placeholder="10th Planet, Movies, Video Games…"
              />

              <div className="flex flex-col gap-1.5">
                <Label htmlFor="card-id">Engine id</Label>
                <Input
                  id="card-id"
                  value={value.id}
                  onChange={(e) => set("id", e.target.value.toLowerCase())}
                  placeholder={suggested}
                  maxLength={40}
                />
                <p className="text-xs text-muted-foreground">
                  Blank uses {suggested}
                </p>
              </div>

              <div className="grid gap-3 sm:col-span-2 sm:grid-cols-2">
                <CardImageUpload
                  card={value}
                  slot="front"
                  label="Front art"
                  onUpdated={onChange}
                />
                <CardImageUpload
                  card={value}
                  slot="back"
                  label="Back art (optional)"
                  onUpdated={onChange}
                />
              </div>

              <NumberField
                id="will-cost"
                label="Will cost"
                value={value.willCost}
                onChange={(n) => set("willCost", n)}
                max={8}
              />
              <NumberField
                id="store-worth"
                label="Store Worth"
                value={value.storeWorth}
                onChange={(n) => set("storeWorth", n)}
              />
              <NumberField
                id="honor-cost"
                label="Honor cost"
                value={value.honorCost}
                onChange={(n) => set("honorCost", n)}
              />
              <NumberField
                id="honor-gain"
                label="Honor gain"
                value={value.honorGain}
                onChange={(n) => set("honorGain", n)}
              />

              {value.type === "Icon" || value.type === "Companion" ? (
                <>
                  <NumberField
                    id="strike"
                    label="Strike"
                    value={value.strike}
                    onChange={(n) => set("strike", n)}
                  />
                  <NumberField
                    id="guard"
                    label="Guard"
                    value={value.guard}
                    onChange={(n) => set("guard", n)}
                  />
                  <NumberField
                    id="health"
                    label="Health"
                    value={value.health}
                    onChange={(n) => set("health", n)}
                    max={30}
                  />
                </>
              ) : null}

              <div className="flex items-center gap-2 sm:col-span-2">
                <ToggleChip
                  active={value.startsInPlay}
                  onClick={() => set("startsInPlay", !value.startsInPlay)}
                >
                  Starts in play
                </ToggleChip>
                <p className="text-xs text-muted-foreground">Icons always start in the Field.</p>
              </div>
            </div>
          ) : null}

          {tab === "Powers" ? (
            <div className="flex flex-col gap-4">
              <div>
                <Label>Keywords</Label>
                <div className="mt-2 flex flex-wrap gap-2">
                  {KEYWORDS.map((keyword) => {
                    const active = value.keywords.includes(keyword);
                    return (
                      <ToggleChip
                        key={keyword}
                        active={active}
                        onClick={() =>
                          set(
                            "keywords",
                            active
                              ? value.keywords.filter((k) => k !== keyword)
                              : [...value.keywords, keyword as Keyword],
                          )
                        }
                      >
                        {keyword}
                      </ToggleChip>
                    );
                  })}
                </div>
              </div>

              <div className="grid gap-3 sm:grid-cols-3">
                <NumberField
                  id="hunted"
                  label="Hunted N"
                  value={value.hunted}
                  onChange={(n) => set("hunted", n)}
                />
                <NumberField
                  id="mend"
                  label="Mend"
                  value={value.mend}
                  onChange={(n) => set("mend", n)}
                />
                <NumberField
                  id="doubleteam"
                  label="Doubleteam"
                  value={value.doubleteam}
                  onChange={(n) => set("doubleteam", n)}
                  max={6}
                />
              </div>

              <div className="rounded-lg border border-border bg-secondary/40 p-3">
                <div className="flex items-center justify-between gap-2">
                  <Label>Aftereffect</Label>
                  <ToggleChip
                    active={value.aftereffect != null}
                    onClick={() =>
                      set(
                        "aftereffect",
                        value.aftereffect
                          ? null
                          : { text: "", oncePerTurn: false, costWill: 0 },
                      )
                    }
                  >
                    {value.aftereffect ? "On" : "Off"}
                  </ToggleChip>
                </div>
                {value.aftereffect ? (
                  <div className="mt-3 grid gap-3 sm:grid-cols-2">
                    <div className="flex flex-col gap-1.5 sm:col-span-2">
                      <Label htmlFor="ae-text">Text</Label>
                      <Textarea
                        id="ae-text"
                        rows={2}
                        value={value.aftereffect.text}
                        onChange={(e) =>
                          set("aftereffect", {
                            ...value.aftereffect!,
                            text: e.target.value,
                          })
                        }
                      />
                    </div>
                    <NumberField
                      id="ae-will"
                      label="Will cost"
                      value={value.aftereffect.costWill}
                      onChange={(n) =>
                        set("aftereffect", { ...value.aftereffect!, costWill: n })
                      }
                      max={8}
                    />
                    <div className="flex items-end">
                      <ToggleChip
                        active={value.aftereffect.oncePerTurn}
                        onClick={() =>
                          set("aftereffect", {
                            ...value.aftereffect!,
                            oncePerTurn: !value.aftereffect!.oncePerTurn,
                          })
                        }
                      >
                        Once per turn
                      </ToggleChip>
                    </div>
                  </div>
                ) : null}
              </div>

              <AbilityList
                abilities={value.abilities}
                onChange={(abilities) => set("abilities", abilities)}
              />
              <SpellList spells={value.spells} onChange={(spells) => set("spells", spells)} />
            </div>
          ) : null}

          {tab === "Engine" ? (
            <div className="flex flex-col gap-3">
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="flavor">Flavor</Label>
                <Textarea
                  id="flavor"
                  rows={2}
                  maxLength={300}
                  value={value.flavor}
                  onChange={(e) => set("flavor", e.target.value)}
                  placeholder="A line the table will remember."
                />
              </div>
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="art-prompt">Art prompt</Label>
                <Textarea
                  id="art-prompt"
                  rows={2}
                  maxLength={400}
                  value={value.artPrompt}
                  onChange={(e) => set("artPrompt", e.target.value)}
                  placeholder="Engine A image prompt. No likenesses."
                />
              </div>
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="notes">Designer notes</Label>
                <Textarea
                  id="notes"
                  rows={2}
                  maxLength={400}
                  value={value.notes}
                  onChange={(e) => set("notes", e.target.value)}
                  placeholder="Not printed."
                />
              </div>
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="import-json">Paste Engine B JSON</Label>
                <Textarea
                  id="import-json"
                  rows={4}
                  value={importText}
                  onChange={(e) => setImportText(e.target.value)}
                  placeholder='{ "name": "…", "type": "Companion" }'
                  className="font-mono text-xs"
                />
                <Button type="button" variant="outline" onClick={importJson}>
                  Load JSON into this card
                </Button>
              </div>
            </div>
          ) : null}
        </div>
      </div>

      {error ? (
        <p className="text-sm text-destructive" role="alert">
          {error}
        </p>
      ) : null}

      <div className="flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
        <Button type="button" variant="outline" onClick={onCancel}>
          Cancel
        </Button>
        <Button type="submit" disabled={submitting}>
          {submitting ? "Saving…" : submitLabel}
        </Button>
      </div>
    </form>
  );
}

function AbilityList({
  abilities,
  onChange,
}: {
  abilities: Ability[];
  onChange: (next: Ability[]) => void;
}) {
  return (
    <div className="flex flex-col gap-3">
      <div className="flex items-center justify-between">
        <Label>Abilities</Label>
        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={() => onChange([...abilities, blankAbility()])}
        >
          Add ability
        </Button>
      </div>
      {abilities.length === 0 ? (
        <p className="text-sm text-muted-foreground">No structured abilities yet.</p>
      ) : (
        abilities.map((ability, index) => (
          <div
            key={index}
            className="grid gap-3 rounded-lg border border-border bg-secondary/40 p-3 sm:grid-cols-2"
          >
            <div className="flex flex-col gap-1.5">
              <Label>Name</Label>
              <Input
                value={ability.name}
                onChange={(e) => {
                  const next = [...abilities];
                  next[index] = { ...ability, name: e.target.value };
                  onChange(next);
                }}
              />
            </div>
            <div className="flex flex-col gap-1.5">
              <Label>Timing</Label>
              <Select
                value={ability.timing}
                onValueChange={(v) => {
                  const next = [...abilities];
                  next[index] = { ...ability, timing: v as Timing };
                  onChange(next);
                }}
              >
                <SelectTrigger aria-label="Timing">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {TIMINGS.map((timing) => (
                    <SelectItem key={timing} value={timing}>
                      {timing}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <NumberField
              id={`ab-will-${index}`}
              label="Will"
              value={ability.costWill}
              onChange={(n) => {
                const next = [...abilities];
                next[index] = { ...ability, costWill: n };
                onChange(next);
              }}
              max={8}
            />
            <NumberField
              id={`ab-honor-${index}`}
              label="Honor"
              value={ability.costHonor}
              onChange={(n) => {
                const next = [...abilities];
                next[index] = { ...ability, costHonor: n };
                onChange(next);
              }}
            />
            <div className="flex flex-col gap-1.5 sm:col-span-2">
              <Label>Text</Label>
              <Textarea
                rows={2}
                value={ability.text}
                onChange={(e) => {
                  const next = [...abilities];
                  next[index] = { ...ability, text: e.target.value };
                  onChange(next);
                }}
              />
            </div>
            <div className="flex flex-wrap items-center gap-2 sm:col-span-2">
              <ToggleChip
                active={ability.oncePerTurn}
                onClick={() => {
                  const next = [...abilities];
                  next[index] = { ...ability, oncePerTurn: !ability.oncePerTurn };
                  onChange(next);
                }}
              >
                Once per turn
              </ToggleChip>
              <ToggleChip
                active={ability.oncePerGame}
                onClick={() => {
                  const next = [...abilities];
                  next[index] = { ...ability, oncePerGame: !ability.oncePerGame };
                  onChange(next);
                }}
              >
                Once per game
              </ToggleChip>
              <Input
                className="h-10 min-w-32 flex-1"
                placeholder="Target (self, companion…)"
                value={ability.target ?? ""}
                onChange={(e) => {
                  const next = [...abilities];
                  next[index] = { ...ability, target: e.target.value || null };
                  onChange(next);
                }}
              />
              <Button
                type="button"
                variant="ghost"
                size="sm"
                onClick={() => onChange(abilities.filter((_, i) => i !== index))}
              >
                Remove
              </Button>
            </div>
          </div>
        ))
      )}
    </div>
  );
}

function SpellList({
  spells,
  onChange,
}: {
  spells: Spell[];
  onChange: (next: Spell[]) => void;
}) {
  return (
    <div className="flex flex-col gap-3">
      <div className="flex items-center justify-between">
        <Label>Named spells</Label>
        <Button
          type="button"
          variant="outline"
          size="sm"
          onClick={() => onChange([...spells, blankSpell()])}
        >
          Add spell
        </Button>
      </div>
      {spells.map((spell, index) => (
        <div
          key={index}
          className="grid gap-3 rounded-lg border border-border bg-secondary/40 p-3 sm:grid-cols-[1fr_5rem_auto]"
        >
          <Input
            placeholder="Name"
            value={spell.name}
            onChange={(e) => {
              const next = [...spells];
              next[index] = { ...spell, name: e.target.value };
              onChange(next);
            }}
          />
          <Input
            inputMode="numeric"
            value={String(spell.willCost)}
            onChange={(e) => {
              const n = Number(e.target.value);
              if (!Number.isInteger(n)) return;
              const next = [...spells];
              next[index] = { ...spell, willCost: n };
              onChange(next);
            }}
            aria-label="Spell Will cost"
          />
          <Button
            type="button"
            variant="ghost"
            size="sm"
            onClick={() => onChange(spells.filter((_, i) => i !== index))}
          >
            Remove
          </Button>
          <Textarea
            className="sm:col-span-3"
            rows={2}
            placeholder="Text"
            value={spell.text}
            onChange={(e) => {
              const next = [...spells];
              next[index] = { ...spell, text: e.target.value };
              onChange(next);
            }}
          />
        </div>
      ))}
    </div>
  );
}
