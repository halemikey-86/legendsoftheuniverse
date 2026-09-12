using UnityEngine;

namespace LegendsOfTheUniverse.Presentation
{
    /// <summary>
    /// Builds all Willbound playmat zones on the table at runtime.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-150)]
    public class PlaymatZonesView : MonoBehaviour
    {
        [Header("Prefab")]
        [SerializeField] CardView cardPrefab;

        public ZoneView Field { get; private set; }
        public ZoneView RelicBond { get; private set; }
        public ZoneView Deck { get; private set; }
        public ZoneView Banished { get; private set; }
        public DialView Will { get; private set; }
        public DialView Worth { get; private set; }
        public DialView Honor { get; private set; }
        public DialView Round { get; private set; }

        // Screen-edge HUD layout: Honor top-left, Round top-center, Worth+Will clustered top-right.
        static readonly Vector2 TopLeftAnchor = new(0f, 1f);
        static readonly Vector2 TopCenterAnchor = new(0.5f, 1f);
        static readonly Vector2 TopRightAnchor = new(1f, 1f);
        static readonly Vector2 HonorOffset = new(90f, -50f);
        static readonly Vector2 RoundOffset = new(0f, -50f);
        static readonly Vector2 WorthOffset = new(-228f, -50f);
        static readonly Vector2 WillOffset = new(-90f, -50f);

        public static PlaymatZonesView Instance { get; private set; }

        bool zonesBuilt;

        void Awake()
        {
            Instance = this;
            ResolveCardPrefab();
            CleanupStaleRuntimeZones();
            BuildZones();
        }

        void OnEnable()
        {
            CardDeck.CountChanged += OnDeckCountChanged;
        }

        void OnDisable()
        {
            CardDeck.CountChanged -= OnDeckCountChanged;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void ResolveCardPrefab()
        {
            if (cardPrefab != null)
                return;

            var storeView = GetComponent<StoreView>();
            if (storeView != null)
                cardPrefab = storeView.CardPrefab;
        }

        void CleanupStaleRuntimeZones()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                var name = child.name;
                if (name.EndsWith("Zone") || name.EndsWith("Dial"))
                    Destroy(child.gameObject);
            }

            zonesBuilt = false;
            Field = null;
            RelicBond = null;
            Deck = null;
            Banished = null;
            Will = null;
            Worth = null;
            Honor = null;
            Round = null;
        }

        void BuildZones()
        {
            if (zonesBuilt)
                return;

            zonesBuilt = true;

            Field = CreateSlotZone("Field", PlaymatZones.FieldCenter, PlaymatZones.FieldSlotCount);
            RelicBond = CreateSlotZone("Relic / Bond", PlaymatZones.RelicBondCenter, PlaymatZones.RelicBondSlotCount);
            Deck = CreatePileZone("Deck", PlaymatZones.Deck, pileStackRotation: CardView.StackRotation);
            Banished = CreatePileZone("Banished", PlaymatZones.Banished);

            Honor = CreateDial(DialKind.Honor, TopLeftAnchor, HonorOffset);
            Round = CreateDial(DialKind.Round, TopCenterAnchor, RoundOffset);
            Worth = CreateDial(DialKind.Worth, TopRightAnchor, WorthOffset);
            Will = CreateDial(DialKind.Will, TopRightAnchor, WillOffset);
        }

        ZoneView CreateSlotZone(string label, Vector3 center, int slotCount)
        {
            var zoneObject = new GameObject(label.Replace(" ", string.Empty) + "Zone");
            zoneObject.transform.SetParent(transform, false);
            zoneObject.transform.position = center;

            var zone = zoneObject.AddComponent<ZoneView>();
            zone.SetCardPrefab(cardPrefab);
            zone.Configure(label, ZoneKind.Slots, ZoneLayout.Vertical, slotCount, PlaymatZones.FieldSlotSpacing, center);
            return zone;
        }

        ZoneView CreatePileZone(string label, Vector3 position, Quaternion? pileStackRotation = null)
        {
            var zoneObject = new GameObject(label.Replace(" ", string.Empty) + "Zone");
            zoneObject.transform.SetParent(transform, false);
            zoneObject.transform.position = position;

            var zone = zoneObject.AddComponent<ZoneView>();
            zone.SetCardPrefab(cardPrefab);
            zone.ConfigurePile(label, position, pileStackRotation);
            return zone;
        }

        DialView CreateDial(DialKind kind, Vector2 screenAnchor, Vector2 screenOffset)
        {
            var dialObject = new GameObject(kind + "Dial");
            dialObject.transform.SetParent(transform, false);

            var dial = dialObject.AddComponent<DialView>();
            dial.Configure(kind, screenAnchor, screenOffset);
            return dial;
        }

        public void SyncDeckCount(int count)
        {
            Deck?.SetPileCount(count);
        }

        void OnDeckCountChanged(int count)
        {
            SyncDeckCount(count);
        }

        public void SetRound(int round)
        {
            Round?.SetRound(round);
        }

        public void SetWillPool(int will)
        {
            Will?.SetWillPool(will);
        }

        public void SetWorth(int worth)
        {
            Worth?.SetValue(worth);
        }

        public void SetHonor(int honor)
        {
            Honor?.SetValue(honor);
        }
    }
}
