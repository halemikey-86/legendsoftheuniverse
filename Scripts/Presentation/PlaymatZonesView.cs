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
        public ZoneView OutOfPlay { get; private set; }
        public ZoneView Banished { get; private set; }
        public DialView WillRound { get; private set; }
        public DialView Worth { get; private set; }
        public DialView Honor { get; private set; }

        public static PlaymatZonesView Instance { get; private set; }

        void Awake()
        {
            Instance = this;
            ResolveCardPrefab();
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

        void BuildZones()
        {
            Field = CreateSlotZone("Field", PlaymatZones.FieldCenter, PlaymatZones.FieldSlotCount);
            RelicBond = CreateSlotZone("Relic / Bond", PlaymatZones.RelicBondCenter, PlaymatZones.RelicBondSlotCount);
            Deck = CreatePileZone("Deck", PlaymatZones.Deck);
            OutOfPlay = CreatePileZone("Out", PlaymatZones.OutOfPlay);
            Banished = CreatePileZone("Banished", PlaymatZones.Banished);

            WillRound = CreateDial(DialKind.WillRound, PlaymatZones.WillRoundTrack);
            Worth = CreateDial(DialKind.Worth, PlaymatZones.Worth);
            Honor = CreateDial(DialKind.Honor, PlaymatZones.Honor);
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

        ZoneView CreatePileZone(string label, Vector3 position)
        {
            var zoneObject = new GameObject(label.Replace(" ", string.Empty) + "Zone");
            zoneObject.transform.SetParent(transform, false);
            zoneObject.transform.position = position;

            var zone = zoneObject.AddComponent<ZoneView>();
            zone.SetCardPrefab(cardPrefab);
            zone.ConfigurePile(label, position);
            return zone;
        }

        DialView CreateDial(DialKind kind, Vector3 position)
        {
            var dialObject = new GameObject(kind + "Dial");
            dialObject.transform.SetParent(transform, false);
            dialObject.transform.position = position;

            var dial = dialObject.AddComponent<DialView>();
            dial.Configure(kind, position);
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
            WillRound?.SetRound(round);
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
