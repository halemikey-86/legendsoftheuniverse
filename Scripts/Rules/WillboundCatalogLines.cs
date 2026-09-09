namespace LegendsOfTheUniverse.Rules
{
    /// <summary>
    /// Canonical WILLBOUND card catalog (Block 1). One line per card.
    /// Format: NAME | TYPE | ROLE | PACK | PLAY | STORE | S/G/H | TEXT
    /// </summary>
    public static class WillboundCatalogLines
    {
        public const string Data = @"
Lnadod Purmt | Icon | Play Maker | Oval Years | 3 | — | 4/2/8 | You're Fired (2): Deal 4 to target Companion or Relic. If Removed, gain 1 Honor. || Build the Wall (1): This Icon Holds. Create Wall token 0/4/4. || Art of the Deal (2): Another Companion you control gets +2 Strike this turn. Its next spell costs 1 less Will.
Ptainac Plenet | Icon | Recursor | Classic Cartoon | 5 | — | 6/3/10 | Play for 0 Will if you control all 5 Bands (Ertha, Rife, Nwid, Tawer, Traeh). || Heart of the Plan (2): Other Companions you control get +1 Strike and +1 Guard this turn. || Go Planet (3): Deal 3 to each opposing Companion.
Weorge Gashinton | Icon | Tank | Politics of Time | 3 | — | 3/4/8 | Crossing (2): This Icon Holds. Create two Riverman tokens 1/2/2. || First Chair (2): Other Companions you control get +1 Guard this turn. Gain 1 Honor if you Held this turn.
Brahama Nicolln | Icon | Healer | Politics of Time | 3 | — | 2/3/9 | Emancipation Line (2): Target Companion gains 4 Health (max printed+2). || Address (3): Each player gains 1 Worth. You gain 2 Honor.
Lujisu Sareac | Icon | Striker | Politics of Time | 4 | — | 5/2/7 | Die (2): Deal 3 to target Companion. If Removed, gain 1 Honor. || Ides (3): This Icon Press. Ignore 2 Guard. You lose 2 Health.
Leocaptra | Icon | Play Maker | Politics of Time | 3 | — | 3/2/7 | Barge (1): Look at the Store. You may swap one Store card with the top of the Supply. || Asp (2): Deal 2 to target Icon. Gain 1 Worth.
Poleonap | Icon | Striker | Politics of Time | 4 | — | 5/3/6 | March (2): Target Companion gets +3 Strike this turn. || Crown (3): Gain 2 Honor. This Icon Press.
Chillurch | Icon | Tank | Politics of Time | 3 | — | 2/4/8 | Finest Hour (2): Prevent the next 4 damage to any Icon or Companion. || V-Sign (1): Draw 1. If you Held this turn, gain 1 Honor.
Dhinag | Icon | Healer | Politics of Time | 2 | — | 1/3/8 | Salt Path (1): You and target player each gain 1 Worth. || Fast (2): Target Companion cannot Press this turn. Gain 1 Honor.
Dalnema | Icon | Play Maker | Politics of Time | 3 | — | 2/3/9 | Long Walk (2): Put the top of the Supply into the Store. Gain 1 Honor. || Robben Key (3): Draw 2. You may Trade this turn without using your Store action.
Llib Nontilc | Icon | Play Maker | Oval Years | 3 | — | 3/2/7 | Sax Line (1): Draw 1. Gain 1 Worth. || Fog the File (2): Look at target hand. They discard a Surge if they have one. || Third Way (2): You may Buy or Trade this turn without using your Store action. Gain 1 Honor.
Nelle Yelpir | Icon | Tank | Mostorno Cycle | 3 | — | 3/4/8 | Last Soul (2): This Icon Holds. Prevent the next 3 damage to any Companion. || Nuke the Site (3): Remove all Companions. You lose 2 Health. Gain 2 Honor.
Neeuq Morphoxen | Icon | Recursor | Mostorno Cycle | 5 | — | 6/3/10 | Brood (2): Create two Hugacef tokens 2/1/2. || Acid Crown (3): This Icon Press. Ignore Guard. If a Companion is Removed this way, Banish it instead.
Divad of Yetani | Icon | Play Maker | Mostorno Cycle | 3 | — | 3/3/7 | Protocol (1): Look at top 2 of Supply. Put one into the Store. || Ampule (2): Deal 2 to target Icon. Gain 1 Worth. That player draws 1.
Ryllahi Nontilc | Companion | Tank | Oval Years | 3 | 3 | 2/4/6 | Briefing Book (1): This Companion Holds. Your Icon gets +1 Guard this turn. || It Takes a Hall (2): Target Companion gains 3 Health (max printed+2). Gain 1 Worth.
Daj Necav | Companion | Striker | Oval Years | 2 | 2 | 3/2/5 | Hill Speech (1): Deal 2 to target Companion. If you have 2+ Honor, deal 3 instead. || Couch Fire (2): This Companion Press. Ignore 1 Guard. Draw 1.
Iconom Aksniwel | Companion | Play Maker | Oval Years | 2 | 2 | 2/1/4 | Blue Dressing (1): Look at target hand. Gain 1 Worth. || Testimony (2): Target Icon cannot use spells this turn. You gain 1 Honor. That player draws 1.
Senatus Elder | Companion | Tank | Politics of Time | 2 | 2 | 1/4/5 | Veto (1): Target spell costs 1 more Will this turn.
Street Tribune | Companion | Play Maker | Politics of Time | 2 | 2 | 2/1/4 | Crowd Read (1): Look at target hand.
Red Courier | Companion | Striker | Politics of Time | 1 | 1 | 2/1/3 | Dispatch (1): Deal 1 to any target. If you scored Honor this turn, draw 1.
Cabinet Shade | Companion | Recursor | Politics of Time | 3 | 3 | 2/2/5 | Leak (2): Put a card from the Store on top of your deck.
Iron Ladybird | Companion | Tank | Politics of Time | 3 | 3 | 2/4/6 | Handbag (2): This Companion Holds. The next Store action an opponent takes costs 1 more Worth.
Desert Colonel | Companion | Striker | Politics of Time | 3 | 3 | 4/2/5 | Radio (2): Deal 3 to target Companion.
Velvet Speaker | Companion | Healer | Politics of Time | 2 | 2 | 1/2/5 | Balcony (1): Target Companion gains 3 Health (max printed+2).
Gulag Clerk | Companion | Recursor | Politics of Time | 2 | 2 | 1/3/4 | File (1): Dump a card, then you may Buy as if you had 1 extra Worth.
Tea-Harbor Rascal | Companion | Play Maker | Politics of Time | 2 | 2 | 2/1/4 | Crate (1): Gain 1 Worth. Deal 1 to target Relic.
Olympia Lawgiver | Companion | Healer | Politics of Time | 2 | 2 | 1/3/5 | Tablet (2): Prevent the next 3 damage to target Icon or Companion. Gain 1 Honor.
Qin Road Warden | Companion | Tank | Politics of Time | 3 | 3 | 2/5/6 | Wall Duty (1): Create Wall token 0/3/3.
Habsburg Twin | Companion | Play Maker | Politics of Time | 3 | 3 | 2/2/5 | Marriage Pact (2): Another Companion you control gets +1 Strike and +1 Guard. You may Trade this turn.
Bolivar Rider | Companion | Striker | Politics of Time | 3 | 3 | 4/1/5 | Pass (2): This Companion Press. Ignore 1 Guard. If a Companion is Removed, gain 1 Honor.
Suffrage Banneress | Companion | Healer | Politics of Time | 2 | 2 | 1/2/5 | Vote (1): You and one opponent each draw 1.
Ink Ambassador | Companion | Play Maker | Politics of Time | 2 | 2 | 1/2/4 | Protocol (1): Look at top 2 of Supply. Put one into the Store.
Cartago Specter | Companion | Recursor | Politics of Time | 4 | 4 | 3/3/6 | Salt (3): Remove target Relic. Gain 2 Honor.
Skcih | Companion | Tank | Mostorno Cycle | 2 | 2 | 2/3/5 | Cover (1): Target Companion Holds.
Twen | Companion | Healer | Mostorno Cycle | 1 | 2 | 1/2/3 | Hide (1): Target Companion is Closed until your next turn.
Nosduh | Companion | Play Maker | Mostorno Cycle | 2 | 2 | 2/1/4 | Game Over (1): Draw 1. Lose 1 Health on your Icon.
Hopsib | Companion | Healer | Mostorno Cycle | 3 | 3 | 1/3/6 | White Blood (2): Target Companion gains 4 Health (max printed+3). It is Sealed this turn.
Sallad | Companion | Tank | Mostorno Cycle | 2 | 2 | 2/3/5 | Airlock (2): Banish target Bond.
Sha | Companion | Recursor | Mostorno Cycle | 3 | 3 | 3/2/5 | Company Line (2): Look at a hand. You may put one Relic from it into the Store.
Zeuqsav | Companion | Striker | Mostorno Cycle | 2 | 2 | 3/1/4 | Smart Gun (1): Deal 2 to target Companion.
Enopa | Companion | Tank | Mostorno Cycle | 2 | 2 | 2/3/5 | Squad (1): Other Companions you control get +1 Guard this turn.
Namral | Companion | Play Maker | Mostorno Cycle | 2 | 2 | 1/2/4 | Map Panic (1): Look at the Store. Swap one card with the top of the Supply.
Nollid | Companion | Tank | Mostorno Cycle | 3 | 3 | 2/4/6 | Fire and Word (2): This Companion Holds. Deal 2 to target Companion.
Snemele | Companion | Healer | Mostorno Cycle | 2 | 2 | 1/2/5 | Infirmary (1): Target Companion gains 3 Health (max printed+2).
Wahs | Companion | Play Maker | Mostorno Cycle | 3 | 3 | 2/2/5 | Map of the Makers (2): Put the top of the Supply into the Store. Gain 1 Honor.
Hugacef | Companion | Striker | Mostorno Cycle | 1 | 1 | 2/1/2 | When this Presses a Companion, that Companion cannot Press next turn. If you control Neeuq Morphoxen, Play cost is 0.
Chest-Splitling | Companion | Recursor | Mostorno Cycle | 2 | 2 | 3/0/2 | When played, deal 2 to target Companion. If Removed, gain 1 Honor.
Morphoxen | Companion | Striker | Mostorno Cycle | 3 | 3 | 4/2/4 | Pack Hunter (1): This Companion Press. If you control Neeuq Morphoxen, ignore 2 Guard.
Reenigne | Companion | Play Maker | Mostorno Cycle | 4 | 4 | 3/3/7 | Font (2): Put the top of the Supply into the Store. || Ampule Seed (3): Create a Hugacef token.
Cagna Marta | Relic | — | Politics of Time | 3 | 3 | — | Store prices you pay are 1 less, min 1.
Oval Desk | Relic | — | Politics of Time | 2 | 3 | — | Sign (1): Gain 1 Worth or gain 1 Honor.
Iron Curtain Rod | Relic | — | Politics of Time | 2 | 2 | — | Opponents' Surges cost 1 more Will.
Ballot Box | Relic | — | Politics of Time | 1 | 2 | — | Count (1): Draw 1, then Dump 1.
War Map Table | Relic | — | Politics of Time | 2 | 3 | — | Strikers you control get +1 Strike.
Peace Pipe Rack | Relic | — | Politics of Time | 2 | 2 | — | Cease (2): No player may Press this turn. Each player gains 1 Worth.
Guillotine Beam | Relic | — | Politics of Time | 3 | 3 | — | Drop (2): Destroy target Companion with Guard 2 or less. Gain 1 Honor.
Embassy Seal | Relic | — | Politics of Time | 2 | 2 | — | Once per turn, you may Trade without using your Store action.
Black-Bag Vault | Relic | — | Willbound Tools | 3 | 3 | — | Bag (2): Banish target Companion or Relic.
Stand-Again Drum | Relic | — | Willbound Tools | 2 | 3 | — | Drum (1): If target Companion you control would be Removed this turn, it is not. It Holds instead.
Pulse Longarm | Relic | — | Mostorno Cycle | 2 | 3 | — | Strikers you control get +1 Strike.
Power Frame | Relic | — | Mostorno Cycle | 3 | 3 | — | Clamp (2): Remove target Relic or Bond.
Motion Beep | Relic | — | Mostorno Cycle | 1 | 2 | — | Ping (1): Look at target hand.
Flame Tube | Relic | — | Mostorno Cycle | 2 | 2 | — | Burn (1): Deal 2 to target Companion. If you control Nelle Yelpir and it is a token, Banish it.
Derelict Beacon | Relic | — | Mostorno Cycle | 2 | 3 | — | Once per turn you may put a Hugacef token into play paying 1 Will.
Black Ampule | Relic | — | Mostorno Cycle | 3 | 4 | — | Dose (2): Deal 3 to target Icon OR your Icon gains 3 Health this turn. If you control Divad of Yetani, do both.
Company Contract | Relic | — | Mostorno Cycle | 2 | 3 | — | Store prices you pay are 1 less (min 1). You lose 1 Honor when you Buy.
Lead-Works Chain | Relic | — | Mostorno Cycle | 2 | 2 | — | Drag (1): Target Companion gets -2 Strike this turn.
Ovumorph Clutch | Relic | — | Mostorno Cycle | 2 | 3 | — | Hatch (2): Create a Hugacef token. If you control Neeuq Morphoxen, create two instead.
Rally Wall | Relic | — | Oval Years | 2 | 2 | — | Wall tokens you control get +1 Guard. If you control Lnadod Purmt, create a Wall token when you Hold with your Icon.
Sax Case | Relic | — | Oval Years | 1 | 2 | — | Once per turn, when you draw, gain 1 Worth. If you control Llib Nontilc, also gain 1 Honor the first time each turn.
Cracked Bell | Relic | — | Politics of Time | 2 | 3 | — | Healers you control get +1 Health. If you control Brahama Nicolln, once per turn a Healer spell costs 1 less Will.
Sash of Office | Bond | — | Politics of Time | 1 | 2 | — | Attach to Icon or Companion. +1 Guard and +1 Health.
Campaign Ribbon | Bond | — | Politics of Time | 1 | 2 | — | Attach to Icon or Companion. +1 Strike. If on an Icon, first Honor you gain each turn is +1.
Exile Shackle | Bond | — | Politics of Time | 2 | 2 | — | Attach to an opponent Companion. -2 Strike.
Laurel Circlet | Bond | — | Politics of Time | 2 | 3 | — | Attach to an Icon. Whenever that Icon Removes a Companion, gain 1 Honor.
Ironclad Seal | Bond | — | Willbound Tools | 2 | 3 | — | Attach to Icon or Companion. Attached is Sealed.
Closed Door | Bond | — | Willbound Tools | 2 | 2 | — | Attach to Icon or Companion. Attached is Closed.
Toll Plate | Bond | — | Willbound Tools | 1 | 2 | — | Attach to Icon or Companion. Toll 2.
Acid Veins | Bond | — | Mostorno Cycle | 2 | 2 | — | Attach to your Companion. When it is Removed by damage, deal 2 to the source.
Face Lock | Bond | — | Mostorno Cycle | 2 | 2 | — | Attach to opponent Companion. Cannot Press. At start of their turn lose 1 Health.
Wey-Tag | Bond | — | Mostorno Cycle | 1 | 2 | — | Attach to Icon. First Store action each turn costs 1 less Worth.
Resin Cocoon | Bond | — | Mostorno Cycle | 2 | 2 | — | Attach to your Companion. Sealed. Cannot Press.
Briefing Binder | Bond | — | Oval Years | 1 | 2 | — | Attach to a Companion. +1 Guard. If you control Ryllahi Nontilc, Sealed while she Holds.
Filibuster | Surge | — | Politics of Time | 1 | 1 | — | Now: Target spell costs 2 more Will this turn.
Coup Hour | Surge | — | Politics of Time | 2 | 2 | — | Now: Deal 3 to target Companion. That player gains 1 Worth.
Summit | Surge | — | Politics of Time | 1 | 2 | — | Now: Each player may Trade. You gain 1 Honor.
Embargo | Surge | — | Politics of Time | 2 | 2 | — | Now: Players cannot Buy this turn.
Band Ertha | Universe Relic | — | Classic Cartoon | 2 | 3 | — | Counts as Ertha toward Ptainac Plenet.
Band Rife | Universe Relic | — | Classic Cartoon | 2 | 3 | — | Counts as Rife toward Ptainac Plenet.
Band Nwid | Universe Relic | — | Classic Cartoon | 2 | 3 | — | Counts as Nwid toward Ptainac Plenet.
Band Tawer | Universe Relic | — | Classic Cartoon | 2 | 3 | — | Counts as Tawer toward Ptainac Plenet.
Band Traeh | Universe Relic | — | Classic Cartoon | 2 | 3 | — | Counts as Traeh toward Ptainac Plenet.
The Eno Band | Universe Relic | — | Universe | 4 | 5 | — | You choose the target of the first Press each turn.
Ragon Orb I | Universe Relic | — | Universe | 2 | 3 | — | Counts as 1 of 7 toward Wish.
Fininty Gem Ecaps | Universe Relic | — | Universe | 2 | 3 | — | Counts as Ecaps toward Snap.
Fininty Gem Worep | Universe Relic | — | Universe | 2 | 3 | — | Counts as Worep toward Snap.
Fininty Gem Ytilaer | Universe Relic | — | Universe | 2 | 3 | — | Counts as Ytilaer toward Snap.
Fininty Gem Luos | Universe Relic | — | Universe | 2 | 3 | — | Counts as Luos toward Snap.
Fininty Gem Emit | Universe Relic | — | Universe | 2 | 3 | — | Counts as Emit toward Snap.
Fininty Gem Dnim | Universe Relic | — | Universe | 2 | 3 | — | Counts as Dnim toward Snap.
Pardon | Surge | — | Politics of Time | 1 | 2 | — | Now: Return target Companion from out-of-play to its owner's hand.
Leak the File | Surge | — | Politics of Time | 1 | 1 | — | Now: Look at target hand. They discard a Relic if they have one.
Midnight Ride | Surge | — | Politics of Time | 1 | 2 | — | Now: Your Icon Press this turn. Draw 1.
Term Limit | Surge | — | Politics of Time | 2 | 3 | — | Now: Target Icon cannot use spells this turn.
Silence the Floor | Surge | — | Willbound Tools | 2 | 2 | — | Now: Silence target spell on the stack.
Banish Writ | Surge | — | Willbound Tools | 3 | 3 | — | Now: Banish target Companion or Relic.
Remove Order | Surge | — | Willbound Tools | 2 | 2 | — | Now: Remove target Companion.
Sweep the Chamber | Surge | — | Willbound Tools | 5 | 4 | — | Now: Remove every Companion.
Seal This Hour | Surge | — | Willbound Tools | 2 | 2 | — | Now: Target Icon or Companion is Sealed until your next turn.
Unbag | Surge | — | Willbound Tools | 2 | 2 | — | Now: Put target Banished card you own into your hand.
Lock the Hatch | Surge | — | Mostorno Cycle | 1 | 1 | — | Now: Target Companion Holds. It is Closed until your next turn.
Self-Destruct | Surge | — | Mostorno Cycle | 3 | 3 | — | Now: Deal 3 to every Companion. You lose 2 Health.
In the Pipes | Surge | — | Mostorno Cycle | 1 | 2 | — | Now: Target Companion is Closed and cannot be Pressed this turn.
Chest Split | Surge | — | Mostorno Cycle | 2 | 2 | — | Now: Remove target Companion with Health 3 or less. Create Chest-Splitling if you control Neeuq Morphoxen or Resin Hive.
Orbit Fire | Surge | — | Mostorno Cycle | 4 | 3 | — | Now: Banish target Relic. Deal 2 to target Icon.
They Mostly Come | Surge | — | Mostorno Cycle | 2 | 2 | — | Now: Create two Hugacef tokens.
Quiet in the Yard | Surge | — | Mostorno Cycle | 1 | 1 | — | Now: Silence target Surge.
Not a Board Decision | Surge | — | Mostorno Cycle | 2 | 2 | — | Now: Gain control of target Relic an opponent got from the Store this turn.
The Engineers' Gift | Surge | — | Mostorno Cycle | 3 | 3 | — | Now: Banish target Companion. You lose 1 Honor.
Fired Stamp | Surge | — | Oval Years | 1 | 2 | — | Now: Deal 2 to target Companion. If you control Lnadod Purmt, deal 4 instead.
Icy Crossing | Surge | — | Politics of Time | 2 | 2 | — | Now: Create two Riverman 1/2/2. If you control Weorge Gashinton, they get +1 Guard.
";
    }
}
