# WILLBOUND — Engine B Prompt
## Card State · Priority · Event · Rule · Game State

Feed this entire file to the implementer (human or LLM).
Do not invent a second vocabulary. Do not port Magic: The Gathering names into the public API.
Magic is a teacher only. The table speaks WILLBOUND.

This pack is the source of truth for **Engine B**.
Engine A (faces, frames, HUD, playmat) is a Unity listener on the event stream.
The kernel has zero `UnityEngine` references.

---

## 0. How to use this pack

Produce, in order, and keep them consistent:

1. **Glossary** — every word the engine and the cards share.
2. **Rules** — numbered. Cards beat the book if they say they do.
3. **State machine** — objects, zones, step graph, Stack loop, Clash pipeline, checks.
4. **Event catalog** — every fact the kernel emits. Unity binds views to these.
5. **C# kernel** — `Willbound.Engine` targeting Unity-safe C# (lang 9, netstandard2.1).
6. **Tests** — the acceptance list at the bottom must pass.

If a later layer fights an earlier layer, stop and fix the earlier layer.
Do not patch in code comments.

One source of truth. One verb list. One Stack.

---

## 1. Locked decisions (do not reopen)

| Lock | Value |
|---|---|
| Voice | WILLBOUND-native. Field, Willwell, Store, Supply, Stack, Clash, Press, Hold, Guard, Now, Then, Remove, Banish, Worth, Honor, Will. |
| Stack | Surges, Algorithms, permanents being played, Will Sites, Store actions, activated spells, triggered abilities, **Presses** (including Answers). |
| Silence / Now | Counters **any** Stack object, including a Press. |
| Clash | Combat-step analog on the **active** player's turn only. |
| Who swings | Active player declares Press or Hold with each Ready body. A Pressed Ready defender **may** put an Answer Press on the Stack. |
| Exhaust | Turn-spent. Declaring Press, Answer, or Hold exhausts that body. Refresh at the start of **your** turn. |
| Enter | Bodies **enter Ready**. Same-turn Press is legal. |
| Play path | Everything paid or chosen as an action goes on the Stack. Including Buy / Trade / Sell / Keep / List / Row and Will Sites. |
| Draw | Start step: refresh → Will pool = round (cap 8) → draw 1. Opening hand is 5. |
| Seats | 2–10 from day one. `PlayerId` 0..n-1. Seat order = left of active, skip eliminated. |
| Host | Unity 3D. Kernel is a pure C# class library. |
| Card text | JSON is source of truth. Kernel **never** parses English. `abilities[].text` is print-only. Execution is `keywords[]` + `abilities[].effects[]`. |
| Currencies | Will (play), Worth (shop), Honor (prestige). Never mix. Anger is a **counter**, not a currency. |
| Damage | Integers only. `Damage = max(0, Strike - Guard)` unless Absolute. |

#Do not name types Instant, Sorcery, Creature, Land, Mana, Battlefield, Graveyard, Exile, Attacking, Blocking.
#Do not implement MTG layers 1–7 unless a card is impossible without a tiny continuous-effect timestamp.
#Do not put `UnityEngine` in the kernel.
#Do not parse `abilities[].text`.
#Do not bank leftover Will.
#Do not let a Will Site add Will.
#Do not give a Token Store Worth.
#Do not swap Will and Worth.
#Do not run Clash on non-active turns.
#Do not use floats for Strike, Guard, Health, damage, or Worth.

---

## 2. You are

You are the Engine B implementer for WILLBOUND.

You write a deterministic rules kernel that can run a 2–10 seat match from opening Icon through last Icon standing, including the shared Store and a full Clash.

You treat printed card English as flavor.
You treat JSON as law.

You emit events so a Unity HUD can animate without asking the kernel how to draw.

---

## 3. Layer 1 — Glossary

Use these words in code names, events, and rules text.

### 3.1 Table

