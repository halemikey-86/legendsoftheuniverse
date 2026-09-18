using LegendsOfTheUniverse.Presentation.EngineBridge;
using LegendsOfTheUniverse.Willbound.Table;
using UnityEngine;

namespace LegendsOfTheUniverse.Presentation
{
    using DropZoneView = LegendsOfTheUniverse.Willbound.Table.ZoneView;

    [DisallowMultipleComponent]
    public sealed class CardDragDropController : MonoBehaviour
    {
        [SerializeField] TableBinder binder;
        [SerializeField] OfflineTableActionHost actionHost;
        [SerializeField] TableMatchBridge matchBridge;
        [SerializeField] HandView handView;
        [SerializeField] DropPlacementBeams placementBeams;

        public static CardDragDropController Instance { get; private set; }

        void Awake()
        {
            Instance = this;
            if (binder == null)
                binder = GetComponent<TableBinder>();
            if (actionHost == null)
                actionHost = GetComponent<OfflineTableActionHost>();
            if (matchBridge == null)
                matchBridge = GetComponent<TableMatchBridge>();
            if (handView == null)
                handView = GetComponent<HandView>();
            if (placementBeams == null)
                placementBeams = GetComponent<DropPlacementBeams>() ?? gameObject.AddComponent<DropPlacementBeams>();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void HighlightZonesForCard(CardView card)
        {
            var instanceId = GetInstanceId(card);
            if (instanceId <= 0 || binder == null)
                return;

            HighlightZonesForInstance(instanceId, card.transform.position);
        }

        public void HighlightZonesForInstance(int instanceId, Vector3 cardWorldPosition)
        {
            if (instanceId <= 0 || binder == null)
                return;

            var zones = FindObjectsByType<DropZoneView>(FindObjectsSortMode.None);
            for (var i = 0; i < zones.Length; i++)
            {
                var zone = zones[i];
                if (zone == null)
                    continue;

                var legal = binder.IsLegalDrop(instanceId, zone.Kind);
                zone.SetHighlighted(legal);
            }

            placementBeams?.Begin(instanceId, cardWorldPosition);
        }

        public void UpdatePlacementBeams(Vector3 cardWorldPosition)
        {
            placementBeams?.UpdateOrigin(cardWorldPosition);
        }

        public void ClearZoneHighlights()
        {
            var zones = FindObjectsByType<DropZoneView>(FindObjectsSortMode.None);
            for (var i = 0; i < zones.Length; i++)
                zones[i]?.ClearHighlight();

            placementBeams?.Clear();
        }

        public bool TryDropHandCard(CardView card, Vector3 worldPosition)
        {
            var instanceId = GetInstanceId(card);
            if (instanceId <= 0 || binder == null || actionHost == null || !actionHost.IsActive)
                return false;

            var zone = TableDropZonesView.FindZoneAt(worldPosition);
            if (zone == null)
                return false;

            int? targetId = null;
            if (zone.Kind == TableZoneKind.Field)
                targetId = FindTargetInstanceIdAt(worldPosition);

            if (!binder.IsLegalDrop(instanceId, zone.Kind, targetId))
                return false;

            var action = binder.BuildDropAction(instanceId, zone.Kind, targetId);
            var result = actionHost.TryApply(action);
            if (!result.Success && !string.IsNullOrEmpty(result.Error))
                Debug.Log($"[CardDragDrop] {result.Error}");

            ClearZoneHighlights();
            return result.Success;
        }

        static int GetInstanceId(CardView card)
        {
            if (card == null)
                return -1;

            var binding = card.GetComponent<TableCardBinding>();
            return binding != null ? binding.InstanceId : -1;
        }

        static int? FindTargetInstanceIdAt(Vector3 worldPoint)
        {
            var probe = new Vector3(worldPoint.x, PlaymatZones.CardY, worldPoint.z);
            var hits = Physics.OverlapSphere(probe, 1.35f, ~0, QueryTriggerInteraction.Collide);
            for (var i = 0; i < hits.Length; i++)
            {
                var binding = hits[i].GetComponentInParent<TableCardBinding>();
                if (binding != null && binding.InstanceId > 0)
                    return binding.InstanceId;
            }

            return null;
        }
    }
}
