# ENGINE_INVENTORY v1

```
runtime: C# / .NET (netstandard2.1 kernel, net8.0 test host)
entry: MatchRunner.Apply(PlayerAction) -> ApplyResult { Success, Error, Events[] }  (Willbound.Engine/Rules/MatchRunner.cs)
schema_loaded: 1.2 and 1.3 (version-agnostic superset parsing, CardPrintingLoader.cs) — yes
parses_english: no (abilities[].text is stored, never read for logic — grep confirmed)
effects_runner: EffectInterpreter.cs — one interpreter, one place. Every ability resolution
                (Surge/Algorithm Now/Then, Activate, an entering permanent's OnEnter) and every
                hook (OnEnter, OnRemoved, OnBanished, OnClashBegin/End, OnPressDeclared/
                DealtDamage/RemovedBody, OnHold, OnAnswer, OnStoreAction) funnels through
                EffectInterpreter.Execute. No card is special-cased by printing id anywhere in
                the kernel (grep for `Printing.Id ==` returns nothing).
keywords_implemented:
  Aggression:    yes  (C1 wave assign, C4/C6 — C4's skip condition was buggy, see Phase 3 fixes)
  Bazerk:        yes  (two Press objects at declare, one exhaust)
  Gashing:       yes  (any damage Removes the Companion)
  HeavyHitter:   yes  (C7 leftover-to-Icon math)
  Hunted:        partial (StateChecks eliminates at >=10; nothing yet *applies* Hunted N via a
                 card ability — PutCounter(counter:"hunted") works generically if a card uses it,
                 but no fixture/test exercises it)
  Aftereffect:   partial (C7 phase exists for HeavyHitter/Drain math; no generic "runs in C7"
                 hook distinct from OnPressDealtDamage/OnPressRemovedBody, which do run generically)
  Still:         partial (emits HoldDeclared only; doesn't register as "also Holding" for any rule)
  Drain:         yes  (Mends attacker's Icon by damage dealt)
  Doubleteam:    yes  (Strike-side AND Guard-side both work — see Phase 3 fixes; helper Guard
                 boosts the pressing body if it's later Pressed itself this Clash, 4.8.48)
  Closed:        yes  (DeclarePress/AnswerPress reject an opponent choosing a Closed target)
  Toll:          yes  (DeclarePress/AnswerPress charge N extra Will per law 4.6.36, N via the
                 "toll" counter, default 1)
  Sealed:        yes  (StateChecks skips Remove-by-damage; MoveZone's Removed branch also honors it)
  Absolute:      yes  (DamageMath: max(Base, Ceil(Strike/2)))
  Silence:       partial (Silence player-action removes any stack object; not gated on the caster
                 having Silence/Now — unchanged from Phase 2)
  Stand Again:   yes  (StateChecks.TryStandAgain, once-only flag)
  Sweep:         yes  (new EffectOp.Sweep — removes up to N permanents matching a Filter,
                 default "OpponentField", respecting Sealed)
player_actions:
  Pass:                              yes
  PlayCard Now / Then / permanent:   yes (timing gates enforced in ValidatePlayCard)
  Activate:                          yes (pays costWill, respects OncePerTurn/Clash/Game, puts
                                      the ability on the Stack, effects[] run on resolve)
  DeclarePress / DeclareHold / Answer: yes
  StoreBuy Trade Sell Keep List Row: yes
ops_implemented (EffectOp -> has a real runtime case in EffectInterpreter.Execute):
  DealDamage, Mend, Draw, GainWill, WillPay, GainWorth, WorthPay, GainHonor, PutCounter,
  RemoveCounter, CreateToken, MoveZone (Remove/Banish via Params["zone"]), AttachBond, DetachBond,
  Exhaust, Ready, GrantKeyword, GrantKeywordOnDeclare, GiveFlag, Silence (by stack id), Sweep,
  LookStore, GuardAdd, StrikeAdd.
  Declared but not yet wired (a card using these loads fine — the op name is recognized — but the
  effect no-ops at runtime): PreventDamage, ReplaceRemove, SearchSupply, StoreBuy, StoreSell,
  StoreTrade, StoreList, StoreRow, ChooseTarget, ForEach, If, Unless. These are exactly the
  control-flow / already-covered-elsewhere ops that would need a real target-choice and branching
  model to do honestly; building that wasn't asked for this phase and no test needs it.
events_emitted: MatchStarted, RoundChanged, TurnStarted, PhaseChanged, PriorityChanged, WillSet,
                WillPaid, WorthChanged, HonorChanged, CardDrew, CardMoved, PermanentEntered,
                PermanentLeft, BondAttached, Exhausted, Readied, StackPushed, StackPopped,
                Silenced, PressDeclared, PressAnswered, HoldDeclared, ClashLocked, DamageDealt,
                CounterPut, HealthChanged, KeywordGranted, StoreRefilled, PlayerLost, PlayerWon,
                CheckRan (+ non-spec: LegendaryIconOffered/Picked, HandKept/Cycled/Mulliganed,
                TurnOrderRandomized). Every event in the section-6 catalog is now emitted
                somewhere (Phase 2's five gaps — MatchStarted, WillPaid, HonorChanged,
                BondAttached, KeywordGranted — are all closed).
tests_passing: 56 / 56 (dotnet test Willbound.Engine.Tests/Willbound.Engine.Tests.csproj)
tests_failing: 0
sample_illegal: MatchRunner.cs
  if (player.StoreActionsThisTurn >= 1)
      return "One Store action per turn."; // test 26
  (returned before any StackObject is created / Match mutated; ValidateAndApply -> ApplyResult.Fail)
sample_card: Willbound.Engine.Tests/AcceptanceTests.cs, Test39_IconFixtureGrantsAggressionAnd...
  — an Icon printing loaded from JSON with id "TEST-PHANTOM-ICON", same ability shape as Jar Jar
  (SITH-001), that grants itself Aggression on its first declared Press and gains Anger when that
  Press deals damage — proving the mechanism is generic, not id-gated.
diff_vs_engine_b: see "Known gaps" below.
```

