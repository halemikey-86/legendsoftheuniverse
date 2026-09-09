import {
  CARD_TYPES,
  FRAMES,
  KEYWORDS,
  ROLES,
  TIMINGS,
  cardInputSchema,
  hasCombat,
  type Card,
  type CardInput,
} from "./card-schema.js";

export type ValidationSeverity = "error" | "warning";

export type ValidationIssue = {
  code: string;
  message: string;
  severity: ValidationSeverity;
};

export type ValidationContext = {
  existingCards?: Card[];
  /** When updating, the row id before edit */
  originalId?: string;
};

const KEYWORD_TEXT: Record<string, RegExp> = {
  Aggression: /\baggression\b/i,
  Bazerk: /\bbazerk\b|\bberserk\b/i,
  Gashing: /\bgashing\b/i,
  HeavyHitter: /\bheavy\s*hitter\b/i,
  Still: /\bstill\b/i,
  Drain: /\bdrain\b/i,
  Sealed: /\bsealed\b/i,
  Closed: /\bclosed\b/i,
  Silence: /\bsilence\b|\bsilenced\b/i,
};

function allCardText(card: CardInput): string {
  const chunks = [
    card.name,
    card.subtype,
    card.flavor,
    card.notes,
    ...card.abilities.map((a) => `${a.name} ${a.text}`),
    ...card.spells.map((s) => `${s.name} ${s.text}`),
    card.aftereffect?.text ?? "",
  ];
  return chunks.join("\n");
}

function cardUsesKeyword(card: CardInput, keyword: string): boolean {
  const pattern = KEYWORD_TEXT[keyword];
  if (!pattern) return true;
  return pattern.test(allCardText(card));
}

