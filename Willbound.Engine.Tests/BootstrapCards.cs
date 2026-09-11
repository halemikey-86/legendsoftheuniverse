using System.Collections.Generic;
using Willbound.Engine;

namespace Willbound.Engine.Tests
{
    public static class BootstrapCards
    {
        public static IEnumerable<CardPrinting> All => new[]
        {
            VanillaIcon(),
            VanillaStriker(),
            SilenceSurge(),
            DarthJarJar(),
            ClosedTank(),
            TokenCompanion(),
            VanillaCompanion(),
            VanillaAlgorithm(),
            WillSite(),
            BondCard(),
        };

        public static CardPrinting VanillaIcon() => new CardPrinting
        {
            Id = "VANILLA-ICON",
            Name = "Vanilla Icon",
            Type = CardType.Icon,
            Strike = 3,
            Guard = 4,
            Health = 8,
            StartsInPlay = true,
        };

        public static CardPrinting VanillaStriker() => new CardPrinting
        {
            Id = "VANILLA-STRIKER",
            Name = "Vanilla Striker",
            Type = CardType.Companion,
            WillCost = 2,
            StoreWorth = 2,
            Strike = 4,
            Guard = 2,
            Health = 3,
        };

        public static CardPrinting VanillaCompanion() => new CardPrinting
        {
            Id = "VANILLA-COMPANION",
            Name = "Vanilla Companion",
            Type = CardType.Companion,
            WillCost = 1,
            StoreWorth = 1,
            Strike = 2,
            Guard = 2,
            Health = 2,
        };

        public static CardPrinting SilenceSurge() => new CardPrinting
        {
            Id = "SILENCE-NOW",
            Name = "Silence",
            Type = CardType.Surge,
            WillCost = 1,
            StoreWorth = 1,
            Keywords = new List<string> { "Now", "Silence" },
            Abilities = new List<AbilityPrinting>
            {
                new AbilityPrinting { Name = "Silence", Timing = Timing.Now, Text = "Counter target Stack object." },
            },
        };

        public static CardPrinting DarthJarJar() => CardPrintingLoader.Parse(@"{
            ""schemaVersion"": ""1.3"",
            ""id"": ""SITH-001"",
            ""name"": ""Darth Jar Jar — The Phantom Menace"",
            ""type"": ""Icon"",
            ""willCost"": 0,
            ""strike"": 3,
            ""guard"": 4,
            ""health"": 8,
            ""startsInPlay"": true,
            ""keywords"": [""Aggression"", ""Bazerk""],
            ""abilities"": [
              {
                ""name"": ""Phantom Hand"",
                ""timing"": ""clash"",
                ""text"": ""First Press each Clash has Aggression."",
                ""effects"": [
                  { ""op"": ""GrantKeywordOnDeclare"", ""filter"": ""FirstPressYouDeclareThisClash"", ""keyword"": ""Aggression"" },
                  { ""op"": ""PutCounter"", ""when"": ""OnPressDealtDamage"", ""counter"": ""anger"", ""amount"": 1, ""target"": ""Self"" }
                ]
              }
            ]
        }");

        public static CardPrinting ClosedTank() => new CardPrinting
        {
            Id = "CLOSED-TANK",
            Name = "Closed Tank",
            Type = CardType.Companion,
            WillCost = 2,
            StoreWorth = 2,
            Strike = 2,
            Guard = 5,
            Health = 5,
            Keywords = new List<string> { "Closed", "Toll" },
        };

        public static CardPrinting TokenCompanion() => new CardPrinting
        {
            Id = "TOKEN-COMPANION",
            Name = "Token Companion",
            Type = CardType.Token,
            Strike = 1,
            Guard = 1,
            Health = 1,
        };

        public static CardPrinting VanillaAlgorithm() => new CardPrinting
        {
            Id = "VANILLA-ALGO",
            Name = "Vanilla Algorithm",
            Type = CardType.Algorithm,
            WillCost = 2,
            StoreWorth = 2,
            Abilities = new List<AbilityPrinting>
            {
                new AbilityPrinting { Name = "Then Spell", Timing = Timing.Then, Text = "Draw 1." },
            },
        };

        public static CardPrinting WillSite() => new CardPrinting
        {
            Id = "WILL-SITE-1",
            Name = "Training Ground",
            Type = CardType.WillSite,
            WillCost = 0,
            StoreWorth = 1,
        };

        public static CardPrinting BondCard() => new CardPrinting
        {
            Id = "VANILLA-BOND",
            Name = "Vanilla Bond",
            Type = CardType.Bond,
            WillCost = 1,
            StoreWorth = 1,
        };

        public static CardPrinting StandAgainBody() => new CardPrinting
        {
            Id = "STAND-AGAIN",
            Name = "Stand Again Companion",
            Type = CardType.Companion,
            WillCost = 2,
            Strike = 2,
            Guard = 2,
            Health = 3,
            Keywords = new List<string> { "StandAgain" },
        };

        public static CardPrinting SealedBody() => new CardPrinting
        {
            Id = "SEALED-BODY",
            Name = "Sealed Companion",
            Type = CardType.Companion,
            WillCost = 2,
            Strike = 2,
            Guard = 2,
            Health = 3,
            Keywords = new List<string> { "Sealed" },
        };

        public static CardPrinting AbsoluteStriker() => new CardPrinting
        {
            Id = "ABSOLUTE-STRIKER",
            Name = "Absolute Striker",
            Type = CardType.Companion,
            WillCost = 3,
            Strike = 3,
            Guard = 1,
            Health = 3,
            Keywords = new List<string> { "Absolute" },
        };

        public static CardPrinting GashingStriker() => new CardPrinting
        {
            Id = "GASHING-STRIKER",
            Name = "Gashing Striker",
            Type = CardType.Companion,
            WillCost = 3,
            Strike = 3,
            Guard = 1,
            Health = 3,
            Keywords = new List<string> { "Gashing" },
        };

        public static CardPrinting HeavyHitter() => new CardPrinting
        {
            Id = "HEAVY-HITTER",
            Name = "Heavy Hitter",
            Type = CardType.Companion,
            WillCost = 3,
            Strike = 5,
            Guard = 1,
            Health = 3,
            Keywords = new List<string> { "HeavyHitter" },
        };

        public static CardPrinting DrainStriker() => new CardPrinting
        {
            Id = "DRAIN-STRIKER",
            Name = "Drain Striker",
            Type = CardType.Companion,
            WillCost = 3,
            Strike = 2,
            Guard = 1,
            Health = 3,
            Keywords = new List<string> { "Drain" },
        };

        public static CardPrinting BazerkStriker() => new CardPrinting
        {
            Id = "BAZERK-STRIKER",
            Name = "Bazerk Striker",
            Type = CardType.Companion,
            WillCost = 3,
            Strike = 3,
            Guard = 1,
            Health = 4,
            Keywords = new List<string> { "Bazerk" },
        };

        public static CardPrinting AggressionStriker() => new CardPrinting
        {
            Id = "AGGRO-STRIKER",
            Name = "Aggression Striker",
            Type = CardType.Companion,
            WillCost = 3,
            Strike = 4,
            Guard = 1,
            Health = 3,
            Keywords = new List<string> { "Aggression" },
        };
    }
}
