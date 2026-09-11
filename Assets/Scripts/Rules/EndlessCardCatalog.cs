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
                    3, 4, 8, true,
                    Ability("Endless", AbilityTiming.Replacement,
                        "When this Icon would be Removed, set its Health to 1 instead.",
                        oncePerGame: true)),

                Warrior("endless-02", "02/50", "Eddie \"The Professor\" Bravo", "Warrior — BJJ",
                    CardRole.PlayMaker, 3, 3, 6, 6, 5,
                    Ability("Rubber Guard", AbilityTiming.OnPress,
                        "When Eddie Presses, you may pay 1 Will. If you do, ignore 1 Guard and move 1 damage from Eddie to the opposing target.",
                        optional: true, costWill: 1)),

                Warrior("endless-03", "03/50", "Steve \"Hard Rock\" Lee", "Warrior — Taijutsu",
                    CardRole.Striker, 2, 2, 6, 5, 4,
                    Ability("Speed Training", AbilityTiming.Static,
                        "This card cannot be Guarded against if you have less Will than your opponent.")),

                Warrior("endless-04", "04/50", "Damon \"The Dempsey\" Cole", "Warrior — Boxing",
                    CardRole.Striker, 3, 3, 6, 6, 5,
                    Ability("Dempsey Roll", AbilityTiming.AfterPress,
                        "Once per turn, after you Press, you may move the attack to a different opposing target.",
                        oncePerTurn: true)),

                Warrior("endless-05", "05/50", "Sanjay \"Black Leg\" Vero", "Warrior — Savate",
                    CardRole.Striker, 4, 3, 6, 6, 5,
                    Ability("Black Leg", AbilityTiming.OnPress,
                        "Once per turn, when Sanjay Presses, you may deal 1 extra damage.",
                        oncePerTurn: true)),

                Warrior("endless-06", "06/50", "Zane \"Three Blades\" Rorik", "Warrior — Kenjutsu",
                    CardRole.Striker, 5, 4, 5, 6, 5,
                    Ability("Three-Blade Style", AbilityTiming.OnPress,
                        "When Zane Presses, you may deal 1 damage to a second opposing target.")),

                Warrior("endless-07", "07/50", "Ronan \"The Anchor\" Cross", "Warrior — Wrestling",
                    CardRole.Tank, 3, 3, 6, 7, 5,
                    Ability("The Anchor", AbilityTiming.Static,
                        "Opponents cannot move Ronan from the Field unless a card specifically names him.")),

                Warrior("endless-08", "08/50", "Ryu \"Eight Limbs\" Soren", "Warrior — Muay Thai",
                    CardRole.Striker, 4, 3, 7, 5, 6,
                    Ability("Eight Limbs", AbilityTiming.Static,
                        "You may use 1 additional Strike for each Clash this turn.")),

                Warrior("endless-09", "09/50", "Kaito \"Iron Grip\" Tanaka", "Warrior — Judo",
                    CardRole.PlayMaker, 3, 2, 5, 7, 4,
                    Ability("Iron Grip", AbilityTiming.OnPress,
                        "When Kaito Presses, you may move the target to another opposing Companion.")),

                Warrior("endless-10", "10/50", "Kiro \"The Mirror\" Ren", "Warrior — Adaptive MMA",
                    CardRole.Recursor, 4, 4, 7, 6, 4,
                    Ability("Copycat", AbilityTiming.Activated,
                        "Once per turn, you may copy the last Move or activated ability an opposing Companion used.",
                        oncePerTurn: true)),
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
