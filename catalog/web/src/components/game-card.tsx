import { cn } from "@/lib/utils";
import {
  TYPE_LABEL,
  hasCombat,
  hashString,
  rulesLines,
  type Card,
  type CardType,
  type Frame,
} from "@/lib/cards";

const TYPE_TINT: Record<CardType, string> = {
  Icon: "text-gold",
  Companion: "text-foreground",
  Relic: "text-muted-foreground",
  Bond: "text-bond",
  Surge: "text-surge",
  Will: "text-will",
};

function CardArt({
  card,
  fit = "contain",
}: {
  card: Card;
  fit?: "contain" | "cover";
}) {
  if (card.frontImageUrl) {
    return (
      <img
        src={card.frontImageUrl}
        alt={card.name || "Card art"}
        className={cn(
          "size-full",
          fit === "contain" ? "object-contain object-center" : "object-cover",
        )}
      />
    );
  }

  const seed = hashString(card.id || card.name);
  const a = 18 + (seed % 28);
  const b = 36 + ((seed >> 5) % 28);
  const type = card.type;

  return (
    <svg viewBox="0 0 160 110" className="size-full" aria-hidden>
      {type === "Icon" && (
        <>
          <circle cx="80" cy="54" r="28" className="fill-gold" opacity="0.2" />
          <circle
            cx="80"
            cy="54"
            r="22"
            fill="none"
            className="stroke-gold"
            strokeWidth="2"
          />
          <path
            d="M80 34 L88 50 H104 L92 60 L96 78 L80 68 L64 78 L68 60 L56 50 H72 Z"
            className="fill-gold"
            opacity="0.9"
          />
        </>
      )}
      {type === "Companion" && (
        <>
          <circle cx="80" cy="38" r="12" className="fill-foreground" opacity="0.7" />
          <path
            d={`M${50 + (a % 8)} 96 Q80 ${44 + (b % 10)} ${110 - (a % 8)} 96`}
            className="fill-foreground"
            opacity="0.55"
          />
          <rect x="74" y="48" width="12" height="22" className="fill-foreground" opacity="0.5" />
        </>
      )}
      {type === "Relic" && (
        <>
          <rect
            x="48"
            y="28"
            width="64"
            height="56"
            rx="4"
            fill="none"
            className="stroke-foreground"
            strokeWidth="2"
            opacity="0.7"
          />
          <circle cx="80" cy="56" r="10" className="fill-foreground" opacity="0.5" />
          <path d="M80 38 V46 M80 66 V74 M62 56 H70 M90 56 H98" className="stroke-foreground" strokeWidth="2" />
        </>
      )}
      {type === "Bond" && (
        <>
          <circle cx="62" cy="56" r="18" fill="none" className="stroke-bond" strokeWidth="3" />
          <circle cx="98" cy="56" r="18" fill="none" className="stroke-bond" strokeWidth="3" />
        </>
      )}
      {type === "Surge" && (
        <>
          <polygon
            points={`80,${18 + (seed % 8)} 92,48 112,50 96,66 102,94 80,78 58,94 64,66 48,50 68,48`}
            className="fill-surge"
            opacity="0.85"
          />
        </>
      )}
      {type === "Will" && (
        <>
          <ellipse cx="80" cy="72" rx="36" ry="14" className="fill-will" opacity="0.3" />
          <ellipse cx="80" cy="66" rx="24" ry="10" className="fill-will" opacity="0.55" />
          <path
            d="M56 48 Q80 22 104 48"
            fill="none"
            className="stroke-will"
            strokeWidth="2"
          />
        </>
      )}
    </svg>
  );
}