| Word | Meaning |
|---|---|
| Icon | Leader body. Starts in play. Health 0 after replacements = that player is out. |
| Companion | Body. Strike, Guard, Health. Roles are flavor + filter, not rules. |
| Relic | Permanent. No body. |
| Bond | Permanent attached to a host. Leaves when the host leaves, unless text saves it. |
| Surge | Spell. Word on the card is **Now**. Legal whenever you have priority. |
| Algorithm | Spell. Word on the card is **Then**. Legal only on your turn when you have priority. |
| Will Site | Permanent in the Willwell. Play cost 0. Rider only. Does not make Will. One per turn. |
| Token | Created, never shuffled, no Worth. Ceases when it would leave the Field unless written. |
| Remnant / Universe | Frame and assembly tag. Still a Relic, Bond, or Icon underneath. Assembly is card text. |
| Body | Icon, Companion, or a Token that has S G H. |
| Permanent | Icon, Companion, Relic, Bond, Will Site, or Token in the Field or Willwell. |

### 3.2 Zones

| Word | Meaning |
|---|---|
| Field | Per player. Icon, Companions, Relics, Bonds. |
| Willwell | Per player. Sites. |
| Hand | Private. No default cap. |
| Deck | Private order. Max 100. |
| Store | Shared river. Default 7 face-up. |
| Supply | Shared facedown pile that refills the Store. |
| Removed | Public out-of-play. Pardon returns from here. |
| Banished | Gone. Unbag only. |
| Stack | Shared. Last in, first **candidate** to resolve. Clash Presses batch — see Clash. |
| Clash | The Press / Hold step on the active player's turn. |

### 3.3 Numbers

| Word | Meaning |
|---|---|
| Will | Play fuel. Pool becomes the round number at Start, cap 8. Does not bank. |
| Worth | Shop fuel. Starts at 3. Never pays Will costs. |
| Honor | Prestige. Starts at 0. Cards read it. |
| Round | Shared. Starts at 1. Increments after the last living seat's End step. |
| Strike | Printed offense. |
| Guard | Printed soak. Always on while the body is in the Field. |
| Health | 0 on a Companion = Removed. 0 on an Icon = player out. |
| Hunted | Counter. 10 on an Icon = that player is out. Guard does not stop Hunted. |
| Anger | Named counter. Usually on your Icon. Spent only as written. Not Will, Worth, or Honor. |
| Exhausted | Spent this cycle. Cannot declare Press, Answer, or Hold. |
| Ready | In Field, not Exhausted, not Removed, not Banished. |

### 3.4 Verbs

Press, Hold, Guard, Buy, Sell, Trade, Keep, List, Row, Worth, Honor, Aggression, Bazerk, Gashing, HeavyHitter, Hunted, Aftereffect, Still, Drain, Mend, Doubleteam, Sealed, Closed, Toll, Silence, Now, Then, Remove, Banish, Sweep, Pardon, Unbag, Stand Again, Exhaust, Ready, Draw, Play, Activate, Pass, Answer, Absolute.

---

## 4. Layer 2 — Rules

Numbered. Short. Cards beat this file.

### 4.1 Match setup

1. 2–10 players. Each player brings one Icon and a deck of at most 100 non-Icon cards.
2. Each Icon enters that player's Field, Ready, Health = printed, startsInPlay = true.
3. Each player draws 5, sets Worth = 3, Honor = 0, Will = 0, Hunted = 0.
4. Round = 1. Deal 7 cards from Supply into the Store, face up.
5. First active player is chosen by the host. Seat order is clockwise (left of active).
6. The first Start step still happens: refresh (noop), Will = 1, draw 1.

### 4.2 Hidden and open

7. Hidden: hand, deck order, Supply order.
8. Open: Field, Willwell, Store, Removed, Banished, Stack, Worth, Honor, Will pool, Round, exhaust, counters, Health.

### 4.3 Cards beat the book

9. If a card's `effects[]` contradicts this file, the card wins for that event.
10. Replacement effects apply before the event they replace.
11. If two replacements want the same event, the controller of the affected object chooses order.

### 4.4 Turn

