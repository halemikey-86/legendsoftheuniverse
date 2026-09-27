# ENGINE_INVENTORY v1

```
runtime: C# / .NET (netstandard2.1 kernel, net8.0 test host)
entry: MatchRunner.Apply(PlayerAction) -> ApplyResult { Success, Error, Events[] }  (Willbound.Engine/Rules/MatchRunner.cs:37-56)
schema_loaded: 1.2 and 1.3 (version-agnostic superset parsing, CardPrintingLoader.cs) — yes
parses_english: no (abilities[].text is stored, never read for logic — grep confirmed)
effects_runner: none generic. SITH-001 (Jar Jar) is hand-coded by printing id in MatchRunner.cs.
                All other rules behavior (damage, clash, store, checks) is hand-written against
                keywords[] and hardcoded card ids, not a data-driven effects[] interpreter.
keywords_implemented:
  Aggression:    yes  (MatchRunner.cs C1 wave assign, C4/C5/C6)
  Bazerk:        yes  (two Press objects at declare, one exhaust)
  Gashing:       yes  (any damage Removes the Companion)
  HeavyHitter:   yes  (C7 leftover-to-Icon math)
  Hunted:        partial (StateChecks eliminates at >=10; nothing ever *applies* Hunted N via keyword/effect)
  Aftereffect:   partial (C7 phase exists but only for hardcoded HeavyHitter/Drain, not a generic hook)
  Still:         partial (emits HoldDeclared only; doesn't register as "also Holding" for any rule)
  Drain:         yes  (Mends attacker's Icon by damage dealt)
  Doubleteam:    partial (Strike-side helper bonus works; Guard-side helper-on-defense is dead code)
  Closed:        no   (enum exists, targeting never checks it)
  Toll:          no   (CardInstance.Toll exists, never charged/enforced)
  Sealed:        yes  (StateChecks skips Remove-by-damage)
  Absolute:      yes  (DamageMath: max(Base, Ceil(Strike/2)))
  Silence:       partial (Silence action removes any stack object; not gated on the caster having Silence/Now)
  Stand Again:   yes  (StateChecks.TryStandAgain, once-only flag)
  Sweep:         no   (not declared anywhere — missing from the Keyword enum entirely)
player_actions:
  Pass:                              yes
  PlayCard Now / Then / permanent:   yes (timing gates enforced in ValidatePlayCard)
  Activate:                          no  (PlayerActionKind.Activate declared, unhandled -> "Unsupported action.")
  DeclarePress / DeclareHold / Answer: yes
  StoreBuy Trade Sell Keep List Row: yes (List fixed this session — see below)
ops_implemented: none of the 29 section-5.3 EffectOps are executed generically. Behavior that
                 overlaps them (damage, counters, exhaust/ready, store actions) is hardcoded
                 procedurally, not dispatched from a card's effects[] JSON.
events_emitted: RoundChanged, TurnStarted, PhaseChanged, PriorityChanged, WillSet, WorthChanged,
                CardDrew, CardMoved, PermanentEntered, PermanentLeft, Exhausted, Readied,
                StackPushed, StackPopped, Silenced, PressDeclared, PressAnswered, HoldDeclared,
                ClashLocked, DamageDealt, CounterPut, HealthChanged, StoreRefilled, PlayerLost,
                PlayerWon, CheckRan (+ non-spec: LegendaryIconOffered/Picked, HandKept/Cycled/
                Mulliganed, TurnOrderRandomized).
                Missing from the section-6 catalog: MatchStarted, WillPaid, HonorChanged,
                BondAttached, KeywordGranted.
tests_passing: 49 / 49 (dotnet test Willbound.Engine.Tests/Willbound.Engine.Tests.csproj)
tests_failing: 0
missing_for_active: none — see Active Bar below, all 8 green as of this session.
sample_illegal: MatchRunner.cs:677-678
  if (player.StoreActionsThisTurn >= 1)
      return "One Store action per turn."; // test 26
  (returned before any StackObject is created / Match mutated; ValidateAndApply -> ApplyResult.Fail)
sample_card: Willbound.Engine.Tests/BootstrapCards.cs:143-154 (ClosedTank) — Companion 2/5/5,
  Keywords ["Closed","Toll"]. Note: neither keyword is currently enforced (see table above),
  so this fixture's namesake keywords are decorative today.
diff_vs_engine_b: see "Known gaps beyond the Active Bar" below.
```

## Repo-integrity fix (found before any rules work was possible)

`Willbound.Engine.csproj` and `Willbound.Engine.Tests.csproj` were both silently excluded by the
stock Unity `.gitignore` line `*.csproj` — untracked in git entirely. `Willbound.Engine.csproj`
happened to still exist as an untracked file on disk; `Willbound.Engine.Tests.csproj` did not exist
anywhere (not on disk, not recoverable from git history) even though its `bin`/`obj` build output
proved it had built and run before. Reconstructed it from the `obj/project.assets.json` lockfile
(exact package versions: `Microsoft.NET.Test.Sdk 17.11.1`, `NUnit 4.2.2`, `NUnit3TestAdapter 4.6.0`,
`net8.0`, `ProjectReference` to `Willbound.Engine`) and added negation lines to `.gitignore` so both
project files stay tracked going forward:

