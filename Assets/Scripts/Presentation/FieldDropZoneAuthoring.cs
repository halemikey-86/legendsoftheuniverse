using LegendsOfTheUniverse.Willbound.Table;
using UnityEngine;

namespace LegendsOfTheUniverse.Presentation
{
    using DropZoneView = LegendsOfTheUniverse.Willbound.Table.ZoneView;
    /// <summary>
    /// Attach to scene-authored "FieldZone" objects so drag-drop and highlights work.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-145)]
    public sealed class FieldDropZoneAuthoring : MonoBehaviour
    {
        [SerializeField] int row = -1;
        [SerializeField] int column = -1;

        void Awake()
        {
            transform.localScale = Vector3.one;

            var hostRenderer = GetComponent<Renderer>();
            if (hostRenderer != null)
                hostRenderer.enabled = false;

            var hostCollider = GetComponent<Collider>();
            if (hostCollider != null && GetComponent<DropZoneView>() == null)
                Destroy(hostCollider);

            var zone = GetComponent<DropZoneView>();
            if (zone == null)
                zone = gameObject.AddComponent<DropZoneView>();

            zone.Configure(TableZoneKind.Field, PlaymatZones.FieldDropColliderSize, gameObject.name);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (row < 0 || column < 0)
                return;

            transform.position = PlaymatZones.GetFieldDropSlotPosition(row, column);
            transform.localScale = Vector3.one;
        }
#endif
    }
}