12. Only the active player has a Clash step.
13. Steps in order: **Start → Site → Main → Clash → End**.
14. Each step opens with a priority window after its automatic work, unless the step says otherwise.
15. Leave a step only when the Stack is empty and every living player has passed in order.

**Start**
16. Ready every permanent you control (clear Exhausted).
17. Set your Will pool to `min(Round, 8)`. Leftover Will is gone before this set.
18. Draw 1. If you cannot, you lose (after replacements).
19. Fire `OnStart` triggers. Priority.

**Site**
20. You may play one Will Site this turn. It goes on the Stack. Cost 0 Will.
21. You may pass without a Site. Priority.

**Main**
22. You may play Companions, Relics, Bonds, Surges, Algorithms, activate spells, and take **one** Store action, each as a Stack object, whenever you have priority.
23. Playing a body uses its `willCost`. You must pay from your Will pool on announcement.
24. Priority. Repeat until you pass on an empty Stack and every opponent has passed.

**Clash** — section 4.8.

**End**
25. Fire `OnEnd` triggers. Priority.
26. Strip “this turn / this Clash” flags.
27. If you are the last living seat in the Round, increment Round by 1.

### 4.5 Priority and the Stack

28. The active player receives priority first in every window. Then living seats to the left.
29. With priority a player may:
    - play a **Now** object they can afford (Surge, or any card whose timing is Now),
    - play a **Then** object if it is their turn (Algorithm, or timing Then),
    - activate an ability whose timing allows it,
    - play a permanent or Site if the current step allows it and they are the active player (opponents do not play permanents on your Main unless a card says so),
    - take the one Store action if they are active, Main step, and they have not already,
    - declare nothing and **Pass**.
30. Opponents with priority on your turn may play Now objects, activate Now abilities, and Silence. They may not play Then objects. They may not take Store actions. They may not play permanents unless a card says so.
31. A player who Passes yields priority to the next living seat.
32. If every living player Passes and the Stack is not empty, resolve the top object, then the active player receives priority again.
33. If every living player Passes and the Stack is empty, the current window closes and the step advances — except during Clash, where empty-Stack + all-pass advances the **Clash pipeline**, not the whole turn.

### 4.6 Putting an object on the Stack

34. Announce the card or ability. Choose modes and targets now.
35. Pay costs now (Will, exhaust, sacrifice, Anger, Worth). If you cannot pay, the announcement is illegal.
36. Toll: if the target has Toll N, the announcer pays N extra Will now or the target is illegal.
37. Closed: opponents cannot choose that object as a target.
38. The object becomes the top of the Stack. Fire `OnCast` / `OnPressDeclared` triggers, then priority.

### 4.7 Resolving a non-Press object

39. If every target is illegal, the object does nothing and leaves.
40. Else run its `effects[]` in listed order.
41. Move a resolved Surge or Algorithm to Removed (unless it says otherwise).
42. Move a resolved permanent to the correct zone: Field or Willwell. Bonds attach to the chosen host. Bodies enter Ready.
43. Run checks (4.10). Then priority.

### 4.8 Clash pipeline

Clash is a step with inner phases. Presses live on the Stack so Silence can touch them. Damage does **not** resolve as naive last-in-first-out. After the answer window, the kernel **locks** remaining Presses and runs batches.

**C0 — Begin Clash**
44. Fire `OnClashBegin` triggers. Priority.

**C1 — Active declare**
45. For each Ready body the active player controls, that player must choose **Press** or **Hold**.
46. Hold: exhaust the body. It does not create a Stack object. Guard stays on. Fire `OnHold`.
47. Press: choose one opposing Icon or Companion as target. Exhaust the attacker. Create a Press object on the Stack. If the body has Bazerk, create **two** Press objects (wave 0 Aggression, wave 1 Normal) and exhaust once.
48. Doubleteam: when you declare a Press, you may call one other Companion you control in the Field. Add its Strike to a Pressing attacker, or its Guard if this body is later Pressed this Clash. The helper does not exhaust from being called.
49. Still: a body that Presses and has Still also counts as Holding this Clash.
50. Phantom-style text that grants Aggression to “the first Press you declare each Clash” applies when that Press object is created.
51. After every active body has declared, priority.

