using System.Collections.Generic;
using LegendsOfTheUniverse.Presentation;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace LegendsOfTheUniverse.Willbound.Table
{
    /// <summary>
    /// Pickup / drag / drop grammar. Sends actions to the host; never parents before accept.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DragAgent : MonoBehaviour
    {
        [SerializeField] TableBinder binder;
        [SerializeField] OfflineTableActionHost actionHost;
        [SerializeField] HandView handView;
        [SerializeField] Camera tableCamera;
        [SerializeField] LayerMask cardLayerMask = ~(1 << 2);
        [SerializeField] float pickRayDistance = 100f;

        CardView draggingCard;
        int draggingInstanceId = -1;
        readonly List<ZoneView> zones = new();

        public bool IsDragging => draggingCard != null;

        void Awake()
        {
            if (binder == null)
                binder = GetComponent<TableBinder>();
            if (actionHost == null)
                actionHost = GetComponent<OfflineTableActionHost>();
            if (handView == null)
                handView = GetComponent<HandView>();
            if (tableCamera == null)
                tableCamera = Camera.main;

            zones.AddRange(FindObjectsByType<ZoneView>(FindObjectsSortMode.None));
        }

        void Update()
        {
            if (WasPrimaryDownThisFrame())
                TryBeginDrag();

            if (draggingCard != null)
            {
                if (IsPrimaryHeld())
                    ContinueDrag();
                else
                    EndDrag();
            }
        }

        void TryBeginDrag()
        {
            if (binder == null || actionHost == null || !actionHost.IsActive)
                return;

            if (!TryRaycastTableCard(out var tableCard))
                return;

            if (tableCard.InstanceId <= 0)
                return;

            if (handView != null && tableCard.Presentation != null && handView.ContainsHandCard(tableCard.Presentation))
                return;

            if (!binder.CanPickup(tableCard.InstanceId))
                return;

            draggingCard = tableCard;
            draggingInstanceId = tableCard.InstanceId;
            draggingCard.BeginDrag();
            UpdateZoneHighlights();
            CardDragDropController.Instance?.HighlightZonesForInstance(
                draggingInstanceId,
                draggingCard.transform.position);
        }

        void ContinueDrag()
        {
            if (draggingCard == null || tableCamera == null)
                return;

            var world = ScreenToWorldOnTable(GetPointerScreenPosition());
            draggingCard.UpdateDrag(world);
            UpdateZoneHighlights();
            CardDragDropController.Instance?.UpdatePlacementBeams(draggingCard.transform.position);
        }

        void EndDrag()
        {
            if (draggingCard == null)
                return;

            var accepted = false;
            string error = null;
            var dropPoint = ScreenToWorldOnTable(GetPointerScreenPosition());
            var dropZone = FindDropZone(dropPoint);

            int? targetId = null;
            if (dropZone != null && dropZone.Kind == TableZoneKind.Field)
                targetId = FindTargetInstanceId(dropPoint);

            if (dropZone != null && binder.IsLegalDrop(draggingInstanceId, dropZone.Kind, targetId))
            {
                var action = binder.BuildDropAction(draggingInstanceId, dropZone.Kind, targetId);
                var result = actionHost.TryApply(action);
                accepted = result.Success;
                error = result.Error;
            }

            draggingCard.EndDrag(accepted);
            ClearZoneHighlights();
            CardDragDropController.Instance?.ClearZoneHighlights();

            if (!accepted && !string.IsNullOrEmpty(error))
                Debug.Log($"[DragAgent] Rejected: {error}");

            draggingCard = null;
            draggingInstanceId = -1;
        }

        ZoneView FindDropZone(Vector3 worldPoint)
        {
            ZoneView best = null;
            var bestArea = float.MaxValue;
            for (var i = 0; i < zones.Count; i++)
            {
                var zone = zones[i];
                if (zone == null || !zone.ContainsPoint(worldPoint))
                    continue;

                var area = zone.DropArea;
                if (area >= bestArea)
                    continue;

                bestArea = area;
                best = zone;
            }

            return best;
        }

        void UpdateZoneHighlights()
        {
            for (var i = 0; i < zones.Count; i++)
            {
                var zone = zones[i];
                if (zone == null)
                    continue;

                var legal = binder != null
                    && draggingInstanceId > 0
                    && binder.IsLegalDrop(draggingInstanceId, zone.Kind);
                zone.SetHighlighted(legal);
            }
        }

        void ClearZoneHighlights()
        {
            for (var i = 0; i < zones.Count; i++)
                zones[i]?.ClearHighlight();
        }

        bool TryRaycastTableCard(out CardView card)
        {
            card = null;
            if (tableCamera == null)
                return false;

            var ray = tableCamera.ScreenPointToRay(GetPointerScreenPosition());
            if (!Physics.Raycast(ray, out var hit, pickRayDistance, cardLayerMask, QueryTriggerInteraction.Collide))
                return false;

            card = hit.collider.GetComponentInParent<CardView>();
            if (card == null)
            {
                var presentation = hit.collider.GetComponentInParent<LegendsOfTheUniverse.Presentation.CardView>();
                if (presentation != null)
                    card = presentation.GetComponent<CardView>();
            }

            return card != null;
        }

        Vector3 ScreenToWorldOnTable(Vector2 screen)
        {
            var ray = tableCamera.ScreenPointToRay(screen);
            var plane = new Plane(Vector3.up, new Vector3(0f, PlaymatZones.CardY, 0f));
            return plane.Raycast(ray, out var enter) ? ray.GetPoint(enter) : transform.position;
        }

        static Vector2 GetPointerScreenPosition()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
                return Mouse.current.position.ReadValue();
#endif
            return Input.mousePosition;
        }

        static bool WasPrimaryDownThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(0);
#endif
        }

        int? FindTargetInstanceId(Vector3 worldPoint)
        {
            if (tableCamera == null)
                return null;

            var ray = tableCamera.ScreenPointToRay(GetPointerScreenPosition());
            if (!Physics.Raycast(ray, out var hit, pickRayDistance, cardLayerMask, QueryTriggerInteraction.Collide))
                return null;

            var tableCard = hit.collider.GetComponentInParent<CardView>();
            return tableCard != null && tableCard.InstanceId > 0 ? tableCard.InstanceId : null;
        }

        static bool IsPrimaryHeld()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.leftButton.isPressed;
#else
            return Input.GetMouseButton(0);
#endif
        }
    }
}