## Repo-integrity fix (Phase 2 — found before any rules work was possible)

`Willbound.Engine.csproj` and `Willbound.Engine.Tests.csproj` were both silently excluded by the
stock Unity `.gitignore` line `*.csproj`. `Willbound.Engine.Tests.csproj` did not exist anywhere
(not on disk, not recoverable from git history) even though its `bin`/`obj` build output proved it
had built and run before. Reconstructed it from the `obj/project.assets.json` lockfile and added
negation lines to `.gitignore` so both project files stay tracked going forward. Without this fix,
a fresh clone of this repo could not build or run the kernel at all.

## Active Bar — all 8 green (Phase 2)

1. ✅ Schema 1.3 load. Text-without-effects[] rejected (`CardLoadException` on load).
2. ✅ 2 seats. Will = min(Round, 8) at Start. Draw 1.
3. ✅ Play Companion. One Store action (including `List`, fixed in Phase 2). Pass.
4. ✅ Clash: Press or Hold. Answer. Silence a Press (refund Will/Worth, exhaust stays).
5. ✅ Damage = max(0, Strike − Guard) unless Absolute.
6. ✅ Icon Health 0 or Hunted 10 = that seat is out.
7. ✅ OnStoreAction + kind List|Buy actually fires.
8. ✅ Events out. No English parser.

## Engine B §8 acceptance tests — all 32 pass

