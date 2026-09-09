using System;
using System.Collections.Generic;

namespace LegendsOfTheUniverse.Rules
{
    /// <summary>
    /// James The Endless — engine block 01–10. Shared card back: 10th Planet.
    /// Warriors map to Companion. willCost = Play. S/G/H = Strike / Guard / Health.
    /// </summary>
    public static class EndlessCardCatalog
    {
        public const string SetName = "James The Endless";
        public const string BackArt = "10th Planet";
        public const string JamesIconName = "James \"The Endless\"";

        public static IReadOnlyList<CardDef> Block01To10 { get; } = BuildBlock();

        public static void RegisterInto(Dictionary<string, CardDef> byName)
        {
            if (byName == null)
                throw new ArgumentNullException(nameof(byName));

            foreach (var def in Block01To10)
                byName[def.Name] = def;
        }

        static List<CardDef> BuildBlock()
        {
            return new List<CardDef>
            {
                Icon(
                    "endless-01", "01/50", JamesIconName, "Endless",
                    strike: 3, guard: 4, health: 8, startsInPlay: true,
                    Ability("Endless", AbilityTiming.Replacement, oncePerGame: true,
                        "When this Icon would be Removed, set its Health to 1 instead.")),

                Warrior("endless-02", "02/50", "Eddie \"The Professor\" Bravo", "Warrior — BJJ",
                    CardRole.PlayMaker, play: 3, worth: 3, strike: 6, guard: 6, health: 5,
                    Ability("Rubber Guard", AbilityTiming.OnPress, costWill: 1, optional: true,
                        "When Eddie Presses, you may pay 1 Will. If you do, ignore 1 Guard and move 1 damage from Eddie to the opposing target.")),

                Warrior("endless-03", "03/50", "Steve \"Hard Rock\" Lee", "Warrior — Taijutsu",
                    CardRole.Striker, play: 2, worth: 2, strike: 6, guard: 5, health: 4,
                    Ability("Speed Training", AbilityTiming.Static,
                        "This card cannot be Guarded against if you have less Will than your opponent.")),

                Warrior("endless-04", "04/50", "Damon \"The Dempsey\" Cole", "Warrior — Boxing",
                    CardRole.Striker, play: 3, worth: 3, strike: 6, guard: 6, health: 5,
                    Ability("Dempsey Roll", AbilityTiming.AfterPress, oncePerTurn: true,
                        "Once per turn, after you Press, you may move the attack to a different opposing target.")),

                Warrior("endless-05", "05/50", "Sanjay \"Black Leg\" Vero", "Warrior — Savate",
                    CardRole.Striker, play: 4, worth: 3, strike: 6, guard: 6, health: 5,
                    Ability("Black Leg", AbilityTiming.OnPress, oncePerTurn: true,
                        "Once per turn, when Sanjay Presses, you may deal 1 extra damage.")),

                Warrior("endless-06", "06/50", "Zane \"Three Blades\" Rorik", "Warrior — Kenjutsu",
                    CardRole.Striker, play: 5, worth: 4, strike: 5, guard: 6, health: 5,
                    Ability("Three-Blade Style", AbilityTiming.OnPress,
                        "When Zane Presses, you may deal 1 damage to a second opposing target.")),

                Warrior("endless-07", "07/50", "Ronan \"The Anchor\" Cross", "Warrior — Wrestling",
                    CardRole.Tank, play: 3, worth: 3, strike: 6, guard: 7, health: 5,
                    Ability("The Anchor", AbilityTiming.Static,
                        "Opponents cannot move Ronan from the Field unless a card specifically names him.")),

                Warrior("endless-08", "08/50", "Ryu \"Eight Limbs\" Soren", "Warrior — Muay Thai",
                    CardRole.Striker, play: 4, worth: 3, strike: 7, guard: 5, health: 6,
                    Ability("Eight Limbs", AbilityTiming.Static,
                        "You may use 1 additional Strike for each Clash this turn.")),

                Warrior("endless-09", "09/50", "Kaito \"Iron Grip\" Tanaka", "Warrior — Judo",
                    CardRole.PlayMaker, play: 3, worth: 2, strike: 5, guard: 7, health: 4,
                    Ability("Iron Grip", AbilityTiming.OnPress,
                        "When Kaito Presses, you may move the target to another opposing Companion.")),

                Warrior("endless-10", "10/50", "Kiro \"The Mirror\" Ren", "Warrior — Adaptive MMA",
                    CardRole.Recursor, play: 4, worth: 4, strike: 7, guard: 6, health: 4,
                    Ability("Copycat", AbilityTiming.Activated, oncePerTurn: true,
                        "Once per turn, you may copy the last Move or activated ability an opposing Companion used.")),
            };
        }

        static CardDef Icon(string id, string number, string name, string subtype,
            int strike, int guard, int health, bool startsInPlay, params CardAbilityDef[] abilities)
        {
            var def = Base(id, number, name, CardType.Icon, subtype, null, 0, 0, strike, guard, health);
            def.StartsInPlay = startsInPlay;
            AddAbilities(def, abilities);
            return def;
        }

        static CardDef Warrior(string id, string number, string name, string subtype, CardRole role,
            int play, int worth, int strike, int guard, int health, params CardAbilityDef[] abilities)
        {
            var def = Base(id, number, name, CardType.Companion, subtype, role, play, worth, strike, guard, health);
            AddAbilities(def, abilities);
            return def;
        }

        static CardDef Base(string id, string number, string name, CardType type, string subtype,
            CardRole? role, int play, int worth, int strike, int guard, int health)
        {
            return new CardDef
            {
                Id = id,
                Name = name,
                SetName = SetName,
                Number = number,
                Type = type,
                DisplayType = type == CardType.Companion ? "Companion" : "Icon",
                Subtype = subtype,
                Role = role,
                Pack = SetName,
                PlayCost = play,
                StoreCost = worth,
                Strike = strike,
                Guard = guard,
                Health = health,
            };
        }

        static CardAbilityDef Ability(string name, AbilityTiming timing, string text,
            bool oncePerGame = false, bool oncePerTurn = false, bool optional = false, int? costWill = null)
        {
            return new CardAbilityDef
            {
                Name = name,
                Timing = timing,
                Text = text,
                OncePerGame = oncePerGame,
                OncePerTurn = oncePerTurn,
                Optional = optional,
                CostWill = costWill,
            };
        }

        static void AddAbilities(CardDef def, CardAbilityDef[] abilities)
        {
            foreach (var ability in abilities)
            {
                def.Abilities.Add(ability);
                if (ability.Timing == AbilityTiming.Static)
                    def.Static.Add(ability.Text);
                else if (ability.Timing == AbilityTiming.Replacement)
                    def.Triggers.Add(ability.Text);
            }
        }
    }
}
