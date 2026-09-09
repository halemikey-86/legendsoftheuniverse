using UnityEngine;

namespace LegendsOfTheUniverse.Presentation
{
    /// <summary>
    /// World-space anchors aligned to the 10th Planet • Willbound playmat (32×18 world units).
    /// Player sits at the bottom (−Z). Market row is toward +Z.
    /// </summary>
    public static class PlaymatZones
    {
        public const float MatWidth = 32f;
        public const float MatDepth = 18f;
        public static readonly Vector3 MatCenter = new(0f, 0f, 1.5f);

        public const float CardY = 0.05f;
        public const float HandY = 0.04f;

        /// <summary>Uniform world scale for every card on the table.</summary>
        public const float CardScale = 3.8f;

        public const float StoreCardScale = CardScale;
        public const float HandKeptScale = CardScale;
        public const float OpeningDealScale = CardScale;
        public const float IconScale = CardScale;
        public const float PileCardScale = CardScale;

        // Market row (top)
        public static readonly Vector3 Supply = new(-12.5f, CardY, 8.5f);
        public static readonly Vector3 StoreRowCenter = new(0.5f, CardY, 8.5f);
        public const int StoreSlotCount = 7;
        public const float StoreSpacing = 2.55f;

        // Left column
        public static readonly Vector3 Deck = new(-14f, CardY, 5.5f);
        public static readonly Vector3 OutOfPlay = new(-14f, CardY, 2.5f);
        public static readonly Vector3 Banished = new(-14f, CardY, -0.5f);
        public static readonly Vector3 ListViewAnchor = new(-13.5f, CardY, 7.5f);

        // Center play area
        public static readonly Vector3 Icon = new(-5.74f, 0f, 1.87f);
        public static readonly Vector3 FieldCenter = new(-2.5f, CardY, 2f);
        public static readonly Vector3 RelicBondCenter = new(5.5f, CardY, 2f);
        public const int FieldSlotCount = 4;
        public const int RelicBondSlotCount = 4;
        public const float FieldSlotSpacing = 1.65f;

        // Hand (bottom band)
        public static readonly Vector3 HandCenter = new(0f, HandY, -6f);
        public static float HandSpreadSpacing => CardLayout.SpreadSpacing(CardScale);

        // Opening hand (pre-keep — middle-lower table)
        public static readonly Vector3 OpeningHandCenter = new(0f, HandY, -1.5f);
        public static float OpeningHandSpreadSpacing => CardLayout.SpreadSpacing(CardScale);

        // Right trackers
        public static readonly Vector3 WillRoundTrack = new(14f, CardY, 5.5f);
        public static readonly Vector3 Worth = new(14f, CardY, 2.5f);
        public static readonly Vector3 Honor = new(14f, CardY, -0.5f);
        public const int MaxRound = 8;

        public static float RecommendedOrthoSize => (MatDepth * 0.5f) + 0.75f;

        public static Vector3 GetStoreSlot(int index)
        {
            var startX = StoreRowCenter.x - ((StoreSlotCount - 1) * StoreSpacing * 0.5f);
            return new Vector3(startX + (index * StoreSpacing), StoreRowCenter.y, StoreRowCenter.z);
        }

        public static Vector3 GetFieldSlot(int index)
        {
            var startZ = FieldCenter.z - ((FieldSlotCount - 1) * FieldSlotSpacing * 0.5f);
            return new Vector3(FieldCenter.x, FieldCenter.y, startZ + (index * FieldSlotSpacing));
        }

        public static Vector3 GetRelicBondSlot(int index)
        {
            var startZ = RelicBondCenter.z - ((RelicBondSlotCount - 1) * FieldSlotSpacing * 0.5f);
            return new Vector3(RelicBondCenter.x, RelicBondCenter.y, startZ + (index * FieldSlotSpacing));
        }
    }
}