**C2 — Answers**
52. In seat order starting left of active, each other living player may, for each Ready body they control that is a target of at least one pending Press, put exactly one Answer Press on the Stack (choose a target: usually the incoming attacker, legal alternatives allowed if text says so) or decline.
53. An Answer is a Press. Same object type. Same keywords. Exhausts the defender.
54. A body that Held during C1, or that is Exhausted, cannot Answer.
55. After all seats have been offered Answers, priority.

**C3 — Interact**
56. Normal Stack rules. Silence may counter any object, including any Press.
57. A countered Press leaves the Stack and will not deal damage. It already exhausted its body.
58. Players may play Now objects here. Then objects only if it is the caster's turn.
59. When all pass and the Stack contains only Press objects (or is empty), **lock Clash**. Move every remaining Press off the Stack into the Clash queue, stable timestamp order. If non-Press objects are on the Stack, resolve them with 4.7 first — never lock over a Surge.

**C4 — Aggression wave**
60. Every queued Press with Aggression, and every Bazerk wave 0, deals damage at once.
61. Simultaneous inside the wave. If order of triggers matters, timestamp then seat order from active.
62. Run checks. Put triggers on the Stack. Priority.

**C5 — Skip**
63. Any queued Press whose **source** is no longer in the Field, or whose source has been Removed or Banished, leaves the queue without damage.
64. This is “Removed skip Press.”

**C6 — Normal wave**
65. Every remaining queued Press deals damage at once (Bazerk wave 1 lives here).
66. Run checks. Triggers. Priority.

**C7 — Aftereffects**
67. In timestamp order: HeavyHitter leftover, Aftereffect, Drain, Mend-from-Drain, Hunted application, Anger-from-connecting-Press, other `OnPressHit` effects written as Aftereffect.
68. HeavyHitter: if this Press Removed a Companion by damage, leftover = damage minus the Companion's Health as it was before the damage. Deal leftover to that Companion's Icon. Guard on the Icon still applies to leftover unless the Press has Absolute.
69. Drain: the damage **dealt** (after Guard / Absolute) also Mends the attacking player's Icon by that much, cap at printed Health.
70. Run checks. Triggers. Priority.

**C8 — End Clash**
71. Fire `OnClashEnd`. Priority.
72. Clear “this Clash” flags that say they die here. Exhaust stays until that player's next Start.

### 4.9 Damage math

73. Base damage = `max(0, StrikeEffective - GuardEffective)`.
74. StrikeEffective includes Doubleteam Strike and one-Clash grants.
75. GuardEffective includes Doubleteam Guard if the target was Pressed and a helper was called for Guard, plus one-Clash grants.
76. **Absolute**: damage = `max(Base, Ceil(StrikeEffective / 2))`. Guard cannot zero an Absolute Press.
77. **Gashing**: if any damage (1 or more) is dealt to a Companion, that Companion is Removed after this wave's damage, replacements first.
78. Health cannot go below 0. Overkill is only used by HeavyHitter.
79. Guard is always on. There is no block step.
80. Hunted is not damage. Put N Hunted as written. Guard does not stop it.

### 4.10 Checks (state-based)

Run checks after every resolution, after every damage wave, and before giving priority.

81. Companion or Token body with Health ≤ 0 → Removed, unless a replacement changes the event.
82. Bond whose host is not in the Field → Removed.
83. Token that would move to Removed or Banished → cease. It does not sit in Removed unless text says so.
84. Icon with Health ≤ 0 after replacements → that player **loses**.
85. Icon with Hunted ≥ 10 → that player **loses**.
86. A player who was forced to draw from an empty deck → that player **loses**.
87. If a loser is removed mid-match: their Field, hand, Willwell, and deck leave the game. Store stays. Stack objects they own fizzle on next resolution if they require that controller. Remaining players continue. If one Icon remains, that player wins.

### 4.11 Replacements

