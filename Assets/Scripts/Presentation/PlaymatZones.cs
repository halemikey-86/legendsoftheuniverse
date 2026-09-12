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
        public const float PlaymatY = -0.5f;
        public static readonly Vector3 MatCenter = new(0f, PlaymatY, 1.5f);

        /// <summary>Tabletop height for cards, store, and zones (above the playmat plane).</summary>
        public const float CardY = 0.45f;
        public const float HandY = 0.42f;

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

        // Player deck (draw pile) and piles
        public static readonly Vector3 Deck = new(18.1f, CardY, -7.23f);
        public static readonly Vector3 Banished = new(-17.9f, CardY, -0.2f);

        // Icon — bottom-left (player side), mirrored top-left for the opponent
        public static readonly Vector3 Icon = new(-17.8f, CardY, -7.37f);
        public static readonly Vector3 OpponentIcon = new(-17.8f, CardY, 8.5f);
        public static readonly Vector3 OpponentFieldCenter = new(-0.09f, CardY, 6.4f);

        // Center play area
        public static readonly Vector3 FieldCenter = new(-0.09f, CardY, 3.6f);
        public static readonly Vector3 RelicBondCenter = new(-0.06f, CardY, -2.04f);
        public const int FieldSlotCount = 4;
        public const int RelicBondSlotCount = 4;
        public const float FieldSlotSpacing = 1.65f;

        // Hand (bottom band)
        public static readonly Vector3 HandCenter = new(0f, HandY, -6f);
        public static float HandSpreadSpacing => CardLayout.SpreadSpacing(CardScale);

        // Opening hand (pre-keep — middle-lower table)
        public static readonly Vector3 OpeningHandCenter = new(0f, HandY, -1.5f);
        public static float OpeningHandSpreadSpacing => CardLayout.SpreadSpacing(CardScale);

        // Engine A mat zones
        public static readonly Vector3 StackWell = new(0.5f, CardY, 6.8f);
        public static readonly Vector3 Willwell = new(-12f, CardY, 2.4f);
        public static readonly Vector3 OpponentWillwell = new(-12f, CardY, 7.4f);
        public const int WillwellSlotCount = 4;
        public const float WillwellSlotSpacing = 1.85f;
        public static readonly Vector3 HoldPlate = new(-10f, CardY, -5.2f);

        // Trackers
        public static readonly Vector3 WillRoundTrack = new(18.4f, CardY, 3.51f);
        public static readonly Vector3 Worth = new(-13.9f, CardY, 1.52f);
        public static readonly Vector3 Honor = new(-13.9f, CardY, -0.5f);
        public const int MaxRound = 8;

        public const float TableFitMargin = 1.75f;

        public static float RecommendedOrthoSize
        {
            get
            {
                var bounds = GetTableContentBounds(TableFitMargin);
                const float aspect = 16f / 9f;
                var sizeForWidth = bounds.HalfWidth / aspect;
                return Mathf.Max(bounds.HalfDepth, sizeForWidth);
            }
        }

        /// <summary>World-space XZ bounds for camera framing (mat + market row + hand + trackers).</summary>
        public static TableContentBounds GetTableContentBounds(float margin = TableFitMargin)
        {
            var cardHalfW = CardLayout.Width(CardScale) * 0.5f;
            var cardHalfD = CardLayout.Depth(CardScale) * 0.5f;

            var minX = -MatWidth * 0.5f - margin;
            var maxX = MatWidth * 0.5f + margin;
            var minZ = HandCenter.z - cardHalfD - margin;
            var maxZ = MatCenter.z + (MatDepth * 0.5f) + margin;

            maxZ = Mathf.Max(maxZ, StoreRowCenter.z + cardHalfD + margin);
            maxZ = Mathf.Max(maxZ, Supply.z + cardHalfD + margin);
            minX = Mathf.Min(minX, Banished.x - cardHalfW - margin, Icon.x - cardHalfW - margin);
            maxX = Mathf.Max(maxX, Deck.x + cardHalfW + margin, WillRoundTrack.x + cardHalfW + margin);
            minZ = Mathf.Min(minZ, Deck.z - cardHalfD - margin, Icon.z - cardHalfD - margin);

            return new TableContentBounds(minX, maxX, minZ, maxZ);
        }

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

        public static Vector3 GetOpponentFieldSlot(int index)
        {
            var startZ = OpponentFieldCenter.z - ((FieldSlotCount - 1) * FieldSlotSpacing * 0.5f);
            return new Vector3(OpponentFieldCenter.x, OpponentFieldCenter.y, startZ + (index * FieldSlotSpacing));
        }

        public static Vector3 GetWillwellSlot(int index)
        {
            var i = Mathf.Clamp(index, 0, WillwellSlotCount - 1);
            var startZ = Willwell.z - ((WillwellSlotCount - 1) * WillwellSlotSpacing * 0.5f);
            return new Vector3(Willwell.x, Willwell.y, startZ + (i * WillwellSlotSpacing));
        }

        public static Vector3 GetOpponentWillwellSlot(int index)
        {
            var i = Mathf.Clamp(index, 0, WillwellSlotCount - 1);
            var startZ = OpponentWillwell.z - ((WillwellSlotCount - 1) * WillwellSlotSpacing * 0.5f);
            return new Vector3(OpponentWillwell.x, OpponentWillwell.y, startZ + (i * WillwellSlotSpacing));
        }

        public static Vector3 GetRelicBondSlot(int index)
        {
            var startZ = RelicBondCenter.z - ((RelicBondSlotCount - 1) * FieldSlotSpacing * 0.5f);
            return new Vector3(RelicBondCenter.x, RelicBondCenter.y, startZ + (index * FieldSlotSpacing));
        }
    }

    public readonly struct TableContentBounds
    {
        public TableContentBounds(float minX, float maxX, float minZ, float maxZ)
        {
            MinX = minX;
            MaxX = maxX;
            MinZ = minZ;
            MaxZ = maxZ;
        }

        public float MinX { get; }
        public float MaxX { get; }
        public float MinZ { get; }
        public float MaxZ { get; }
        public float CenterX => (MinX + MaxX) * 0.5f;
        public float CenterZ => (MinZ + MaxZ) * 0.5f;
        public float HalfWidth => (MaxX - MinX) * 0.5f;
        public float HalfDepth => (MaxZ - MinZ) * 0.5f;
    }
}
