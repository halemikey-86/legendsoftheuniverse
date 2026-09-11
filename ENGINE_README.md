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

- **In:** `PlayerAction` (Pass, PlayCard, DeclarePress, DeclareHold, Answer, Store*, Silence)
- **Out:** `ApplyResult` with `Success`, optional `Error`, and `GameEvent[]`
- Illegal actions do **not** mutate state.

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