88. **Sealed**: cannot be Removed by damage or by the Remove verb. Banish still works.
89. **Stand Again**: would be Removed → instead remain in Field at 1 Health, or as written.
90. **Endless** and similar once-per-game saves are Stand Again with a spent flag.
91. Replacements apply before the event. They do not use the Stack.

### 4.12 Store

92. One Store action per turn, active player, Main step, on the Stack.
93. **Buy** — pay the card's Worth. Choose hand or deck. Deck size cannot exceed 100.
94. **Trade** — swap a hand card with a Store card. Pay Worth difference if the incoming card costs more.
95. **Sell / Dump** — move a hand card into the Store. Gain 1 Worth.
96. **Keep** — take nothing. Still consumes the one action.
97. **List** — look at the top 2 of Supply; one enters Store, one goes to the bottom of Supply.
98. **Row** — put the whole Store on the bottom of Supply. Deal a fresh 7. Costs 2 Worth.
99. After a Store action resolves, refill the Store to 7 from Supply if able.
100. Worth never pays Will costs. Will never pays Worth costs.

### 4.13 Winning and losing

101. You win if you are the last player with an Icon that still has Health, or a card's effect says you win.
102. You lose if 4.10 says you lose, or a card says you lose.
103. Eliminated players do not receive priority.

### 4.14 Keyword short rules

| Keyword | Engine rule |
|---|---|
| Aggression | This Press deals in C4. If it Removes the target before C6, that target's pending Press is skipped. |
| Bazerk | Declare two Presses: wave 0 Aggression, wave 1 Normal. One exhaust. |
| Gashing | Any damage to a Companion Removes it. |
| HeavyHitter | Overkill on a Companion hits its Icon in C7. |
| Hunted N | Put N Hunted on the written object. 10 on an Icon loses. |
| Aftereffect | Runs in C7 if this Press dealt damage, unless written otherwise. |
| Still | After Pressing, also counts as Holding this Clash. |
| Drain | Damage dealt also Mends your Icon that much, cap printed. |
| Doubleteam | Call one other Companion you control on declare. |
| Sealed | See 4.11. |
| Closed | Opponents cannot target it. |
| Toll N | Pay N extra Will to target it. |
| Absolute | See 4.9.76. |
| Silence / Now | Counter target Stack object. |

Do not stack Aggression + Bazerk + Gashing + HeavyHitter as four extra swings on one card. Bazerk already contains Aggression as wave 0. Other keywords modify those two waves.

---

## 5. Layer 3 — State machine

### 5.1 Objects

```
Match
  matchId
  round: int
  phase: Start | Site | Main | Clash | End
  clashPhase: C0..C8 | None
  active: PlayerId
  priority: PlayerId
  passed: set of PlayerId
  stack: StackObject[]
  clashQueue: PressObject[]
  store: CardInstance[]
  supply: CardInstance[]
  removed: CardInstance[]
  banished: CardInstance[]
  players: Player[]
  nextTimestamp: int
  winner: PlayerId | null
  eventLog: Event[]

Player
  id, name, seat
  will, worth, honor
  hunted                // on their Icon, also mirrored as a counter
  lost: bool
  deck, hand
  field: CardInstance[]
  willwell: CardInstance[]
  storeActionsThisTurn: int
  sitesPlayedThisTurn: int
  icon: CardInstance

CardInstance
  instanceId
  printing: CardPrinting   // immutable JSON
  controller: PlayerId
  owner: PlayerId
  zone
  hostInstanceId?          // Bond
  ready / exhausted
  damageMarked             // or currentHealth
  counters: map<string,int>   // hunted, anger, ...
  flagsThisTurn: set
  flagsThisClash: set
  keywordsNow: set         // printed + granted
  timestamp

StackObject
  stackId
  timestamp
  type: PlayCard | Activate | Trigger | Press | StoreAction | Silence
  controller
  sourceInstanceId?
  printingId?
  abilityIndex?
  targets: InstanceId[]
  paidWill, paidWorth
  wave: 0 | 1              // Bazerk
  keywords: set            // snapshot at declare
  strikeSnapshot, doubleteamStrike
  answeredToStackId?       // Answer Press
  effects: Effect[]

Press is a StackObject type.
Answer is a Press with answeredToStackId set.
```