```
*.csproj
!/Willbound.Engine/Willbound.Engine.csproj
!/Willbound.Engine.Tests/Willbound.Engine.Tests.csproj
```

Without this fix, a fresh clone of this repo could not build or run the kernel at all.

## Active Bar — all 8 green

1. ✅ **Schema 1.3 load. Text-without-effects[] rejected.**
   Load already worked for 1.2/1.3. Added the missing rejection: `CardPrintingLoader` now throws
   `CardLoadException` when an ability has non-empty `text` and empty/missing `effects[]`, and
   when an effect's `op` is missing or unrecognized (previously both silently no-opped to
   `EffectOp.If` — exactly what the law says not to do). Tests: `Loader_RejectsAbilityWithTextAndNoEffects`,
   `Loader_RejectsUnknownEffectOp`. Verified this doesn't reject any existing fixture (`SITH-001`'s
   one ability has populated `effects[]`; `RemnantRelic` has no abilities at all).
2. ✅ **2 seats. Will = min(Round, 8) at Start. Draw 1.** Already worked (`MatchSetup.cs:271-307`).
   Tests 1-3.
3. ✅ **Play Companion. One Store action. Pass.** Already worked; `StoreList` was previously
   announceable but resolved as a silent no-op (no `case` in `ResolveStore`). Implemented rule
   4.12.97 (peek top 2 of Supply, one enters an open Store slot, the other goes to the bottom of
   Supply; if no Store slot is open, the first card stays on top for the next refill). Tests:
   `Test36_StoreListEntersOpenSlotAndBuriesSecondCard`, `Test37_StoreListLeavesFirstCardOnTopWhenStoreIsFull`.
4. ✅ **Clash: Press or Hold. Answer. Silence a Press (refund Will/Worth, exhaust stays).**
   Already worked. Tests 5, 8, 9, 10, 11.
5. ✅ **Damage = max(0, Strike − Guard) unless Absolute.** Already worked (`DamageMath.cs`). Test 15.
6. ✅ **Icon Health 0 or Hunted 10 = that seat is out.** Already worked (`StateChecks.cs`). Tests 19, 20.
7. ✅ **OnStoreAction + kind List|Buy actually fires.** Buy already worked (test 33); List fixed
   this session (see item 3).
8. ✅ **Events out. No English parser.** Confirmed no runtime English parsing exists. Every action
   in the Active Bar emits events through `IMatchView`/`ApplyResult.Events`. (Some section-6
   catalog events unrelated to this bar — `MatchStarted`, `WillPaid`, `HonorChanged`,
   `BondAttached`, `KeywordGranted` — are not yet emitted; see gaps below.)

## Engine B §8 acceptance tests — all 32 pass

All 32 named tests in the law's section 8 map 1:1 by name/order to `Test01`…`Test32` in
`AcceptanceTests.cs` and pass, plus 17 additional tests (Store List/Buy/Sell edge cases, loader
validation, pregame flow, legendary draft, mulligan, seat randomization) for **49/49 total**.

## Known gaps beyond the Active Bar (not fixed this session — out of scope for "smallest change")

These are real deviations from the law, left alone because no Active Bar item or acceptance test
requires them yet, and building them now would be exactly the kind of unrequested architecture the
brief warns against (in particular, a generic 29-op effects interpreter is a large, separate piece
of work):

- **No generic effects interpreter.** Every `EffectOp` in law 5.3 is declared but never dispatched;
  all effect-like behavior is hardcoded (mostly against `SITH-001`'s printing id specifically).
  A second card with the same JSON shape as Jar Jar would do nothing.
- `Sweep` keyword is not declared anywhere (not even in the `Keyword` enum).
- `Closed` and `Toll` keywords exist as data but are never enforced at targeting/announce time.
- `Doubleteam`'s Guard-side half (helper called when the defender is later Pressed) is dead code —
  `Match.DoubleteamGuardBonus` is read but never written.
- `Hunted` can only be eliminated-on, never applied, as a keyword/effect.
- `Aftereffect` has no generic C7 hook for card-authored abilities.
- `Silence` the player action isn't gated on the caster actually having the Silence/Now keyword.
- `PlayerActionKind.Activate` is declared but unhandled (`"Unsupported action."`).
- Bonds enter the Field like any permanent but never get `hostInstanceId` set or emit
  `BondAttached` — only the *removal* side (host leaves -> Bond removed) is implemented.
- `Honor` is set to 0 at setup and never read or written anywhere else.
- Store refill (rule 4.12.99, "after a Store action resolves, refill the Store to 7 if able") only
  actually runs after `Row`; `Buy`/`Trade`/`Sell` leave a gap in the Store until something else
  triggers a refill.
- Opening hand size is 7 (`MatchConstants.OpeningHandSize`), not 5 as law rule 4.1.3 states; a
  fresh match's active player ends up with 8 hand cards after the first Start-step draw. This is
  covered by an existing test (`PregameFlow_DisabledByDefaultSkipsStraightToStart`) asserting the
  7-card behavior as intended, so it reads as a deliberate design change rather than a bug — flagging
  it here since it contradicts the law document.
- The Legendary Icon draft / Mulligan pregame flow (`MatchSetup.cs`, several `PlayerActionKind`
  values) is a substantial addition not specified anywhere in the law document.
