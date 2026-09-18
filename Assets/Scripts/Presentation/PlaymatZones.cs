using UnityEngine;



namespace LegendsOfTheUniverse.Presentation

{

    /// <summary>

    /// World-space anchors aligned to the official single-player playmat.

    /// Values come from <see cref="TableLayoutSettings"/> (tunable at runtime).

    /// </summary>

    public static class PlaymatZones

    {

        static TableLayoutData L => TableLayoutSettings.Active;



        public static float MatScaleX => L.matScaleX;

        public static float MatScaleZ => L.matScaleZ;

        public static float MatWidth => L.MatWidth;

        public static float MatDepth => L.MatDepth;

        public static float PlaymatY => L.playmatY;

        public static Vector3 MatCenter => L.MatPosition;



        public static float CardY => L.cardY;

        public static float HandY => L.handY;



        public static float CardScale => L.cardScale;

        public static float StoreCardScale => L.cardScale;

        public static float HandKeptScale => L.cardScale;

        public static float OpeningDealScale => L.cardScale;

        public static float IconScale => L.iconScale;

        public static float PileCardScale => L.cardScale;



        public static Vector3 Supply => L.Supply;

        public static Vector3 SupplyNumberOffset => L.SupplyNumberOffset;



        public static readonly Vector3 HiddenStoreAnchor = new(0f, -8f, 0f);

        public static Vector3 StoreRowCenter => HiddenStoreAnchor;

        public const int StoreSlotCount = 7;

        public const float StoreSpacing = 2.55f;



        public static Vector3 Deck => L.Deck;

        public static float DeckCardScale => L.deckCardScale;

        public static Vector3 Discard => L.Discard;

        public static Vector3 Icon => L.Icon;



        public const int FieldDropRows = 2;

        public const int FieldDropColumns = 5;

        public const int FieldSlotCount = FieldDropRows * FieldDropColumns;

        public static Vector3 FieldDropZoneScale => new(L.fieldColliderX, L.fieldSlotSpacingZ, 0.982f);

        public static Vector3 FieldDropColliderSize => L.FieldDropColliderSize;

        public static Vector3 FieldDropGridCenter => L.FieldDropGridCenter;

        public static Vector2 FieldDropSlotSpacing => L.FieldDropSlotSpacing;



        public static Vector3 FieldCenter => FieldDropGridCenter;

        public static Vector3 RelicBondCenter => L.RelicBondCenter;

        public const int RelicBondSlotCount = 6;

        public static float FieldSlotSpacing => L.fieldSlotSpacing;



        public static Vector3 HandCenter => L.HandCenter;

        public static float HandSpreadSpacing => CardLayout.SpreadSpacing(CardScale);



        public static Vector3 OpeningHandCenter => L.OpeningHandCenter;

        public static float OpeningHandSpreadSpacing => CardLayout.SpreadSpacing(CardScale);



        public static Vector3 StackWell => L.StackWell;

        public static Vector3 Willwell => L.Willwell;

        public static Vector3 HoldPlate => L.HoldPlate;



        public static Vector3 RoundTrack => L.RoundTrack;

        public static Vector3 WorthTrack => L.WorthTrack;

        public static Vector3 WillTrack => L.WillTrack;

        public static Vector3 Honor => L.Honor;



        public static Vector3 WillRoundTrack => WillTrack;

        public static Vector3 Worth => WorthTrack;

        public static Vector3 Banished => Discard;



        public const int MaxRound = 8;



        public static float TableFitMargin => L.tableFitMargin;

        public static float PlaymatCameraMargin => L.playmatCameraMargin;

        public static float PlaymatCameraZoom => L.cameraZoomMultiplier;



        public static float RecommendedOrthoSize

        {

            get

            {

                var bounds = GetPlaymatCameraBounds(PlaymatCameraMargin);

                const float aspect = 16f / 9f;

                var sizeForWidth = bounds.HalfWidth / aspect;

                return Mathf.Max(bounds.HalfDepth, sizeForWidth);

            }

        }



        public static TableContentBounds GetPlaymatCameraBounds(float margin = -1f)

        {

            if (margin < 0f)

                margin = PlaymatCameraMargin;



            var halfWidth = (MatWidth * 0.5f) + margin;

            var halfDepth = (MatDepth * 0.5f) + margin;

            return new TableContentBounds(-halfWidth, halfWidth, -halfDepth, halfDepth);

        }



        public static TableContentBounds GetTableContentBounds(float margin = -1f)

        {

            if (margin < 0f)

                margin = TableFitMargin;



            var cardHalfW = CardLayout.Width(CardScale) * 0.5f;

            var cardHalfD = CardLayout.Depth(CardScale) * 0.5f;

            var deckHalfW = CardLayout.Width(DeckCardScale) * 0.5f;

            var deckHalfD = CardLayout.Depth(DeckCardScale) * 0.5f;



            var minX = -MatWidth * 0.5f - margin;

            var maxX = MatWidth * 0.5f + margin;

            var minZ = HandCenter.z - cardHalfD - margin;

            var maxZ = MatCenter.z + (MatDepth * 0.5f) + margin;



            maxZ = Mathf.Max(maxZ, Supply.z + cardHalfD + margin);

            minX = Mathf.Min(minX, Icon.x - cardHalfW - margin, Discard.x - cardHalfW - margin);

            maxX = Mathf.Max(maxX, Deck.x + deckHalfW + margin, WillTrack.x + cardHalfW + margin);

            minZ = Mathf.Min(minZ, Deck.z - deckHalfD - margin, Icon.z - cardHalfD - margin, Discard.z - cardHalfD - margin);



            return new TableContentBounds(minX, maxX, minZ, maxZ);

        }



        public static Vector3 GetStoreSlot(int index)

        {

            var startX = StoreRowCenter.x - ((StoreSlotCount - 1) * StoreSpacing * 0.5f);

            return new Vector3(startX + (index * StoreSpacing), StoreRowCenter.y, StoreRowCenter.z);

        }



        public static Vector3 GetFieldDropSlotPosition(int row, int column)

        {

            row = Mathf.Clamp(row, 0, FieldDropRows - 1);

            column = Mathf.Clamp(column, 0, FieldDropColumns - 1);



            var startX = FieldDropGridCenter.x - ((FieldDropColumns - 1) * FieldDropSlotSpacing.x * 0.5f);

            var backRowZ = FieldDropGridCenter.z + ((FieldDropRows - 1) * FieldDropSlotSpacing.y * 0.5f);



            return new Vector3(

                startX + (column * FieldDropSlotSpacing.x),

                FieldDropGridCenter.y,

                backRowZ - (row * FieldDropSlotSpacing.y));

        }



        public static Vector3 GetFieldSlot(int index)

        {

            index = Mathf.Clamp(index, 0, FieldSlotCount - 1);

            var row = index / FieldDropColumns;

            var column = index % FieldDropColumns;

            return GetFieldDropSlotPosition(row, column);

        }



        public static void DecomposeFieldSlotIndex(int index, out int row, out int column)

        {

            index = Mathf.Clamp(index, 0, FieldSlotCount - 1);

            row = index / FieldDropColumns;

            column = index % FieldDropColumns;

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