### 5.2 Card printing (schema 1.3)

Keep schema 1.2 fields. Add what the kernel needs. Print text stays.

```json
{
  "schemaVersion": "1.3",
  "id": "SITH-001",
  "set": "Sith Deals In Absolute",
  "number": "01/60",
  "name": "Darth Jar Jar — The Phantom Menace",
  "type": "Icon",
  "subtype": "Hidden Lord",
  "role": null,
  "willCost": 0,
  "storeWorth": 0,
  "strike": 3,
  "guard": 4,
  "health": 8,
  "startsInPlay": true,
  "keywords": ["Aggression", "Bazerk"],
  "abilities": [
    {
      "name": "Phantom Hand",
      "costWill": 0,
      "timing": "clash",
      "oncePerTurn": false,
      "oncePerGame": false,
      "oncePerClash": false,
      "text": "The first Press you declare each Clash has Aggression. When that Press deals damage, put 1 Anger on this Icon.",
      "effects": [
        { "op": "GrantKeywordOnDeclare", "filter": "FirstPressYouDeclareThisClash", "keyword": "Aggression" },
        { "op": "PutCounter", "when": "OnPressDealtDamage", "counter": "anger", "amount": 1, "target": "Self" }
      ]
    },
    {
      "name": "Mask Off",
      "costWill": 2,
      "timing": "activated",
      "text": "This Clash, this Icon has Bazerk. If a Press by this Icon Removes a Companion or an Icon, put 2 Anger on this Icon.",
      "effects": [
        { "op": "GrantKeyword", "keyword": "Bazerk", "duration": "ThisClash", "target": "Self" },
        { "op": "PutCounter", "when": "OnPressRemovedBody", "counter": "anger", "amount": 2, "target": "Self" }
      ]
    }
  ]
}
```

`text` is display. If `effects` is missing, the ability does nothing in the kernel except print.
Do not write a sentence parser. Do write a small compiler table later if you want; it is not Engine B v1.

### 5.3 Effect ops (v1)

Implement at least:

`DealDamage`, `Mend`, `Draw`, `GainWill`, `GainWorth`, `GainHonor`,
`PutCounter`, `RemoveCounter`, `CreateToken`, `MoveZone`,
`AttachBond`, `DetachBond`, `Exhaust`, `Ready`,
`GrantKeyword`, `GrantKeywordOnDeclare`, `GiveFlag`,
`Silence`, `PreventDamage`, `ReplaceRemove`,
`SearchSupply`, `StoreBuy`, `StoreSell`, `StoreTrade`, `StoreList`, `StoreRow`,
`ChooseTarget`, `ForEach`, `If`, `Unless`.

`when` hooks: `Immediate`, `OnEnter`, `OnStart`, `OnEnd`, `OnClashBegin`, `OnClashEnd`, `OnHold`, `OnPressDeclared`, `OnPressDealtDamage`, `OnPressRemovedBody`, `OnRemoved`, `OnBanished`.

Timing enum: `static | Now | Then | activated | enter | clash | removed | trigger`.

### 5.4 Step graph

```
Setup → (Start → Site → Main → Clash[C0..C8] → End → next living seat)
      → if one Icon remains: Halt(Winner)
```

No Clash on other players' turns means: when you are not active, your turn loop does not exist. You only receive priority in their windows.

### 5.5 Determinism

- Integers only for table math.
- Timestamps monotonic.
- Simultaneous events: sort by timestamp, then seat from active, then instanceId.
- RNG, if any (first player, random Store in tests), is injected as `IRng`. Never call `System.Random` inside rules.
- No time-based logic.

---

## 6. Layer 4 — Event catalog

Unity binds to these. Names are stable.

