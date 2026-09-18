using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace LegendsOfTheUniverse.Presentation
{
    /// <summary>
    /// Pointer reads compatible with Input System-only player settings.
    /// </summary>
    public static class TablePointerInput
    {
        public static Vector2 ScreenPosition
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return Mouse.current != null
                    ? Mouse.current.position.ReadValue()
                    : Vector2.zero;
#else
                return Input.mousePosition;
#endif
            }
        }

        public static bool WasPrimaryDownThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(0);
#endif
        }

        public static bool WasPrimaryUpThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
#else
            return Input.GetMouseButtonUp(0);
#endif
        }

        public static bool IsPrimaryHeld()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.leftButton.isPressed;
#else
            return Input.GetMouseButton(0);
#endif
        }
    }
}
