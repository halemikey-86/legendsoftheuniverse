using System.Collections.Generic;
using Willbound.Engine;

namespace LegendsOfTheUniverse.Presentation.EngineBridge
{
    /// <summary>
    /// Bootstrap printings for the rules kernel until StreamingAssets export is present.
    /// </summary>
    public static class EngineCatalog
    {
        public static IReadOnlyList<CardPrinting> LoadPrintings()
        {
            return new List<CardPrinting>
            {
                VanillaIcon(),
                VanillaCompanion(),
                VanillaStriker(),
                WillSite(),
                SilenceSurge(),
                DarthJarJar(),
            };
        }

        static CardPrinting VanillaIcon() => new CardPrinting
        {
            Id = "VANILLA-ICON",
            Name = "Vanilla Icon",
            Type = CardType.Icon,
            Strike = 3,
            Guard = 4,
            Health = 8,
            StartsInPlay = true,
        };

        static CardPrinting VanillaCompanion() => new CardPrinting
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

        static CardPrinting VanillaStriker() => new CardPrinting
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

        static CardPrinting WillSite() => new CardPrinting
        {
            Id = "WILL-SITE-1",
            Name = "Training Ground",
            Type = CardType.WillSite,
            WillCost = 0,
            StoreWorth = 1,
        };

        static CardPrinting SilenceSurge() => new CardPrinting
        {
            Id = "SILENCE-NOW",
            Name = "Silence",
            Type = CardType.Surge,
            WillCost = 1,
            StoreWorth = 1,
            Keywords = new List<string> { "Now", "Silence" },
        };

        static CardPrinting DarthJarJar() => CardPrintingLoader.Parse(@"{
            ""schemaVersion"": ""1.3"",
            ""id"": ""SITH-001"",
            ""name"": ""Darth Jar Jar"",
            ""type"": ""Icon"",
            ""willCost"": 0,
            ""strike"": 3,
            ""guard"": 4,
            ""health"": 8,
            ""startsInPlay"": true,
            ""keywords"": [""Aggression"", ""Bazerk""],
            ""abilities"": [{
                ""name"": ""Phantom Hand"",
                ""timing"": ""clash"",
                ""text"": ""First Press each Clash has Aggression."",
                ""effects"": [
                    { ""op"": ""GrantKeywordOnDeclare"", ""filter"": ""FirstPressYouDeclareThisClash"", ""keyword"": ""Aggression"" },
                    { ""op"": ""PutCounter"", ""when"": ""OnPressDealtDamage"", ""counter"": ""anger"", ""amount"": 1, ""target"": ""Self"" }
                ]
            }]
        }");

        public static List<string> DefaultDeck(int count = 30, string cardId = "VANILLA-COMPANION")
        {
            var deck = new List<string>(count);
            for (var i = 0; i < count; i++)
                deck.Add(cardId);
            return deck;
        }
    }
}