```
MatchStarted
RoundChanged
TurnStarted { player }
PhaseChanged { phase, clashPhase }
PriorityChanged { player }
WillSet { player, amount }
WillPaid { player, amount }
WorthChanged { player, delta, reason }
HonorChanged { player, delta }
CardDrew { player, instanceId }          // HUD may hide identity from opponents
CardMoved { instanceId, from, to }
PermanentEntered { instanceId, zone }
PermanentLeft { instanceId, zone, reason }
BondAttached { bond, host }
Exhausted { instanceId }
Readied { instanceId }
StackPushed { stackId, type }
StackPopped { stackId, type, fizzled }
Silenced { stackId }
PressDeclared { stackId, source, target, wave, keywords }
PressAnswered { stackId, source, target, incoming }
HoldDeclared { source }
ClashLocked { pressIds }
DamageDealt { source, target, amount, wave, absolute }
CounterPut { instanceId, name, amount }
HealthChanged { instanceId, current, printed }
KeywordGranted { instanceId, keyword, duration }
StoreRefilled { cards }
PlayerLost { player, reason }
PlayerWon { player, reason }
CheckRan
```

Do not emit render commands. Emit facts.

---

## 7. Layer 5 — C# / Unity code prompt

Write a Unity-safe C# kernel.

### 7.1 Projects

```
Willbound.Engine          // class library, no UnityEngine
Willbound.Engine.Tests    // NUnit or Unity Test Framework against the library
Willbound.Unity           // thin adapter only: MonoBehaviours subscribe to IMatchView
```

If you are generating only one project, generate `Willbound.Engine` + tests. Leave Unity adapter as interfaces.

### 7.2 Required types

```
Match, Player, CardInstance, CardPrinting, StackObject, PressObject
Effect, EffectOp, Timing, Zone, Phase, ClashPhase, Keyword
ICardDatabase, IRng, IMatchView
MatchRunner.Tick() / Apply(PlayerAction)
PlayerAction { PlayCard, Activate, DeclarePress, DeclareHold, Answer, Pass, StoreX, Silence }
```

### 7.3 Rules of the code

- Public API is actions in, events out.
- `Match` is a plain object. Snapshot-serializable (fields, not properties with logic).
- One `Apply(action)` either mutates legally or returns `IllegalAction` without mutation.
- After every successful action, run checks, then emit events.
- Card lookup by `printing.id` (`SITH-001`, `PM-001`, `MK-01`).
- 2–10 seats from day one. Tests may use 2.
- No threads in the kernel.
- No LINQ that hides allocation rules; clarity over cleverness.
- Comments cite rule numbers from this file (`// 4.8.63 Removed skip Press`).

### 7.4 Unity boundary

```
IMatchView
  void OnEvent(GameEvent e);

// Willbound.Unity may:
// - own meshes, animation, audio
// - call runner.Apply(action) from input
// - never compute damage itself
```

Do not put Health bars in the kernel. Emit `HealthChanged`.

### 7.5 First cards the kernel must run

Bootstrap with:

1. A vanilla Icon 3/4/8.
2. A vanilla Companion Striker.
3. A Surge Silence / Now that counters a Press.
4. Darth Jar Jar (`SITH-001`) Phantom Hand + Mask Off as written in 5.2.
5. A Tank with Closed or Toll 1.
6. A Token Companion that vanishes when Removed.
7. One Store Buy.

If `effects[]` are missing on a loaded card, log once and treat the ability as vanilla.

---

## 8. Acceptance tests (must ship)

Name them. Make them fail loudly.

