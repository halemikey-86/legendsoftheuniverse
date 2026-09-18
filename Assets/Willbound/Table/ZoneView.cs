using UnityEngine;

namespace LegendsOfTheUniverse.Willbound.Table
{
    /// <summary>
    /// Drop collider on the mat. Highlights when a legal drag hovers.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ZoneView : MonoBehaviour
    {
        const int IgnoreRaycastLayer = 2;

        [SerializeField] TableZoneKind zoneKind = TableZoneKind.Field;
        [SerializeField] BoxCollider dropCollider;
        [SerializeField] Renderer highlightRenderer;
        [SerializeField] Color legalColor = new(0.85f, 0.72f, 0.28f, 0.55f);
        [SerializeField] bool showHighlightVisual = true;

        public TableZoneKind Kind => zoneKind;
        public float DropArea => dropCollider != null
            ? dropCollider.size.x * dropCollider.size.z
            : 0f;

        public void Configure(TableZoneKind kind, Vector3 colliderSize, string label = null)
        {
            zoneKind = kind;
            EnsureCollider();
            if (dropCollider != null)
                dropCollider.size = colliderSize;
            SyncVisualFootprint();
            if (!string.IsNullOrEmpty(label))
                gameObject.name = label + "Zone";
        }

        void EnsureVisual()
        {
            if (highlightRenderer != null)
                return;

            var visual = GameObject.CreatePrimitive(PrimitiveType.Quad);
            visual.name = "ZoneVisual";
            visual.transform.SetParent(transform, false);
            visual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            visual.transform.localPosition = Vector3.zero;
            visual.layer = IgnoreRaycastLayer;

            var collider = visual.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            highlightRenderer = visual.GetComponent<Renderer>();
            if (highlightRenderer != null)
            {
                highlightRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                highlightRenderer.receiveShadows = false;
                highlightRenderer.enabled = false;

                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                highlightRenderer.sharedMaterial = new Material(shader);
                if (highlightRenderer.material.HasProperty("_BaseColor"))
                    highlightRenderer.material.SetColor("_BaseColor", legalColor);
            }
        }

        void SyncVisualFootprint()
        {
            EnsureVisual();
            if (highlightRenderer == null || dropCollider == null)
                return;

            var footprint = GetFootprintSize();
            highlightRenderer.transform.localScale = new Vector3(footprint.x, footprint.y, 1f);
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
            gameObject.layer = IgnoreRaycastLayer;
        }

        public bool ContainsPoint(Vector3 worldPoint)
        {
            if (dropCollider == null)
                return false;

            var bounds = dropCollider.bounds;
            return worldPoint.x >= bounds.min.x && worldPoint.x <= bounds.max.x
                && worldPoint.z >= bounds.min.z && worldPoint.z <= bounds.max.z;
        }

        public void SetHighlighted(bool legal)
        {
            if (highlightRenderer == null)
                return;

            highlightRenderer.enabled = showHighlightVisual && legal;
        }

        public void ClearHighlight()
        {
            if (highlightRenderer != null)
                highlightRenderer.enabled = false;
        }

        Vector2 GetFootprintSize()
        {
            if (dropCollider == null)
                return new Vector2(4f, 4f);

            return new Vector2(dropCollider.size.x, dropCollider.size.z);
        }
    }
}
