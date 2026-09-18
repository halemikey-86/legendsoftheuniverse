using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace LegendsOfTheUniverse.Presentation
{
    /// <summary>
    /// Drag screen-space number overlays while layout mode is active.
    /// </summary>
    public static class TableLayoutOverlayEditor
    {
        const float ScreenToWorld = 0.028f;

        public static void SetAllPickable(bool pickable)
        {
            var overlays = Object.FindObjectsByType<WorldAnchoredUi>(FindObjectsSortMode.None);
            for (var i = 0; i < overlays.Length; i++)
            {
                if (overlays[i] != null)
                    overlays[i].SetLayoutPickable(pickable);
            }
        }

        public static bool TryPickAtPointer(out WorldAnchoredUi overlay)
        {
            overlay = null;
            var eventSystem = EventSystem.current;
            if (eventSystem == null)
                return false;

            var pointerData = new PointerEventData(eventSystem)
            {
                position = TablePointerInput.ScreenPosition,
            };

            var results = new System.Collections.Generic.List<RaycastResult>();
            eventSystem.RaycastAll(pointerData, results);

            for (var i = 0; i < results.Count; i++)
            {
                var ui = results[i].gameObject.GetComponentInParent<WorldAnchoredUi>();
                if (ui != null && ui.LayoutPickable)
                {
                    overlay = ui;
                    return true;
                }
            }

            return false;
        }

        public static Vector3 ScreenDeltaToWorldOffset(Vector2 delta) =>
            new(delta.x * ScreenToWorld, delta.y * ScreenToWorld, 0f);
    }
}
