using LegendsOfTheUniverse.Presentation.EngineBridge;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Willbound.Table
{
    /// <summary>Carved Pass stone — visible Pass when you have priority. Space also Passes.</summary>
    [DisallowMultipleComponent]
    public sealed class PassStoneView : MonoBehaviour
    {
        [SerializeField] TableMatchBridge matchBridge;
        [SerializeField] TableBinder binder;
        [SerializeField] Collider clickCollider;

        void Awake()
        {
            if (matchBridge == null)
                matchBridge = GetComponent<TableMatchBridge>();
            if (binder == null)
                binder = GetComponent<TableBinder>();
            if (clickCollider == null)
                clickCollider = GetComponent<Collider>();
        }

        void Update()
        {
            if (WasPassKeyPressed())
                TryPass();
        }

        void OnMouseDown()
        {
            TryPass();
        }

        void TryPass()
        {
            if (matchBridge == null || !matchBridge.IsActive || binder == null)
                return;

            var snap = binder.Snapshot;
            if (snap.PriorityPlayerId != snap.LocalPlayerId)
                return;

            if (!matchBridge.TryPassPriority(out var error))
                Debug.Log($"[PassStone] {error}");
        }

        static bool WasPassKeyPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Space);
#endif
        }
    }
}
