# WILLBOUND Engine B — Unity Integration

Engine B is the deterministic rules kernel for WILLBOUND. It has **zero** `UnityEngine` references.

## Projects

| Project | Role |
|---|---|
| `Willbound.Engine` | Pure C# rules kernel (`netstandard2.1`) |
| `Willbound.Engine.Tests` | NUnit acceptance tests |
| `Assets/Scripts/Presentation/` | Engine A — listens to events, renders the table |

## How Unity talks to the kernel

1. Load card printings from catalog JSON (`CardPrintingLoader.ParseMany`).
2. Build a `Match` via `MatchSetup.Create` or `MatchRunner.FromSetup`.
3. Create `MatchRunner(match, database, rng, view)`.
4. On player input, call `runner.Apply(new PlayerAction { ... })`.
5. Implement `IMatchView.OnEvent(GameEvent e)` to animate HUD/cards — **never compute damage in Unity**.

```csharp
public class TableMatchBridge : MonoBehaviour, IMatchView
{
    MatchRunner runner;

    void Start()
    {
        var db = new InMemoryCardDatabase(CatalogLoader.LoadPrintings());
        runner = MatchRunner.FromSetup(db.All, setupPlayers, seed: 42);
        runner = new MatchRunner(runner.Match, db, new SeededRng(42), this);
    }

    public void OnBuyClicked(int slot)
    {
        var result = runner.Apply(new PlayerAction
        {
            Kind = PlayerActionKind.StoreBuy,
            PlayerId = localPlayerId,
            StoreSlotIndex = slot,
        });
        if (!result.Success)
            ShowError(result.Error);
    }

    public void OnEvent(GameEvent e)
    {
        switch (e.Kind)
        {
            case EventKind.DamageDealt:
                AnimateDamage(e);
                break;
            case EventKind.CardMoved:
                MoveCardView(e);
                break;
        }
    }
}
```

## Action → event flow

- **In:** `PlayerAction` (Pass, PlayCard, DeclarePress, DeclareHold, Answer, Store*, Silence, plus the pregame actions below)
- **Out:** `ApplyResult` with `Success`, optional `Error`, and `GameEvent[]`
- Illegal actions do **not** mutate state.

## Pregame flow (opt-in)

`MatchSetup.Create(..., enablePregameFlow: true)` inserts two steps before Round 1's Start step:
`Setup → LegendaryDraft → Mulligan → Start → …`. Off by default (`enablePregameFlow: false`,
matching every existing call site and test) — plain `Create`/`FromSetup` calls skip straight to
`Start` exactly as before.

- **LegendaryDraft**: a `SetupPlayer` with `DraftLegendaryIcon = true` brings no fixed `IconId`.
  Instead the kernel offers 3 random Icons flagged `IsLegendary` (`Player.LegendaryChoices`). The
  player resolves this with `PickLegendaryIcon` (assigns `Player.Icon`, the other two go to
  `Match.Supply`) or, once, `CycleLegendaryIcon` (rerolls the 3 choices — costs that player's entire
  first round of Will, via `Player.SkipFirstWill`). Declined choices are shuffled into Supply once
  every seated player has picked.
- **Mulligan**: every player resolves their opening hand (dealt at `MatchConstants.OpeningHandSize`,
  7 cards) with exactly one of `KeepHand`, `CycleHandCard` (swap one named card for a fresh draw), or
  `Mulligan` (shuffle the hand back into the deck, draw `MatchConstants.MulliganHandSize`, 6).

Pregame actions bypass the normal priority check — each player resolves their own step independently,
regardless of `Match.PriorityPlayerId`. `MatchSetup.AdvancePregameIfReady` runs after each one and
advances the phase once every seated (non-`Lost`) player has finished the current step.

### Round terminology mapping

External "3-phase round" language maps onto the kernel's real `Phase` steps like this (no separate
step exists for "Phase 2" — it's the priority window already built into every step):

| External name | Kernel steps |
|---|---|
| Phase 1 (play/shop before combat) | `Site` + `Main` |
| Phase 2 (stack/priority resolution) | The priority window inherent in every step (rules 4.5–4.7) |
| Phase 3 (combat + end) | `Clash` (`C0`–`C8`) + `End` |

### Turn timer

A 75-second per-turn timer is **intentionally not implemented in the kernel** — Engine B's
determinism rules forbid time-based logic. Implement it client-side in Unity: start a countdown on
`TurnStarted`/`PhaseChanged`, and call `runner.Apply(new PlayerAction { Kind = PlayerActionKind.Pass, ... })`
(or another forced default action) on expiry.

## Card data

- JSON schema **1.2** and **1.3** supported via `CardPrintingLoader`.
- `abilities[].text` is print-only.
- Execution uses `keywords[]` and `abilities[].effects[]`.

## Running tests

```bash
dotnet test Willbound.Engine.Tests/Willbound.Engine.Tests.csproj
```

## Vocabulary

Use WILLBOUND terms only: Field, Willwell, Store, Supply, Stack, Clash, Press, Hold, Will, Worth, Honor. Do not expose MTG names in the public API.