1. **Will reset** — Round 3 Start sets Will to 3 even if the player had 1 left.
2. **Will cap** — Round 9 Start sets Will to 8.
3. **Draw loss** — empty deck on Start draw eliminates that player.
4. **Enter Ready** — Companion played on Main can Press that Clash.
5. **Press exhausts** — same body cannot Press twice in one Clash unless Bazerk created two waves at declare.
6. **Refresh** — that body is Ready again on its controller's next Start.
7. **No Clash on defense turn** — non-active player does not get a Clash step.
8. **Answer legal** — Pressed Ready defender may put an Answer Press on the Stack.
9. **Answer illegal** — Exhausted or untargeted body cannot Answer.
10. **Silence Press** — Now Silence counters a Press; no damage from it; exhaust stays.
11. **Silence Store** — Silence counters a Buy on the Stack; Worth is already paid unless you refund on fizzle — **refund costs on Silence** (announce-cost stays paid only if the object resolves; illegal vs fizzle: paid costs of a Silenced object return Will/Worth, but exhaust from a Press does not Ready).
12. **Aggression skip** — Aggression Press Removes the defender in C4; defender's queued normal Press is gone in C5.
13. **Mutual Aggression** — both deal C4 damage; both may die before C6.
14. **Bazerk** — two waves, one exhaust, Silence on wave 0 does not delete wave 1 unless both were targeted.
15. **Absolute** — Strike 3 vs Guard 4 deals 2.
16. **Gashing** — 1 damage Removes a Companion.
17. **HeavyHitter** — 5 damage into a 2 Health Companion deals 3 leftover to the Icon, Icon Guard applies.
18. **Drain** — 2 damage dealt Mends the attacking Icon 2, not past printed.
19. **Hunted 10** — Icon with 10 Hunted loses even at full Health.
20. **Icon 0** — Icon at 0 after Stand Again is used up loses.
21. **Stand Again** — first lethal on a Stand Again body leaves it at 1.
22. **Sealed** — damage and Remove cannot take it; Banish can.
23. **Bond dies** — host Removed, Bond Removed by check.
24. **Token cease** — Token that dies is not in Removed.
25. **Site cap** — second Site the same turn is illegal.
26. **Store cap** — second Store action the same turn is illegal.
27. **Row** — costs 2 Worth, river replaced.
28. **Then illegal** — opponent cannot play an Algorithm on your turn.
29. **Now legal** — opponent can Surge on your Main and during C3.
30. **Multi seat** — 3 players, middle player loses mid-Clash, priority skips them, Store remains 7.
31. **Jar Jar Anger** — first Press each Clash gains Aggression; connecting damage puts 1 Anger on that Icon.
32. **Priority order** — after a resolve, active player gets priority first.

Test 11 lock: **Silence refunds Will and Worth. Press exhaust is not undone.**

---

## 9. Output the implementer must produce

When this prompt is executed, write:

1. `Willbound.Engine` C# sources (zones, stack, clash, checks, effects, events).
2. `Willbound.Engine.Tests` for the 32 cases above.
3. `CardPrinting` + loader for schema 1.2 and 1.3 JSON.
4. A one-page `ENGINE_README.md` that restates how Unity talks to the kernel.
5. Do not write a renderer. Do not generate card art. Do not invent MTG type names.

Stop when the tests exist and the Clash pipeline is readable against section 4.8.

---

## 10. Worked Clash (sanity)

Active A. Defender B. A controls Icon Jar Jar 3/4/8 Ready and a Striker 4/2/3 Ready. B controls Icon 3/4/8 Ready and Tank 2/5/5 Ready.

- A declares Jar Jar Press → B's Tank (first Press this Clash: Aggression). Exhaust Jar Jar.
- A declares Striker Press → B's Icon. Exhaust Striker.
- Priority. B Answers with Tank Press → A's Jar Jar. Exhaust Tank. B's Icon declines.
- C3: nobody Silences. Lock.
- C4: Jar Jar's Aggression Press deals `max(0,3-5)=0` to Tank. No Anger (no damage).
- C5: nobody Removed.
- C6: Striker deals `max(0,4-4)=0` to B Icon. Tank Answer deals `max(0,2-4)=0` to Jar Jar.
- C7: nothing connects.
- If instead Jar Jar had Absolute that Clash: C4 deals `max(0, ceil(3/2))=2` to Tank. Anger +1. Gashing would also Remove the Tank, and the Tank's Answer would skip in C5.

That is the engine.
That is the prompt.
Do not flatten it into a generic card-game framework.
