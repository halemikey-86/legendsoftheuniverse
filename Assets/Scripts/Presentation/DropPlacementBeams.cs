using System.Collections.Generic;
using LegendsOfTheUniverse.Willbound.Table;
using UnityEngine;

namespace LegendsOfTheUniverse.Presentation
{
    using DropZoneView = LegendsOfTheUniverse.Willbound.Table.ZoneView;

    /// <summary>
    /// Arcing energy beams from a dragged card to each legal drop zone.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DropPlacementBeams : MonoBehaviour
    {
        const int ArcSegments = 20;

        [SerializeField] TableBinder binder;
        [SerializeField] Color beamColor = new(0.95f, 0.82f, 0.35f, 0.92f);
        [SerializeField] Color beamTipColor = new(0.55f, 0.95f, 1f, 0.75f);
        [SerializeField] float startWidth = 0.055f;
        [SerializeField] float endWidth = 0.018f;
        [SerializeField] float minArcHeight = 0.28f;
        [SerializeField] float maxArcHeight = 1.15f;
        [SerializeField] float arcHeightScale = 0.2f;

        readonly Dictionary<DropZoneView, LineRenderer> beams = new();
        Transform beamsRoot;
        Material beamMaterial;
        int activeInstanceId = -1;
        bool active;

        void Awake()
        {
            if (binder == null)
                binder = GetComponent<TableBinder>();
        }

        void Update()
        {
            if (!active)
                return;

            AnimateBeams();
        }

        public void Begin(int instanceId, Vector3 cardWorldPosition)
        {
            activeInstanceId = instanceId;
            active = instanceId > 0 && binder != null;
            if (!active)
                return;

            EnsureRoot();
            RebuildBeams(cardWorldPosition);
        }

        public void UpdateOrigin(Vector3 cardWorldPosition)
        {
            if (!active || activeInstanceId <= 0)
                return;

            RebuildBeams(cardWorldPosition);
        }

        public void Clear()
        {
            active = false;
            activeInstanceId = -1;

            foreach (var pair in beams)
            {
                if (pair.Value != null)
                    Destroy(pair.Value.gameObject);
            }

            beams.Clear();
        }

        void RebuildBeams(Vector3 cardWorldPosition)
        {
            var zones = FindObjectsByType<DropZoneView>(FindObjectsSortMode.None);
            var seen = new HashSet<DropZoneView>();

            for (var i = 0; i < zones.Length; i++)
            {
                var zone = zones[i];
                if (zone == null)
                    continue;

                seen.Add(zone);
                var legal = binder.IsLegalDrop(activeInstanceId, zone.Kind);
                if (!legal)
                {
                    RemoveBeam(zone);
                    continue;
                }

                var line = GetOrCreateBeam(zone);
                UpdateArc(line, GetBeamOrigin(cardWorldPosition), GetBeamTarget(zone));
            }

            var stale = new List<DropZoneView>();
            foreach (var pair in beams)
            {
                if (!seen.Contains(pair.Key))
                    stale.Add(pair.Key);
            }

            for (var i = 0; i < stale.Count; i++)
                RemoveBeam(stale[i]);
        }

        void AnimateBeams()
        {
            var pulse = 0.78f + (Mathf.Sin(Time.time * 8f) * 0.22f);
            foreach (var pair in beams)
            {
                var line = pair.Value;
                if (line == null)
                    continue;

                line.startWidth = startWidth * pulse;
                line.endWidth = endWidth * pulse;

                var start = beamColor;
                start.a *= pulse;
                var end = beamTipColor;
                end.a *= pulse;
                line.startColor = start;
                line.endColor = end;
            }
        }

        static Vector3 GetBeamOrigin(Vector3 cardWorldPosition) =>
            cardWorldPosition + Vector3.up * 0.08f;

        static Vector3 GetBeamTarget(DropZoneView zone)
        {
            var target = zone.transform.position;
            target.y = PlaymatZones.CardY + 0.06f;
            return target;
        }

        void UpdateArc(LineRenderer line, Vector3 from, Vector3 to)
        {
            var distance = Vector3.Distance(from, to);
            var arcHeight = Mathf.Clamp(distance * arcHeightScale, minArcHeight, maxArcHeight);
            var control = ((from + to) * 0.5f) + (Vector3.up * arcHeight);

            line.positionCount = ArcSegments + 1;
            for (var i = 0; i <= ArcSegments; i++)
            {
                var t = i / (float)ArcSegments;
                line.SetPosition(i, QuadraticBezier(from, control, to, t));
            }

            line.enabled = true;
        }

        static Vector3 QuadraticBezier(Vector3 a, Vector3 b, Vector3 c, float t)
        {
            var u = 1f - t;
            return (u * u * a) + (2f * u * t * b) + (t * t * c);
        }

        LineRenderer GetOrCreateBeam(DropZoneView zone)
        {
            if (beams.TryGetValue(zone, out var existing) && existing != null)
                return existing;

            var go = new GameObject($"DropBeam_{zone.Kind}");
            go.transform.SetParent(beamsRoot, false);

            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.numCapVertices = 4;
            line.numCornerVertices = 4;
            line.textureMode = LineTextureMode.Stretch;
            line.alignment = LineAlignment.View;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.material = EnsureMaterial();
            line.startColor = beamColor;
            line.endColor = beamTipColor;
            line.startWidth = startWidth;
            line.endWidth = endWidth;
            beams[zone] = line;
            return line;
        }

        void RemoveBeam(DropZoneView zone)
        {
            if (!beams.TryGetValue(zone, out var line))
                return;

            if (line != null)
                Destroy(line.gameObject);

            beams.Remove(zone);
        }

        void EnsureRoot()
        {
            if (beamsRoot != null)
                return;

            var existing = transform.Find("DropPlacementBeams");
            beamsRoot = existing != null ? existing : new GameObject("DropPlacementBeams").transform;
            beamsRoot.SetParent(transform, false);
        }

        Material EnsureMaterial()
        {
            if (beamMaterial != null)
                return beamMaterial;

            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");

            beamMaterial = new Material(shader);
            if (beamMaterial.HasProperty("_BaseColor"))
                beamMaterial.SetColor("_BaseColor", beamColor);
            return beamMaterial;
        }
    }
}
