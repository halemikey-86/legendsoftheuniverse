using System.Collections.Generic;
using LegendsOfTheUniverse.Presentation;
using UnityEngine;
using Willbound.Engine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Willbound.Table
{
    /// <summary>
    /// Pickup / drag / drop grammar. Sends actions to the host; never parents before accept.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DragAgent : MonoBehaviour
    {
        [SerializeField] TableBinder binder;
        [SerializeField] OfflineTableActionHost actionHost;
        [SerializeField] Camera tableCamera;
        [SerializeField] LayerMask cardLayerMask = ~0;
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
            if (tableCamera == null)
                tableCamera = Camera.main;

            zones.AddRange(FindObjectsByType<ZoneView>());
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

            if (!binder.CanPickup(tableCard.InstanceId))
                return;

            draggingCard = tableCard;
            draggingInstanceId = tableCard.InstanceId;
            draggingCard.BeginDrag();
            UpdateZoneHighlights();
        }

        void ContinueDrag()
        {
            if (draggingCard == null || tableCamera == null)
                return;

            var world = ScreenToWorldOnTable(GetPointerScreenPosition());
            draggingCard.UpdateDrag(world);
            UpdateZoneHighlights();
        }

        void EndDrag()
        {
            if (draggingCard == null)
                return;

            var accepted = false;
            string error = null;
            var dropZone = FindDropZone(draggingCard.transform.position);

            var targetId = FindTargetInstanceId();
            PlayerAction action = null;

            if (binder.IsBodyInDeclareQueue(draggingInstanceId))
            {
                if (targetId.HasValue && binder.IsLegalPressTarget(draggingInstanceId, targetId.Value))
                    action = binder.BuildDropAction(draggingInstanceId, TableZoneKind.Field, targetId);
                else if (dropZone != null && dropZone.Kind == TableZoneKind.HoldPlate
                    && binder.IsLegalDrop(draggingInstanceId, TableZoneKind.HoldPlate))
                    action = binder.BuildDropAction(draggingInstanceId, TableZoneKind.HoldPlate);
                else
                {
                    var fallback = binder.DefaultPressTargetId();
                    if (fallback.HasValue)
                        action = binder.BuildDropAction(draggingInstanceId, TableZoneKind.Field, fallback);
                }
            }
            else if (dropZone != null && binder.IsLegalDrop(draggingInstanceId, dropZone.Kind, targetId))
            {
                action = binder.BuildDropAction(draggingInstanceId, dropZone.Kind, targetId);
            }

            if (action != null)
            {
                var result = actionHost.TryApply(action);
                accepted = result.Success;
                error = result.Error;
            }

            draggingCard.EndDrag(accepted);
            ClearZoneHighlights();

            if (!accepted && !string.IsNullOrEmpty(error))
                Debug.Log($"[DragAgent] Rejected: {error}");

            draggingCard = null;
            draggingInstanceId = -1;
        }

        ZoneView FindDropZone(Vector3 worldPoint)
        {
            for (var i = 0; i < zones.Count; i++)
            {
                if (zones[i] != null && zones[i].ContainsPoint(worldPoint))
                    return zones[i];
            }

            return null;
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
            var hits = Physics.RaycastAll(ray, pickRayDistance, cardLayerMask, QueryTriggerInteraction.Collide);
            if (hits == null || hits.Length == 0)
                return false;

            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            for (var i = 0; i < hits.Length; i++)
            {
                card = CardFromHit(hits[i]);
                if (card != null && card.InstanceId > 0)
                    return true;
            }

            card = null;
            return false;
        }

        CardView CardFromHit(RaycastHit hit)
        {
            if (hit.collider == null)
                return null;

            var tableCard = hit.collider.GetComponentInParent<CardView>();
            var presentation = hit.collider.GetComponentInParent<LegendsOfTheUniverse.Presentation.CardView>();
            if (tableCard != null && tableCard.InstanceId > 0)
                return tableCard;

            if (presentation != null && presentation.EngineCardInstanceId is int id && id > 0)
            {
                var bound = tableCard != null
                    ? tableCard
                    : presentation.GetComponent<CardView>() ?? presentation.gameObject.AddComponent<CardView>();
                if (binder != null)
                    binder.RegisterCardView(id, bound);
                presentation.SetClickable(true);
                return bound;
            }

            return tableCard;
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

        int? FindTargetInstanceId()
        {
            if (tableCamera == null)
                return null;

            var ray = tableCamera.ScreenPointToRay(GetPointerScreenPosition());
            var hits = Physics.RaycastAll(ray, pickRayDistance, cardLayerMask, QueryTriggerInteraction.Collide);
            if (hits == null || hits.Length == 0)
                return null;

            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            for (var i = 0; i < hits.Length; i++)
            {
                var tableCard = CardFromHit(hits[i]);
                if (tableCard == null || tableCard.InstanceId <= 0 || tableCard.InstanceId == draggingInstanceId)
                    continue;

                return tableCard.InstanceId;
            }

            return null;
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
