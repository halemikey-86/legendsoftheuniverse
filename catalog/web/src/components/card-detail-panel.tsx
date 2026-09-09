import type { ReactNode } from "react";
import { Pencil, Trash2 } from "lucide-react";
import {
  TYPE_LABEL,
  hasCombat,
  rulesLines,
  type Card,
} from "@/lib/cards";
import { cn } from "@/lib/utils";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { frameBadgeVariant, typeBadgeVariant } from "@/components/game-card";

function DetailField({
  label,
  value,
  className,
}: {
  label: string;
  value: ReactNode;
  className?: string;
}) {
  if (value == null || value === "" || value === false) return null;
  return (
    <div className={cn("grid gap-1", className)}>
      <dt className="text-xs font-medium tracking-wide text-muted-foreground uppercase">
        {label}
      </dt>
      <dd className="text-sm leading-relaxed text-foreground">{value}</dd>
    </div>
  );
}

function DetailSection({
  title,
  children,
  className,
}: {
  title: string;
  children: ReactNode;
  className?: string;
}) {
  return (
    <section className={cn("grid gap-3", className)}>
      <h3 className="border-b border-border pb-1 font-display text-sm tracking-wide text-muted-foreground uppercase">
        {title}
      </h3>
      <dl className="grid gap-3 sm:grid-cols-2">{children}</dl>
    </section>
  );
}

export function CardDetailPanel({
  card,
  onEdit,
  onRemove,
  onCopyJson,
}: {
  card: Card;
  onEdit: () => void;
  onRemove: () => void;
  onCopyJson: () => void;
}) {
  const rules = rulesLines(card);
  const typeLine = card.subtype
    ? `${TYPE_LABEL[card.type]} — ${card.subtype}`
    : TYPE_LABEL[card.type];

  return (
    <div className="grid max-h-[min(90dvh,880px)] gap-0 lg:grid-cols-[minmax(0,22rem)_1fr]">
      <div className="flex items-start justify-center border-b border-border bg-background/40 p-4 sm:p-6 lg:sticky lg:top-0 lg:max-h-[min(90dvh,880px)] lg:border-r lg:border-b-0">
        {card.frontImageUrl ? (
          <img
            src={card.frontImageUrl}
            alt={card.name || "Card art"}
            className="max-h-[min(78dvh,720px)] w-full max-w-[20rem] rounded-lg border border-border object-contain shadow-[var(--shadow-border)]"
          />
        ) : (
          <div className="flex aspect-[5/7] w-full max-w-[20rem] items-center justify-center rounded-lg border border-dashed border-border bg-muted/20 px-6 text-center text-sm text-muted-foreground">
            No art uploaded
          </div>
        )}
      </div>

      <div className="flex min-h-0 min-w-0 flex-col gap-5 overflow-y-auto p-4 sm:p-6">
        <div className="space-y-3 pr-8">
          <div className="flex flex-wrap items-center gap-2">
            <Badge variant={typeBadgeVariant(card.type)}>{TYPE_LABEL[card.type]}</Badge>
            <Badge variant={frameBadgeVariant(card.frame)}>{card.frame}</Badge>
            <span className="font-mono text-xs text-muted-foreground">{card.id}</span>
          </div>
          <h2 className="font-display text-2xl leading-tight text-foreground">{card.name}</h2>
          <p className="text-sm text-muted-foreground">{typeLine}</p>
        </div>

        <DetailSection title="Identity">
          <DetailField label="Set" value={card.set} />
          <DetailField label="Number" value={card.number} />
          <DetailField label="Series" value={card.series} />
          <DetailField label="Role" value={card.role} />
        </DetailSection>

        <DetailSection title="Costs">
          <DetailField label="Will" value={card.willCost} />
          <DetailField label="Store worth" value={card.storeWorth} />
          <DetailField label="Honor cost" value={card.honorCost || null} />
          <DetailField label="Honor gain" value={card.honorGain || null} />
        </DetailSection>

        {hasCombat(card.type) ? (
          <DetailSection title="Combat">
            <DetailField label="Strike" value={card.strike} />
            <DetailField label="Guard" value={card.guard} />
            <DetailField label="Health" value={card.health} />
          </DetailSection>
        ) : null}

        {card.keywords.length > 0 ? (
          <DetailSection title="Keywords">
            <DetailField
              className="sm:col-span-2"
              label="Keywords"
              value={card.keywords.join(" · ")}
            />
          </DetailSection>
        ) : null}

        {rules.length > 0 ? (
          <section className="grid gap-3">
            <h3 className="border-b border-border pb-1 font-display text-sm tracking-wide text-muted-foreground uppercase">
              Rules text
            </h3>
            <ul className="grid gap-2">
              {rules.map((line) => (
                <li
                  key={line}
                  className="rounded-md border border-border bg-secondary/40 px-3 py-2 text-sm leading-relaxed text-foreground"
                >
                  {line}
                </li>
              ))}
            </ul>
          </section>
        ) : null}

        {card.flavor ? (
          <DetailField
            label="Flavor"
            value={<span className="italic text-muted-foreground">{card.flavor}</span>}
          />
        ) : null}

        {card.notes ? <DetailField label="Notes" value={card.notes} /> : null}

        {(card.startsInPlay || card.hunted > 0 || card.mend > 0 || card.doubleteam > 0) && (
          <DetailSection title="Flags">
            <DetailField label="Starts in play" value={card.startsInPlay ? "Yes" : null} />
            <DetailField label="Hunted" value={card.hunted > 0 ? card.hunted : null} />
            <DetailField label="Mend" value={card.mend > 0 ? card.mend : null} />
            <DetailField label="Doubleteam" value={card.doubleteam > 0 ? card.doubleteam : null} />
          </DetailSection>
        )}

        {card.artPrompt ? (
          <DetailField label="Art prompt" value={card.artPrompt} />
        ) : null}

        <div className="mt-auto flex flex-wrap gap-2 border-t border-border pt-4">
          <Button variant="outline" onClick={onCopyJson}>
            Copy JSON
          </Button>
          <Button variant="outline" onClick={onEdit}>
            <Pencil />
            Edit
          </Button>
          <Button variant="destructive" onClick={onRemove}>
            <Trash2 />
            Remove
          </Button>
        </div>
      </div>
    </div>
  );
}
