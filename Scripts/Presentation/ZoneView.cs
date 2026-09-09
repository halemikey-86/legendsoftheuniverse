using System.Collections.Generic;
using UnityEngine;

namespace LegendsOfTheUniverse.Presentation
{
    public enum ZoneKind
    {
        Slots,
        Pile,
    }

    public enum ZoneLayout
    {
        Horizontal,
        Vertical,
    }

    /// <summary>
    /// Playmat zone — slot row (Field, Relic/Bond) or card pile (Deck, Out, Banished).
    /// </summary>
    [DisallowMultipleComponent]
    public class ZoneView : MonoBehaviour
    {
        static readonly Color MarkerFill = new(0.28f, 0.16f, 0.42f, 0.22f);

        [Header("Zone")]
        [SerializeField] string zoneLabel = "Zone";
        [SerializeField] ZoneKind zoneKind = ZoneKind.Slots;
        [SerializeField] ZoneLayout layout = ZoneLayout.Vertical;
        [SerializeField] int slotCount = 1;
        [SerializeField] float slotSpacing = PlaymatZones.FieldSlotSpacing;
        [SerializeField] Vector2 markerSize = new(0.65f, 0.9f);

        [Header("Cards")]
        [SerializeField] CardView cardPrefab;
        [SerializeField] float cardScale = PlaymatZones.PileCardScale;
        [SerializeField] int maxVisiblePileLayers = 5;
        [SerializeField] float pileLayerHeight = 0.004f;
        [SerializeField] float pileLayerDepth = 0.04f;

        [Header("Label")]
        [SerializeField] bool showMarkers;
        [SerializeField] Vector3 labelWorldOffset = new(0f, 0f, -0.85f);
        [SerializeField] Vector2 labelPanelSize = new(110f, 34f);

        Transform markersRoot;
        Transform cardsRoot;
        WorldAnchoredUi labelUi;
        readonly List<CardView> slotCards = new();
        readonly List<CardView> pileCards = new();
        readonly List<Transform> slotMarkers = new();
        int pileCount;

        public string ZoneLabel => zoneLabel;
        public int PileCount => pileCount;
        public int SlotCount => slotCount;
        public IReadOnlyList<CardView> SlotCards => slotCards;

        public void Configure(string label, ZoneKind kind, ZoneLayout zoneLayout, int slots, float spacing, Vector3 worldPosition)
        {
            zoneLabel = label;
            zoneKind = kind;
            layout = zoneLayout;
            slotCount = Mathf.Max(1, slots);
            transform.position = worldPosition;
            slotSpacing = spacing;
            BuildZone();
        }

        public void ConfigurePile(string label, Vector3 worldPosition)
        {
            zoneLabel = label;
            zoneKind = ZoneKind.Pile;
            slotCount = 1;
            transform.position = worldPosition;
            BuildZone();
        }

        public void SetCardPrefab(CardView prefab)
        {
            cardPrefab = prefab;
        }

        void Awake()
        {
            BuildZone();
        }

        void BuildZone()
        {
            EnsureRoots();
            RebuildMarkers();
            EnsureLabel();
            labelUi.Label = zoneLabel;
            UpdateLabelValue();
        }

        void EnsureRoots()
        {
            if (markersRoot == null)
            {
                var markersObject = new GameObject("Markers");
                markersObject.transform.SetParent(transform, false);
                markersRoot = markersObject.transform;
            }

            if (cardsRoot == null)
            {
                var cardsObject = new GameObject("Cards");
                cardsObject.transform.SetParent(transform, false);
                cardsRoot = cardsObject.transform;
            }
        }

        void EnsureLabel()
        {
            if (labelUi != null)
                return;

            labelUi = WorldAnchoredUi.Create(transform, zoneLabel, labelWorldOffset, labelPanelSize, 14, true);
        }

        void RebuildMarkers()
        {
            EnsureRoots();

            for (var i = slotMarkers.Count - 1; i >= 0; i--)
            {
                if (slotMarkers[i] != null)
                    Destroy(slotMarkers[i].gameObject);
            }

            slotMarkers.Clear();

            if (!showMarkers)
                return;

            if (zoneKind == ZoneKind.Pile)
            {
                CreateMarker(Vector3.zero, 0);
                return;
            }

            for (var i = 0; i < slotCount; i++)
                CreateMarker(GetSlotLocalPosition(i), i);
        }

