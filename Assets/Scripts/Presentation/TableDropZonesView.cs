using LegendsOfTheUniverse.Willbound.Table;
using UnityEngine;

namespace LegendsOfTheUniverse.Presentation
{
    using DropZoneView = LegendsOfTheUniverse.Willbound.Table.ZoneView;

    /// <summary>
    /// Visible drop zones on the playmat — 2×5 field grid plus engine wells.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-140)]
    public sealed class TableDropZonesView : MonoBehaviour
    {
        Transform zonesRoot;

        void Awake()
        {
            BootstrapSceneFieldZones();
            RebuildFromLayout();
        }

        public void RebuildFromLayout()
        {
            BuildZones();
        }

        static void BootstrapSceneFieldZones()
        {
            var all = FindObjectsByType<Transform>(FindObjectsSortMode.None);
            for (var i = 0; i < all.Length; i++)
            {
                var t = all[i];
                if (t == null || !IsFieldZoneName(t.name))
                    continue;

                var hostRenderer = t.GetComponent<Renderer>();
                if (hostRenderer != null)
                    hostRenderer.enabled = false;

                if (t.GetComponent<FieldDropZoneAuthoring>() == null)
                    t.gameObject.AddComponent<FieldDropZoneAuthoring>();
            }
        }

        static bool IsFieldZoneName(string objectName)
        {
            if (string.IsNullOrEmpty(objectName))
                return false;

            return objectName.StartsWith("FieldZone", System.StringComparison.OrdinalIgnoreCase)
                || objectName.StartsWith("Field Zone", System.StringComparison.OrdinalIgnoreCase);
        }

        void BuildZones()
        {
            if (zonesRoot == null)
            {
                var existing = transform.Find("DropZones");
                zonesRoot = existing != null ? existing : new GameObject("DropZones").transform;
                zonesRoot.SetParent(transform, false);
            }

            for (var i = zonesRoot.childCount - 1; i >= 0; i--)
                Destroy(zonesRoot.GetChild(i).gameObject);

            if (CountFieldDropZones() < PlaymatZones.FieldSlotCount)
                BuildFieldDropGrid();

            CreateZone("Willwell", TableZoneKind.Willwell, PlaymatZones.Willwell, new Vector3(3f, 0.04f, 3f));
            CreateZone("Stack", TableZoneKind.StackWell, PlaymatZones.StackWell, new Vector3(2.5f, 0.04f, 2.5f));
            CreateZone("Hold", TableZoneKind.HoldPlate, PlaymatZones.HoldPlate, new Vector3(2.5f, 0.04f, 2.5f));
        }

        static int CountFieldDropZones()
        {
            var zones = FindObjectsByType<DropZoneView>(FindObjectsSortMode.None);
            var count = 0;
            for (var i = 0; i < zones.Length; i++)
            {
                if (zones[i] != null && zones[i].Kind == TableZoneKind.Field)
                    count++;
            }

            return count;
        }

        void BuildFieldDropGrid()
        {
            for (var row = 0; row < PlaymatZones.FieldDropRows; row++)
            {
                for (var col = 0; col < PlaymatZones.FieldDropColumns; col++)
                {
                    var anchor = PlaymatZones.GetFieldDropSlotPosition(row, col);
                    var go = new GameObject($"FieldZone_DropZone_{row}_{col}");
                    go.transform.SetParent(zonesRoot, false);
                    go.transform.position = ZoneWorldPosition(anchor);
                    go.transform.localScale = Vector3.one;

                    var zone = go.AddComponent<DropZoneView>();
                    zone.Configure(TableZoneKind.Field, PlaymatZones.FieldDropColliderSize, "FieldZone");
                }
            }
        }

        void CreateZone(string label, TableZoneKind kind, Vector3 center, Vector3 size)
        {
            var go = new GameObject(label + "Drop");
            go.transform.SetParent(zonesRoot, false);
            go.transform.position = ZoneWorldPosition(center);

            var zone = go.AddComponent<DropZoneView>();
            zone.Configure(kind, size, label);
        }

        static Vector3 ZoneWorldPosition(Vector3 center) =>
            new Vector3(center.x, PlaymatZones.PlaymatY + 0.03f, center.z);

        public static DropZoneView FindZoneAt(Vector3 worldPoint)
        {
            DropZoneView best = null;
            var bestArea = float.MaxValue;
            var zones = FindObjectsByType<DropZoneView>(FindObjectsSortMode.None);
            for (var i = 0; i < zones.Length; i++)
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
    }
}