/** Rules Engine B guardrails — errors block save; warnings are shown in admin UI. */
export function validateCardRules(
  card: CardInput,
  context: ValidationContext = {},
): ValidationIssue[] {
  const issues: ValidationIssue[] = [];
  const existing = context.existingCards ?? [];
  const selfId = card.id.trim();
  const originalId = context.originalId?.trim();

  if (!selfId) {
    issues.push({
      code: "MISSING_ID",
      message: "Engine id is required",
      severity: "error",
    });
  }

  const dupId = existing.find((c) => c.id === selfId && c.id !== originalId);
  if (dupId) {
    issues.push({
      code: "DUPLICATE_ID",
      message: `Engine id "${selfId}" is already in the catalog`,
      severity: "error",
    });
  }

  if (card.number.trim()) {
    const dupNumber = existing.find(
      (c) =>
        c.id !== selfId &&
        c.id !== originalId &&
        c.set === card.set &&
        c.number === card.number,
    );
    if (dupNumber) {
      issues.push({
        code: "DUPLICATE_NUMBER",
        message: `Set "${card.set}" already has card number ${card.number} (${dupNumber.name})`,
        severity: "error",
      });
    }
  }

  const dupName = existing.find(
    (c) =>
      c.id !== selfId &&
      c.id !== originalId &&
      c.set === card.set &&
      c.name.toLowerCase() === card.name.trim().toLowerCase(),
  );
  if (dupName) {
    issues.push({
      code: "DUPLICATE_NAME",
      message: `Set "${card.set}" already has a card named "${card.name}"`,
      severity: "error",
    });
  }

  if (card.type === "Icon" && card.frame !== "gold") {
    issues.push({
      code: "ICON_FRAME",
      message: "Icons must use the gold frame",
      severity: "error",
    });
  }

  if (card.type !== "Companion" && card.role) {
    issues.push({
      code: "ROLE_TYPE",
      message: "Only Companions may have a role",
      severity: "error",
    });
  }

  if (card.type === "Companion" && !card.role) {
    issues.push({
      code: "ROLE_MISSING",
      message: "Companions should declare a role (Healer, Tank, Striker, Play Maker, Recursor)",
      severity: "warning",
    });
  }

  if (!hasCombat(card.type) && (card.strike > 0 || card.guard > 0 || card.health > 0)) {
    issues.push({
      code: "COMBAT_TYPE",
      message: "Strike, Guard, and Health are only for Icon and Companion",
      severity: "error",
    });
  }

  if (hasCombat(card.type) && card.health <= 0) {
    issues.push({
      code: "COMBAT_HEALTH",
      message: "Icon and Companion must have Health ≥ 1",
      severity: "error",
    });
  }

  if (card.type === "Icon") {
    const iconsInSet =
      existing.filter(
        (c) =>
          c.set === card.set &&
          c.type === "Icon" &&
          c.id !== originalId &&
          c.id !== selfId,
      ).length + (card.type === "Icon" ? 1 : 0);
    if (iconsInSet > 3) {
      issues.push({
        code: "ICON_CAP",
        message: "A 100-card pack allows at most 3 Icons per set",
        severity: "error",
      });
    }
  }

  if (card.frame === "universe" && card.type !== "Relic" && card.type !== "Bond") {
    issues.push({
      code: "UNIVERSE_FRAME",
      message: "Universe frame is normally used on Relic or Bond",
      severity: "warning",
    });
  }

  for (const kw of card.keywords) {
    if (!cardUsesKeyword(card, kw)) {
      issues.push({
        code: "KEYWORD_UNUSED",
        message: `Keyword "${kw}" is not referenced in card text — keywords only when the text uses one`,
        severity: "warning",
      });
    }
  }

  const abilityNames = card.abilities.map((a) => a.name.trim().toLowerCase()).filter(Boolean);
  if (new Set(abilityNames).size !== abilityNames.length) {
    issues.push({
      code: "DUPLICATE_ABILITY",
      message: "Duplicate ability names on the same card",
      severity: "error",
    });
  }

  for (const ability of card.abilities) {
    if (!ability.text.trim()) {
      issues.push({
        code: "ABILITY_TEXT",
        message: `Ability "${ability.name || "(unnamed)"}" needs rules text`,
        severity: "error",
      });
    }
    if (!(TIMINGS as readonly string[]).includes(ability.timing)) {
      issues.push({
        code: "ABILITY_TIMING",
        message: `Unknown timing "${ability.timing}" on ability "${ability.name}"`,
        severity: "error",
      });
    }
  }

  if (card.willCost > 8) {
    issues.push({
      code: "WILL_CAP",
      message: "Will cost cannot exceed 8 (round cap)",
      severity: "error",
    });
  }

  const totalCost = card.willCost + card.honorCost;
  if (totalCost === 0 && card.type === "Companion" && card.strike + card.guard + card.health > 18) {
    issues.push({
      code: "STAT_BUDGET",
      message: "Very high combat stats with zero cost — review for balance",
      severity: "warning",
    });
  }

  if (card.type === "Will" && card.abilities.some((a) => a.timing === "onPress")) {
    issues.push({
      code: "WILL_ON_PRESS",
      message: "Will sites should not use onPress timing",
      severity: "warning",
    });
  }

  return issues;
}

export function validationErrors(issues: ValidationIssue[]): ValidationIssue[] {
  return issues.filter((i) => i.severity === "error");
}

export function assertCardValid(
  card: CardInput,
  context: ValidationContext = {},
): ValidationIssue[] {
  const issues = validateCardRules(card, context);
  const errors = validationErrors(issues);
  if (errors.length > 0) {
    throw new Error(errors.map((e) => e.message).join("; "));
  }
  return issues;
}

export function parseCardInput(raw: unknown): CardInput {
  return cardInputSchema.parse(raw);
}

export const ALLOWED_TYPES = CARD_TYPES;
export const ALLOWED_FRAMES = FRAMES;
export const ALLOWED_ROLES = ROLES;
export const ALLOWED_KEYWORDS = KEYWORDS;