        void CreateMarker(Vector3 localPosition, int index)
        {
            var markerObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            markerObject.name = $"Slot_{index}";
            markerObject.transform.SetParent(markersRoot, false);
            markerObject.transform.localPosition = localPosition + new Vector3(0f, 0.001f, 0f);
            markerObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            markerObject.transform.localScale = new Vector3(markerSize.x, markerSize.y, 1f);

            var collider = markerObject.GetComponent<Collider>();
            if (collider != null)
                collider.enabled = false;

            var renderer = markerObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                var material = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
                material.color = MarkerFill;
                renderer.sharedMaterial = material;
            }

            slotMarkers.Add(markerObject.transform);
        }

        Vector3 GetSlotLocalPosition(int index)
        {
            var axisOffset = (index - ((slotCount - 1) * 0.5f)) * slotSpacing;
            return layout == ZoneLayout.Horizontal
                ? new Vector3(axisOffset, 0f, 0f)
                : new Vector3(0f, 0f, axisOffset);
        }

        public Vector3 GetSlotWorldPosition(int index)
        {
            index = Mathf.Clamp(index, 0, slotCount - 1);
            return transform.position + GetSlotLocalPosition(index);
        }

        public int GetFirstEmptySlot()
        {
            for (var i = 0; i < slotCount; i++)
            {
                if (i >= slotCards.Count || slotCards[i] == null)
                    return i;
            }

            return -1;
        }

        public CardView PlaceInSlot(int slotIndex, Texture2D front, bool faceUp = true)
        {
            if (cardPrefab == null || front == null || slotIndex < 0 || slotIndex >= slotCount)
                return null;

            EnsureSlotListSize();
            ClearSlot(slotIndex);

            var worldPosition = GetSlotWorldPosition(slotIndex);
            var card = Instantiate(cardPrefab, worldPosition, CardView.TableRotation, cardsRoot);
            card.SetFrontTexture(front);
            card.SetFaceUpImmediate(faceUp);
            card.SetCardScale(cardScale);
            card.SetClickable(false);
            slotCards[slotIndex] = card;
            return card;
        }

        public void ClearSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= slotCards.Count)
                return;

            if (slotCards[slotIndex] != null)
                Destroy(slotCards[slotIndex].gameObject);

            slotCards[slotIndex] = null;
        }

        public void ClearAllSlots()
        {
            for (var i = 0; i < slotCards.Count; i++)
                ClearSlot(i);
        }

        public void AddToPile(int amount = 1)
        {
            pileCount += amount;
            RebuildPileVisual();
            UpdateLabelValue();
        }

        public void SetPileCount(int count)
        {
            pileCount = Mathf.Max(0, count);
            RebuildPileVisual();
            UpdateLabelValue();
        }

        public void RebuildPileVisual()
        {
            EnsureRoots();

            for (var i = pileCards.Count - 1; i >= 0; i--)
            {
                if (pileCards[i] != null)
                    Destroy(pileCards[i].gameObject);
            }

            pileCards.Clear();

            if (cardPrefab == null || pileCount <= 0)
                return;

            var layers = Mathf.Min(pileCount, maxVisiblePileLayers);
            for (var i = 0; i < layers; i++)
            {
                var card = Instantiate(cardPrefab, cardsRoot);
                card.transform.localPosition = new Vector3(0f, i * pileLayerHeight, i * pileLayerDepth);
                card.transform.localRotation = CardView.TableRotation;
                card.SetFaceUpImmediate(false);
                card.SetCardScale(cardScale);
                card.SetClickable(false);
                pileCards.Add(card);
            }
        }

        void EnsureSlotListSize()
        {
            while (slotCards.Count < slotCount)
                slotCards.Add(null);
        }

        void UpdateLabelValue()
        {
            if (labelUi == null)
                return;

            if (zoneKind == ZoneKind.Slots)
            {
                var occupied = 0;
                for (var i = 0; i < slotCards.Count; i++)
                {
                    if (slotCards[i] != null)
                        occupied++;
                }

                labelUi.Value = $"{occupied}/{slotCount}";
                return;
            }

            labelUi.Value = pileCount.ToString();
        }
    }
}
