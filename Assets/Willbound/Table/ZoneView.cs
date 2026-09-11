using UnityEngine;

namespace Willbound.Table
{
    /// <summary>
    /// Drop collider on the mat. Highlights when a legal drag hovers.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneView : MonoBehaviour
    {
        [SerializeField] TableZoneKind zoneKind = TableZoneKind.Field;
        [SerializeField] BoxCollider dropCollider;
        [SerializeField] Renderer highlightRenderer;
        [SerializeField] Color idleColor = new(0.12f, 0.12f, 0.14f, 0.15f);
        [SerializeField] Color legalColor = new(0.85f, 0.72f, 0.28f, 0.35f);

        public TableZoneKind Kind => zoneKind;

        public void Configure(TableZoneKind kind, Vector3 colliderSize)
        {
            zoneKind = kind;
            EnsureCollider();
            if (dropCollider != null)
                dropCollider.size = colliderSize;
        }

        void Awake()
        {
            EnsureCollider();
        }

        void EnsureCollider()
        {
            if (dropCollider != null)
                return;

            dropCollider = GetComponent<BoxCollider>();
            if (dropCollider == null)
            {
                dropCollider = gameObject.AddComponent<BoxCollider>();
                dropCollider.size = new Vector3(4f, 0.2f, 4f);
                dropCollider.center = Vector3.zero;
            }

            dropCollider.isTrigger = true;
        }

        public bool ContainsPoint(Vector3 worldPoint)
        {
            if (dropCollider == null)
                return false;

            return dropCollider.bounds.Contains(worldPoint);
        }

        public void SetHighlighted(bool legal)
        {
            if (highlightRenderer == null)
                return;

            var block = highlightRenderer.material;
            if (block.HasProperty("_BaseColor"))
                block.SetColor("_BaseColor", legal ? legalColor : idleColor);
        }

        public void ClearHighlight() => SetHighlighted(false);
    }
}
