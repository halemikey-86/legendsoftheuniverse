-- Seed cards (idempotent). Re-run safe.
insert into cards (
  id, schema_version, set_name, number, series, name, type, subtype, role, frame,
  will_cost, store_worth, honor_cost, honor_gain, strike, guard, health,
  keywords, hunted, aftereffect, mend, doubleteam, starts_in_play,
  abilities, spells, flavor, art_prompt, notes
) values
(
  'endless-01', '1.2', 'James The Endless', '01/50', '10th Planet',
  'James "The Endless"', 'Icon', 'Endless', null, 'gold',
  0, 0, 0, 0, 3, 4, 8,
  '[]'::jsonb, 0, null, 0, 0, true,
  $ab$[{"name":"Endless","timing":"replacement","costWill":0,"costHonor":0,"oncePerTurn":false,"oncePerGame":true,"target":"self","text":"When this Icon would be Removed, set its Health to 1 instead."}]$ab$::jsonb,
  '[]'::jsonb,
  'You can knock me down. You cannot keep me down.',
  'Gold-framed Icon seal. A kneeling figure that will not stay down. No face likeness.',
  'Rebalanced from 7/7/7. Save does not grant Will.'
),
(
  'endless-02', '1.2', 'James The Endless', '02/50', '10th Planet',
  'Ronan', 'Companion', 'Wall', 'Tank', 'standard',
  3, 2, 0, 0, 2, 5, 6,
  '[]'::jsonb, 0, null, 0, 0, false,
  $ab$[{"name":"Wall","timing":"onHold","costWill":0,"costHonor":0,"oncePerTurn":false,"oncePerGame":false,"target":"self","text":"When this Holds, it gets +2 Guard until end of Clash."}]$ab$::jsonb,
  '[]'::jsonb,
  'The mat does not yield. Neither does he.',
  '',
  'Tank for James. Hold is the plan.'
),
(
  'endless-03', '1.2', 'James The Endless', '03/50', '10th Planet',
  'Helio', 'Companion', 'Base', 'Tank', 'standard',
  4, 3, 0, 0, 1, 6, 7,
  '["Sealed"]'::jsonb, 0, null, 0, 0, false,
  $ab$[{"name":"Base","timing":"static","costWill":0,"costHonor":0,"oncePerTurn":false,"oncePerGame":false,"target":"self","text":"Sealed while you control James The Endless."}]$ab$::jsonb,
  '[]'::jsonb,
  'A floor you cannot pick up.',
  '',
  'Sealed only while the Icon is in the Field.'
),
(
  'endless-04', '1.2', 'James The Endless', '04/50', '10th Planet',
  'Ali', 'Companion', 'Anchor', 'Tank', 'standard',
  3, 2, 0, 0, 3, 4, 5,
  '[]'::jsonb, 0, null, 0, 1, false,
  $ab$[{"name":"Anchor","timing":"onPress","costWill":0,"costHonor":0,"oncePerTurn":true,"oncePerGame":false,"target":"companion","text":"Doubleteam 1. If you Held last Clash, this Press ignores 2 Guard."}]$ab$::jsonb,
  '[]'::jsonb,
  'He arrives where the weight already is.',
  '',
  'Bridge from Hold into Press.'
),
(
  'endless-05', '1.2', 'James The Endless', '05/50', 'Video Games',
  'Ryu', 'Companion', 'Striker', 'Striker', 'standard',
  3, 3, 0, 0, 4, 1, 4,
  '["Aggression"]'::jsonb, 0, null, 0, 0, false,
  $ab$[{"name":"Hadou","timing":"activated","costWill":1,"costHonor":0,"oncePerTurn":true,"oncePerGame":false,"target":"any","text":"Deal 1 damage to a Companion. This is not a Press."}]$ab$::jsonb,
  '[]'::jsonb,
  'The swing that does not wait its turn.',
  '',
  'Press engine after the Endless save.'
),
(
  'endless-06', '1.2', 'James The Endless', '06/50', 'History',
  'Steve', 'Companion', 'Pressure', 'Striker', 'standard',
  2, 2, 0, 0, 3, 2, 4,
  '[]'::jsonb, 0,
  $ae${"text":"Mend your Icon 1.","oncePerTurn":true,"costWill":0}$ae$::jsonb,
  0, 0, false,
  $ab$[{"name":"Chain","timing":"onPress","costWill":0,"costHonor":0,"oncePerTurn":false,"oncePerGame":false,"target":"self","text":"If this Removes a Companion, you may Press again with another Companion you control."}]$ab$::jsonb,
  '[]'::jsonb,
  'One opening is a career.',
  '',
  'Aftereffect mends the Icon.'
),
(
  'endless-07', '1.2', 'James The Endless', '07/50', 'Movies',
  'Damon', 'Companion', 'Closer', 'Striker', 'standard',
  4, 3, 0, 0, 5, 1, 3,
  '["HeavyHitter"]'::jsonb, 0, null, 0, 0, false,
  $ab$[{"name":"Through","timing":"afterPress","costWill":0,"costHonor":0,"oncePerTurn":false,"oncePerGame":false,"target":"icon","text":"Overkill on a Companion hits the Icon."}]$ab$::jsonb,
  '[]'::jsonb,
  'He does not stop at the body.',
  '',
  'Do not stack with Gashing on the same card.'
),
(
  'endless-08', '1.2', 'James The Endless', '08/50', '10th Planet',
  'Eddie', 'Companion', 'Redirect', 'Play Maker', 'standard',
  2, 2, 0, 0, 2, 2, 4,
  '["Drain"]'::jsonb, 0, null, 0, 0, false,
  $ab$[{"name":"Move Damage","timing":"activated","costWill":1,"costHonor":0,"oncePerTurn":true,"oncePerGame":false,"target":"companion","text":"Move 2 damage from one Companion you control to another opposing Companion."}]$ab$::jsonb,
  '[]'::jsonb,
  'The hit was always going somewhere else.',
  '',
  'Moves damage. Pairs with Kaito.'
),
(
  'endless-09', '1.2', 'James The Endless', '09/50', '10th Planet',
  'Kaito', 'Companion', 'Shift', 'Play Maker', 'standard',
  2, 2, 0, 0, 1, 3, 4,
  '[]'::jsonb, 0, null, 0, 0, false,
  $ab$[{"name":"Move Bodies","timing":"activated","costWill":1,"costHonor":0,"oncePerTurn":true,"oncePerGame":false,"target":"companion","text":"Swap the Field positions of two Companions. Guard does not change."}]$ab$::jsonb,
  '[]'::jsonb,
  'The room rearranges around the grip.',
  '',
  'Moves bodies. Pairs with Eddie.'
),
(
  'endless-10', '1.2', 'James The Endless', '10/50', '10th Planet',
  'Kiro', 'Companion', 'Copyist', 'Recursor', 'standard',
  3, 3, 0, 0, 2, 2, 4,
  '[]'::jsonb, 0, null, 0, 0, false,
  $ab$[{"name":"Copy Move","timing":"activated","costWill":2,"costHonor":0,"oncePerTurn":true,"oncePerGame":false,"target":"any","text":"Copy the last activated ability played this turn. Pay its Will cost again."}]$ab$::jsonb,
  '[]'::jsonb,
  'He keeps the last sentence and reads it louder.',
  '',
  'Copies Moves. James does not win the Clash alone.'
),
(
  'planet-01', '1.2', '10th Planet', '01/40', '10th Planet',
  'Purple Mat', 'Relic', 'Gym', null, 'standard',
  2, 2, 0, 0, 0, 0, 0,
  '["Still"]'::jsonb, 0, null, 0, 0, false,
  $ab$[{"name":"Hold Plan","timing":"static","costWill":0,"costHonor":0,"oncePerTurn":false,"oncePerGame":false,"target":null,"text":"Companions you control have Still. After Pressing they still count as Holding."}]$ab$::jsonb,
  '[]'::jsonb,
  'The floor is a teacher with no mouth.',
  '',
  'Turns Hold into a plan.'
),
(
  'planet-02', '1.2', '10th Planet', '02/40', '10th Planet',
  'Cage Fence', 'Relic', 'Cage', null, 'standard',
  3, 3, 0, 0, 0, 0, 0,
  '["Closed"]'::jsonb, 0, null, 0, 0, false,
  $ab$[{"name":"Fence","timing":"static","costWill":0,"costHonor":0,"oncePerTurn":false,"oncePerGame":false,"target":null,"text":"Your Icon is Closed. Opponents cannot target it unless they pay Toll 2."}]$ab$::jsonb,
  '[]'::jsonb,
  'There is a wall. Then there is the fence.',
  '',
  'Closed + Toll on the Icon.'
),
(
  'planet-03', '1.2', '10th Planet', '03/40', '10th Planet',
  'Gi Lapel', 'Bond', 'Grip', null, 'standard',
  1, 1, 0, 0, 0, 0, 0,
  '[]'::jsonb, 0, null, 0, 0, false,
  $ab$[{"name":"Grip","timing":"onPlay","costWill":0,"costHonor":0,"oncePerTurn":false,"oncePerGame":false,"target":"companion","text":"Attach to a Companion. It gets +1 Strike. Leaves if the host leaves."}]$ab$::jsonb,
  '[]'::jsonb,
  'Cloth becomes law.',
  '',
  'Bond. Host leaves, this leaves.'
),
(
  'planet-04', '1.2', '10th Planet', '04/40', '10th Planet',
  'Sprawl', 'Surge', 'Now', null, 'standard',
  1, 1, 0, 0, 0, 0, 0,
  '["Still"]'::jsonb, 0, null, 0, 0, false,
  $ab$[{"name":"Sprawl","timing":"now","costWill":1,"costHonor":0,"oncePerTurn":false,"oncePerGame":false,"target":"companion","text":"A Companion you control Holds this Clash. Guard stays on."}]$ab$::jsonb,
  '[]'::jsonb,
  'Weight first. Argument later.',
  '',
  'Word is Now. Uses the stack.'
),
(
  'planet-05', '1.2', '10th Planet', '05/40', '10th Planet',
  'Grounded', 'Will', 'Site', null, 'standard',
  0, 0, 0, 0, 0, 0, 0,
  '[]'::jsonb, 0, null, 0, 0, false,
  $ab$[{"name":"Rider","timing":"startStep","costWill":0,"costHonor":0,"oncePerTurn":false,"oncePerGame":false,"target":null,"text":"While this is your Will site, the first Companion you control that Holds each Clash Mends your Icon 1."}]$ab$::jsonb,
  '[]'::jsonb,
  'The well is a gym that learned to sit still.',
  '',
  'Will site. No Will generated. Rider only. One site per turn.'
),
(
  'planet-06', '1.2', '10th Planet', '06/40', '10th Planet',
  'BJJ Counters', 'Surge', 'Now', null, 'standard',
  2, 2, 0, 0, 0, 0, 0,
  '["Silence"]'::jsonb, 0, null, 0, 0, false,
  $ab$[{"name":"Counter","timing":"now","costWill":2,"costHonor":0,"oncePerTurn":false,"oncePerGame":false,"target":"any","text":"Silence a Surge or activated spell on the stack."}]$ab$::jsonb,
  '[]'::jsonb,
  'The answer was already in the grip.',
  '',
  'Silence. Legal whenever you have priority.'
),
(
  'mostorno-01', '1.2', 'Mostorno', '01/30', 'Politics of Time',
  'Queen', 'Companion', 'Brood', 'Play Maker', 'standard',
  4, 4, 0, 1, 2, 3, 5,
  '[]'::jsonb, 0, null, 0, 0, false,
  $ab$[{"name":"Eggs","timing":"activated","costWill":2,"costHonor":0,"oncePerTurn":true,"oncePerGame":false,"target":null,"text":"Create a Hugacef token (Companion, Strike 1, Guard 0, Health 1). Tokens have no Store Worth."}]$ab$::jsonb,
  '[]'::jsonb,
  'A court that hatches instead of marching.',
  '',
  'Token engine into Gashing or Hunted.'
),
(
  'mostorno-02', '1.2', 'Mostorno', '02/30', 'Politics of Time',
  'Hugacef', 'Companion', 'Spawn', 'Striker', 'standard',
  1, 0, 0, 0, 1, 0, 1,
  '["Gashing"]'::jsonb, 0, null, 0, 0, false,
  $ab$[{"name":"Tooth","timing":"static","costWill":0,"costHonor":0,"oncePerTurn":false,"oncePerGame":false,"target":"self","text":"Gashing. Any damage this deals to a Companion Removes it. Token. Cannot be Bought."}]$ab$::jsonb,
  '[]'::jsonb,
  'Small, and then there is no body.',
  '',
  'Printed token reference. storeWorth 0.'
),
(
  'time-01', '1.2', 'Politics of Time', '01/30', 'Politics of Time',
  'Toll of Hours', 'Relic', 'Law', null, 'standard',
  3, 3, 0, 2, 0, 0, 0,
  '["Closed"]'::jsonb, 0, null, 0, 0, false,
  $ab$[{"name":"Toll 2","timing":"static","costWill":0,"costHonor":0,"oncePerTurn":false,"oncePerGame":false,"target":null,"text":"Opponents pay Toll 2 extra Will to target you. When they do, you gain 1 Honor."}]$ab$::jsonb,
  '[]'::jsonb,
  'Time is a fee, not a river.',
  '',
  'Honor lane. Never mix Honor with Will or Worth.'
),
(
  'time-02', '1.2', 'Politics of Time', '02/30', 'Politics of Time',
  'Sweep the Record', 'Surge', 'Now', null, 'standard',
  5, 4, 2, 0, 0, 0, 0,
  '[]'::jsonb, 0, null, 0, 0, false,
  $ab$[{"name":"Sweep","timing":"now","costWill":5,"costHonor":2,"oncePerTurn":false,"oncePerGame":false,"target":null,"text":"Remove all Companions. Relics and Universe Remnants stay. Honor is spent, not Will alone."}]$ab$::jsonb,
  '[]'::jsonb,
  'The minutes keep the furniture and throw the guests.',
  '',
  'Universe Remnants live in Relic slots so Sweep does not eat the set.'
),
(
  'cartoon-01', '1.2', 'Classic Cartoon', '01/20', 'Classic Cartoon',
  'The One Ring', 'Relic', 'Remnant', null, 'universe',
  4, 6, 0, 0, 0, 0, 0,
  '["Sealed"]'::jsonb, 0, null, 0, 0, false,
  $ab$[{"name":"Set Piece","timing":"static","costWill":0,"costHonor":0,"oncePerTurn":false,"oncePerGame":false,"target":null,"text":"Universe Remnant. Sealed. If you control the full Classic Cartoon remnant set, deploy the matching Icon for free."}]$ab$::jsonb,
  '[]'::jsonb,
  'One fragment of a myth, worn as a Relic so Sweep cannot eat it.',
  'Borderless nebula, thin red trim, a single ring hovering over a dark field. No character likeness.',
  'Universe = nebula + red trim. Gold is reserved for high-value Icons.'
)
on conflict (id) do nothing;