All 32 named tests in the law's section 8 map 1:1 by name/order to `Test01`…`Test32` in
`AcceptanceTests.cs` and pass, plus 24 additional tests (Store edge cases, loader validation,
pregame flow, legendary draft, mulligan, seat randomization, and Phase 3's generic-effects tests)
for **56/56 total**.

## Phase 3 — the generic effects[] interpreter

**Done = fixture cards run by effects[] and the Jar Jar id branch is gone — confirmed:**
`grep -rn "Printing.Id ==" Willbound.Engine/` and `grep -rn "SITH-001" Willbound.Engine/*.cs
Willbound.Engine/**/*.cs` both return nothing outside this file.

### What was built

- **`EffectInterpreter.cs`** (new file) — the one interpreter. `RunAbility`/`RunImmediate` resolve
  an ability's effects in order; `FireOwnHook` fires a hook against one card's own abilities;
  `FireOnStoreAction` fires `OnStoreAction` against every permanent a player controls, filtered by
  a pipe-delimited `StoreActionKind` list (e.g. `"List|Buy"`); `EvaluateDeclareFilter` evaluates
  `GrantKeywordOnDeclare`'s filter at Press-declare time.
- **Jar Jar generalized**: `ApplyJarJarFirstPressGrant` (gated on `Printing.Id == "SITH-001"`) is
  gone, replaced by `ApplyGrantKeywordOnDeclareEffects`, which scans the declaring body's own
  `effects[]` for `GrantKeywordOnDeclare` regardless of id. The hardcoded Anger-on-damage block in
  `DealPressDamage` is gone, replaced by firing `OnPressDealtDamage`/`OnPressRemovedBody` generically
  — any card with Jar Jar's exact ability JSON now behaves identically to Jar Jar (Test39).
- **New `EffectOp` members**: `LookStore`, `Sweep`, `GuardAdd`, `StrikeAdd`, `WillPay`, `WorthPay`
  (the repo has no `WILLBOUND_Engine_Opcodes.md`, so per instruction these extend the existing
  enum rather than replacing it).
- **New `EffectWhen` members**: `OnAnswer`, `OnStoreAction` (both were referenced as "hooks that
  already exist" but weren't declared; added and wired).
- **Hooks wired**: `OnEnter` (permanent resolve), `OnRemoved`/`OnBanished` (state-check removal and
  the new `MoveZone` effect op), `OnClashBegin`/`OnClashEnd` (C0/C8, every Field permanent on every
  seat), `OnPressDeclared`/`OnPressDealtDamage`/`OnPressRemovedBody` (the pressing body's own
  abilities), `OnHold`, `OnAnswer`, `OnStoreAction`.
- **Activate**: `PlayerActionKind.Activate` now pays `costWill`, respects
  `OncePerTurn`/`OncePerClash`/`OncePerGame`, pushes a `StackObjectType.Activate` object, and its
  effects[] run on resolve via the same interpreter (Test41).
- **Bond attach**: `ResolvePlayCard` now attaches a resolving Bond to its chosen host (the
  `PlayCard` action's `TargetInstanceId`, defaulting to the controller's Icon), sets
  `HostInstanceId`, and emits `BondAttached`. Host removal already cascaded to the Bond generically
  via `StateChecks` — that half wasn't broken, just never fed a host id (Test40).
- **Closed / Toll enforced**: per Engine B's actual definitions (4.6.36/37, not the "if missing"
  fallback ones), `DeclarePress`/`AnswerPress` now reject targeting a `Closed` permanent you don't
  control, and charge `Toll N` extra Will (or reject) when targeting one.
- **Surges/Algorithms now run their effects[]** on resolve before moving to Removed (previously
  they just moved to Removed with no effect execution at all).

### Two pre-existing kernel bugs found and fixed while writing the Doubleteam test

Writing `Test45_DoubleteamAddsHelperStrikeAndGuard` (the first test to use a *non-lethal* press
with an *exact* damage assertion — the original 32 tests either unit-test `DamageMath` directly,
use one-shot-lethal damage that self-masks via the C5 skip check, or use loose `>`/`<` assertions)
surfaced two bugs that predate this session:

1. **`ResolveAggressionWave`'s skip condition was wrong.** It skipped only when
   `!Aggression && Wave != 0`, which means an ordinary non-Aggression Wave-0 press (the vast
   majority of presses in the game) was *never* skipped in C4 — it fired there, then fired *again*
   in C6 (`ResolveNormalWave`'s skip condition was already correct). Fixed to the law's actual rule
   4.8.60: skip unless `Keywords.Contains(Aggression)` (Bazerk wave 0 is always tagged Aggression
   at declare, so this alone covers "every Bazerk wave 0" too, per rule text).
2. **Doubleteam's bonus was applied one line too late.** `ApplyDoubleteam` ran *after*
   `CreatePressStack`, which snapshots Strike into `StackObject.StrikeSnapshot` at declare time —
   so the bonus it set was never read. Reordered: Doubleteam now applies before the press object
   (or objects, for Bazerk) is created.

Both are one-line/reordering fixes, verified against the full 56-test suite (no regressions) —
not a pipeline rewrite.

### Known gaps (still open, and why)

- **Control-flow ops are declared but inert**: `ChooseTarget`, `ForEach`, `If`, `Unless`,
  `PreventDamage`, `ReplaceRemove`, `SearchSupply`, and the Store* effect ops (the player-action
  Store flow already works independently). Building real conditional branching / multi-target
  choice needs a target-selection protocol this kernel doesn't have yet; no test requires it.
- **`GrantKeyword`/`GrantKeywordOnDeclare` duration isn't cleaned up.** Engine B rule 26 ("strip
  this-turn/this-Clash flags") was never implemented generically (a pre-existing gap, not
  introduced this phase) — a keyword granted with `"duration": "ThisClash"` currently never expires.
  Only Jar Jar-shaped abilities (grant + separate damage-trigger) are exercised by tests today.
- **`Aftereffect` and `Still`** remain as described in the Phase 2 inventory — `OnPressDealtDamage`/
  `OnPressRemovedBody` now cover the generic-hook need Aftereffect implied, but there's no keyword
  gate named `Aftereffect` itself, and `Still` still doesn't register as "also Holding" anywhere.
- **`Silence` (the player action)** still isn't gated on the caster holding Silence/Now.
- **Honor** now has a working `GainHonor` op and `HonorChanged` event, but no card in the bundled
  fixtures reads Honor back for anything — it's mutable now, not yet consumed by any rule.
- **Store refill after Buy/Trade/Sell** still doesn't happen automatically (only `Row` refills) —
  unchanged from Phase 2, not touched this phase.
- **Opening hand size (7, not 5) and the Legendary Icon draft/Mulligan pregame flow** — unchanged
  from Phase 2, still a documented deviation from the law, not in scope here.
