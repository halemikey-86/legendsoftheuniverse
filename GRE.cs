/* Legends Of The Universe — Dual-Engine Spec (Unity)

Use this as a design bible and as a Unity implementation prompt. Two engines only. They never call each other’s internals.



Engine A — Presentation (3D / animation)

Job: look, feel, timing, VFX.



Does not: decide legality, winners, or stack order.

Unity prompt (Presentation)

Unity 3D presentation layer for Legends Of The Universe. Do not implement rules.



Render a dark comic-book table: midnight navy, charcoal ink, thin bronze rims, gold fire only in Willwells.

Shared card BACK on every card. Fronts vary by tier (Standard series frames, gold Icon frame, Universe nebula + red trim).



Animate only what the Rules Engine tells you:

- CardDraw, CardPlay, CardMove(zoneFrom, zoneTo)

- Press, Hold, Damage, Heal, Remove

- StoreRefill, Buy, Trade, Dump

- WillAdd, WorthChange, HonorChange

- StackPush, StackResolve, PriorityPass

- TokenSpawn(Wall|Hugacef), TokenDie

- IconEnter, UniverseAssemble

- Win, Lose



Use ScriptableObject skins. Do not store game state here.

Subscribe to IPresentationEvents from the Rules Engine.

All animation is fire-and-forget plus an optional OnAnimComplete callback so the Rules Engine can wait.

2–10 player table layouts. Empty playmat background is the gothic storm city; slots are separate prefabs spawned on top.

Presentation objects



TableView — camera, lighting, 6-seat mat + optional 4 clip-on seats

CardView — mesh + front/back materials

ZoneView — Icon, Field, Hand, Willwell, Out, Store slots, Supply, Sold, Wall, Hugacef

DialView — Worth 0–20, Honor 0–10

VfxLibrary — Press slash, Hold shield, Will flame, Honor spark, Store purchase flash

AnimQueue — serializes visuals so two Presses don’t overlap





Engine B — Rules (WILLBOUND working title)

Job: the whole game. Magic-like priority and state. You track every object, zone, cost, and timing.

You never invent house rules. If a card and this document conflict, the card wins for that object only.

If two cards conflict, last-applied timestamp wins unless a card says “cannot.”



Does not: play animations. It emits events.

Unity prompt (Rules)

Unity-agnostic C# rules engine for WILLBOUND. No MonoBehaviour in the core. No animation.



Implement:

- GameState, PlayerState, CardInstance, Zone, Stack, Priority

- Turn structure, timing words (Now / Until end of turn)

- Cost payment (Will), Store actions (Buy, Trade, Dump), Honor, Worth

- Combat: Press and Hold, Strike vs Guard vs Health

- One Will type (round-based pool; see §2)

- Card types: Icon, Companion, Relic, Bond, Surge, Will, Universe, Token

- Effects as data (EffectDef) resolved on a stack

- Event log so Presentation can subscribe

- Deterministic RNG seed

- 2 to 10 players

- Deck max 100

- Win: last Icon with Health > 0, or card-text win (Wish / Snap only if those cards resolve)



════════════════════════════════════

1. OBJECTS AND ZONES

════════════════════════════════════

Card types: Icon, Companion, Relic, Bond, Surge, Will (site).

Special objects: tokens, Wall tokens, Hugacef tokens, Universe cards.

Universe cards are not a sixth ordinary type. They are a special object with their own frame (borderless nebula, thin red trim). A Universe card still has a play type printed on it (usually Relic or Bond) PLUS the Universe stamp. Rules that name “Relic” or “Bond” see that printed type. Rules that name “Universe” see the stamp.



Zones (10th Planet • Willbound playmat — player at bottom):

Market row (top):
- SUPPLY — face-down shared pile, leftmost market slot
- STORE — 7 face-up river slots to the right of Supply

Left column:
- DECK — player deck (below ROW/LIST control area)
- OUT-OF-PLAY — removed / dumped cards
- BANISHED — left the game

Center:
- ICON — one Icon per player (large center-left frame)
- FIELD — up to 4 Companion slots (vertical strip, center)
- RELIC / BOND — up to 4 Relic or Bond slots (vertical strip, center-right)

Bottom:
- HAND — player hand band

Right column:
- WILL / ROUND — round tracker 1–8 (Will pool = round number, cap 8)
- WORTH — store currency dial
- HONOR — honor dial

Rules-only zones (not on mat art):
- Willwell (Will sites; they do not generate Will)
- Token pile (not searchable)
- Universe lock (optional face-up row for assembled sets: 5 Bands, 7 Ragon Orbs, 6 Fininty Gems). Cards in a lock are still in the Field.

Presentation anchors live in PlaymatZones.cs.



Pack size: 100 cards max. 3 Icons per 100-card pack. Gold frame = Icon only. Universe never uses the gold Icon wrap.



One Icon per player in the Field.



════════════════════════════════════

2. WILL SYSTEM (LOCKED)

════════════════════════════════════

Will is the only cast resource. One type.



Shared round token. After the last player in seating ends their turn, increment round by 1. Cap 8. Later rounds still grant 8 Will.



At the START of each player’s turn, that player’s Will pool is set to EXACTLY the current round number.

Round 1 = 1 Will … Round 8+ = 8 Will.

Leftover Will does NOT bank. Pool is overwritten.



Pay Play costs and named-spell costs from the pool. No overspend. Pay only with priority.



Will sites: at most ONE per turn, your turn, no Will cost. They do NOT add Will. Rider only. Errata: ignore “add 1 Will to your Willwell.”



Store actions spend WORTH. Honor is not Will. Discounts min 0.



════════════════════════════════════

3. OTHER CURRENCIES

════════════════════════════════════

Worth: Store money. Default start 3.

Honor: only when a card says gain or spend.

Deck may not exceed 100 after a Buy.



════════════════════════════════════

4. UNIVERSE OBJECTS

════════════════════════════════════

Universe cards use nebula + red trim. They follow their printed type for targeting (Relic, Bond, etc.).

Set assembly (only if the card names the set):

- 5 Bands → may play Ptainac Plenet for 0 Will this turn

- 7 Ragon Orbs → one Wish (card text on the 7th)

- 6 Fininty Gems → one Snap (card text on the 6th)

Assembled pieces stay in Field or the Universe lock. Removing one piece breaks the set until replaced.

Banished Universe pieces do not count toward assembly.

Silence can stop a Universe activated spell. Sweep that says “every Companion” does not hit Universe Relics.



════════════════════════════════════

5. TURN ORDER

════════════════════════════════════

1. Start: Will pool = round number. Clear “this turn” flags.

2. Will site step: may play one Will card.

3. Main: play cards, activate spells, ONE Store action.

4. Clash: Press or Hold.

5. End. If last in seating, increment round.



════════════════════════════════════

6. PRIORITY (MAGIC-LIKE)

════════════════════════════════════

Active player gets priority first after each event and after each spell resolves.

With priority: play a Surge, activate a legal spell, or pass.

All pass → resolve top of stack. Stack empty + all pass → next step.

Stack: LIFO. Surges and activated spells use the stack.

Playing Icon / Companion / Relic / Bond / Will site / Universe object does NOT use the stack unless a card says it can be Silenced as played.

Silence targets a spell on the stack.



════════════════════════════════════

7. COMBAT WORDS

════════════════════════════════════

Strike = Press damage.

Guard = prevented from that Press. Damage = max(0, Strike − Guard), then lose Health.

Health ≤ 0 → Remove unless Sealed.

Press = attack. Hold = no Press this turn.

Default one Press or Hold per Icon/Companion per turn.



════════════════════════════════════

8. KEYWORDS

════════════════════════════════════

Removed = out-of-play.

Banished = left the game.

Sealed = no Remove by damage or Remove spells. Banish / bounce / Silence still work.

Closed = opponents cannot target it. Non-target “every” effects still hit.

Toll N = extra N Will to target it.

Silence = counter spell on the stack.

Token = Field only; Removed tokens vanish.

Universe = special stamp; see §4.



════════════════════════════════════

9. STATE CHECKS

════════════════════════════════════

After every resolution and after damage:

- Health ≤ 0 and not Sealed → Remove

- Bond host gone → Bond to out-of-play

- Icon Health ≤ 0 → that player loses unless a card says otherwise

- Store holes refill from Supply after a Store action

- Draw from empty deck → lose unless a card says otherwise

- Deck count > 100 → illegal; last card acquired is reversed

- Universe set missing a piece → set bonus off



Icon Health ≤ 0 → that player is eliminated immediately.

Their Field, hand, Willwell, and deck go to out-of-play.

Cards they do not control (Store, Supply, other players’ Field) stay.

Sealed does not save an Icon from this check unless a card says “this Icon cannot cause elimination.”



════════════════════════════════════

9b. JAMES THE ENDLESS — ENGINE BLOCK 01–10

════════════════════════════════════

Set: James The Endless (50). Shared card back: 10th Planet.

Mapping: Warriors → Companion. James 01 is the only Icon. willCost → Play. storeWorth → Store. S/G/H → Strike / Guard / Health. Keywords only when card text uses one.

Setup (solo James):
- startsInPlay on James → Icon slot at setup; do not draw him.
- Cards 02–10 go in deck. Use EndlessDeckSetup.ConfigureSoloJames or RulesEngine.StartJamesEndlessDemo.

Endless (James, replacement, oncePerGame):
- When James would be Removed, set remaining Health to 1 instead — before elimination, not as a post-check.
- After first use, oncePerGame lock (endless-01:Endless:p{playerId}). Second trip to 0 eliminates.

Combat hooks (EndlessCombatEngine):
- Steve (03): if controller Will pool < defender Will pool at Press, treat defender Guard as 0 for that Press only.
- Eddie (02): optional 1 Will on Press — ignore 1 Guard; 1 of Eddie’s incoming Clash damage redirects to his Press target.
- Damon (04): after Press damage assigned, once per turn retarget same damage to another legal opposing Icon or Companion.
- Sanjay (05): optional once per turn +1 damage on Press.
- Zane (06): optional 1 damage to a second opposing target after his Press.
- Ronan (07): cannot move from Field unless naming card names Ronan / The Anchor.
- Ryu (08): +1 Press allowed per Clash this turn (ClashesThisTurn at Clash step start).
- Kaito (09): optional retarget Press to another opposing Companion before damage.
- Kiro (10): once per turn activated — copy last opposing Companion Move or activated ability remembered in game state (no Play cost on copy).

Catalog: EndlessCardCatalog.Block01To10 registered via WillboundCardCatalog.Load().



════════════════════════════════════

10. HOW ENGINE B ANSWERS

════════════════════════════════════

Always return: whose turn, whose priority, round, Will pools, Worth, Honor, stack (top last), what happened, legal actions for the player with priority.

Judge lines as: cost → stack → responses → resolve → state check.

Never generate flavor. Never change Will or the 100-card cap.



════════════════════════════════════

11. EVENT LIST (Rules → Presentation)

════════════════════════════════════

TurnStart, StepStart, PriorityChanged, StackChanged, CardMoved, CostsPaid, DamageDealt, Healed, Removed, WillChanged, WorthChanged, HonorChanged, StoreChanged, PlayerOut, GameOver, RoundChanged



Minimal C# shape



RulesEngine

  GameState

    Players[]

    Supply, Store[], Sold, Round

    Stack

    PriorityIndex, ActivePlayerIndex

    RNG

  PlayerState

    Icon, Hand, Field, Willwell, Deck, Out

    WillPool, Worth, Honor

    HasTakenStoreAction, HasPlayedWillSiteThisTurn

  CardInstance

    Def, Controller, Zone, Damage, AttachedTo, Exhausted, Timestamps

  EffectDef

    CostWill, Targets, Ops[]

  StackItem

    Source, Effect, Targets



Two sentences to pin the split:



Engine B says “this Press is legal, target takes 3, card dies.”

Engine A plays the slash, the Health chip, the card flip to Out.
*/