export function GameCard({
  card,
  size = "grid",
  className,
}: {
  card: Card;
  size?: "grid" | "hero";
  className?: string;
}) {
  const lines = rulesLines(card);
  const typeLine = card.subtype
    ? `${TYPE_LABEL[card.type]} — ${card.subtype}`
    : TYPE_LABEL[card.type];
  const frameClass: Record<Frame, string> = {
    standard: "",
    gold: "frame-gold",
    universe: "frame-universe",
  };
  const artClass =
    card.frame === "universe"
      ? "art-universe"
      : card.frame === "gold"
        ? "art-gold"
        : "bg-background";

  if (size === "grid") {
    return (
      <article
        className={cn(
          "flex flex-col overflow-hidden rounded-lg border border-border bg-card text-left shadow-[var(--shadow-border)]",
          frameClass[card.frame],
          className,
        )}
      >
        <div
          className={cn(
            "relative aspect-[5/7] w-full overflow-hidden",
            artClass,
          )}
        >
          <CardArt card={card} fit="contain" />
          <div className="pointer-events-none absolute inset-x-0 top-0 flex items-start justify-between p-1.5">
            <span className="flex size-6 items-center justify-center rounded-full border border-border/80 bg-card/90 font-display text-xs font-medium tabular-nums backdrop-blur-sm">
              {card.willCost}
            </span>
            <span
              className={cn(
                "rounded-sm bg-card/90 px-1.5 py-0.5 text-[10px] font-medium tracking-widest uppercase backdrop-blur-sm",
                TYPE_TINT[card.type],
              )}
            >
              {TYPE_LABEL[card.type]}
            </span>
          </div>
        </div>

        <div className="space-y-1 border-t border-border px-2 py-2">
          <h3 className="line-clamp-2 font-display text-sm leading-tight text-foreground">
            {card.name || "Untitled"}
          </h3>
          <p className="line-clamp-1 text-[11px] tracking-wide text-muted-foreground uppercase">
            {typeLine}
            {card.role ? ` · ${card.role}` : ""}
          </p>
          <div className="flex items-center justify-between gap-2 pt-0.5">
            <span className="truncate text-[10px] tracking-wide text-muted-foreground uppercase">
              {card.number || card.id || "—"}
            </span>
            {hasCombat(card.type) ? (
              <span className="shrink-0 rounded-sm border border-border bg-secondary px-1.5 py-0.5 font-display text-[10px] tabular-nums">
                {card.strike}/{card.guard}/{card.health}
              </span>
            ) : card.storeWorth ? (
              <span className="shrink-0 text-[10px] tabular-nums text-muted-foreground">
                Worth {card.storeWorth}
              </span>
            ) : null}
          </div>
        </div>
      </article>
    );
  }

  return (
    <article
      className={cn(
        "game-card flex flex-col overflow-hidden rounded-lg border border-border bg-card p-1.5 text-left shadow-[var(--shadow-border)]",
        frameClass[card.frame],
        size === "hero" && "p-2 rounded-xl",
        className,
      )}
    >
      <div className="flex items-center justify-between px-1.5 pt-0.5 pb-1">
        <span className="flex size-6 items-center justify-center rounded-full border border-border bg-secondary font-display text-xs font-medium tabular-nums">
          {card.willCost}
        </span>
        <span
          className={cn(
            "text-xs font-medium tracking-widest uppercase",
            TYPE_TINT[card.type],
          )}
        >
          {TYPE_LABEL[card.type]}
        </span>
      </div>

      <div className={cn("relative min-h-0 flex-1 overflow-hidden rounded-md", artClass)}>
        <CardArt card={card} fit="contain" />
      </div>

      <div className="mt-1.5 px-1.5">
        <h3
          className={cn(
            "font-display leading-tight text-foreground",
            size === "hero" ? "text-xl" : "text-sm",
          )}
        >
          {card.name || "Untitled"}
        </h3>
        <p className="mt-0.5 text-xs tracking-wide text-muted-foreground uppercase">
          {typeLine}
          {card.role ? ` · ${card.role}` : ""}
        </p>
      </div>

      <div
        className={cn(
          "mt-1.5 flex min-h-0 flex-1 flex-col rounded-md bg-secondary/70 px-2 py-1.5",
          size === "hero" && "px-3 py-2.5",
        )}
      >
        {card.keywords.length > 0 ? (
          <p className="mb-1 text-xs tracking-wide text-gold uppercase">
            {card.keywords.join(" · ")}
          </p>
        ) : null}
        <p
          className={cn(
            "text-foreground/90",
            size === "hero" ? "text-sm leading-relaxed" : "line-clamp-3 text-xs leading-snug",
          )}
        >
          {lines[0] || "No printed abilities."}
        </p>
        {size === "hero" && lines.length > 1
          ? lines.slice(1).map((line) => (
              <p key={line} className="mt-1 text-xs leading-relaxed text-foreground/80">
                {line}
              </p>
            ))
          : null}
        {card.flavor ? (
          <p
            className={cn(
              "mt-1 italic text-muted-foreground",
              size === "hero" ? "text-xs leading-relaxed" : "line-clamp-1 text-xs",
            )}
          >
            {card.flavor}
          </p>
        ) : null}
      </div>

      <div className="mt-1 flex items-end justify-between px-1.5 pb-0.5">
        <span className="text-xs tracking-wide text-muted-foreground uppercase">
          {card.number || card.id || "—"}
        </span>
        {hasCombat(card.type) ? (
          <span className="rounded-sm border border-border bg-secondary px-1.5 py-0.5 font-display text-xs tabular-nums">
            {card.strike}/{card.guard}/{card.health}
          </span>
        ) : (
          <span className="text-xs tabular-nums text-muted-foreground">
            {card.storeWorth ? `Worth ${card.storeWorth}` : ""}
          </span>
        )}
      </div>
    </article>
  );
}

export function frameBadgeVariant(frame: Frame) {
  if (frame === "gold") return "gold" as const;
  if (frame === "universe") return "universe" as const;
  return "outline" as const;
}

export function typeBadgeVariant(type: CardType) {
  if (type === "Icon") return "gold" as const;
  if (type === "Will") return "will" as const;
  if (type === "Surge") return "surge" as const;
  if (type === "Bond") return "bond" as const;
  if (type === "Relic") return "universe" as const;
  return "default" as const;
}
