using System.Collections.Generic;
using UnityEngine;
using Willbound.Engine;

namespace Willbound.Table
{
    /// <summary>
    /// Press targeting lines — attacker to target until Clash locks or Press skipped.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ClashLines : MonoBehaviour
    {
        [SerializeField] TableBinder binder;
        [SerializeField] Color lineColor = new(0.85f, 0.72f, 0.28f, 0.85f);
        [SerializeField] float lineWidth = 0.04f;
        [SerializeField] float lineHeight = 0.08f;

        readonly Dictionary<int, LineRenderer> activeLines = new();
        Material lineMaterial;

        void Awake()
        {
            if (binder == null)
                binder = GetComponent<TableBinder>();
        }

        public void OnEngineEvent(GameEvent e)
        {
            if (e == null)
                return;

            switch (e.Kind)
            {
                case EventKind.PressDeclared:
                    if (TryInt(e, "source", out var source) && TryInt(e, "target", out var target))
                        ShowLine(source, target);
                    break;
                case EventKind.ClashLocked:
                    ClearAll();
                    break;
                case EventKind.HoldDeclared:
                    if (TryInt(e, "source", out var holdSource))
                        RemoveLine(holdSource);
                    break;
            }
        }

        void ShowLine(int sourceId, int targetId)
        {
            if (binder == null)
                return;

            if (!binder.TryGetCardView(sourceId, out var sourceView)
                || !binder.TryGetCardView(targetId, out var targetView))
                return;

            var line = GetOrCreateLine(sourceId);
            UpdateLine(line, sourceView.transform.position, targetView.transform.position);
        }

        void UpdateLine(LineRenderer line, Vector3 from, Vector3 to)
        {
            var a = from + Vector3.up * lineHeight;
            var b = to + Vector3.up * lineHeight;
            line.positionCount = 2;
            line.SetPosition(0, a);
            line.SetPosition(1, b);
            line.enabled = true;
        }

        LineRenderer GetOrCreateLine(int sourceId)
        {
            if (activeLines.TryGetValue(sourceId, out var existing))
                return existing;

            var go = new GameObject($"PressLine_{sourceId}");
            go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;
            line.material = EnsureMaterial();
            line.startColor = lineColor;
            line.endColor = lineColor;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            activeLines[sourceId] = line;
            return line;
        }

        Material EnsureMaterial()
        {
            if (lineMaterial != null)
                return lineMaterial;

            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");

            lineMaterial = new Material(shader);
            if (lineMaterial.HasProperty("_BaseColor"))
                lineMaterial.SetColor("_BaseColor", lineColor);
            return lineMaterial;
        }

        void RemoveLine(int sourceId)
        {
            if (!activeLines.TryGetValue(sourceId, out var line))
                return;

            if (line != null)
                Destroy(line.gameObject);
            activeLines.Remove(sourceId);
        }

        public void ClearAll()
        {
            foreach (var pair in activeLines)
            {
                if (pair.Value != null)
                    Destroy(pair.Value.gameObject);
            }

            activeLines.Clear();
        }

        static bool TryInt(GameEvent e, string key, out int value)
        {
            value = 0;
            if (e?.Data == null || !e.Data.TryGetValue(key, out var raw) || raw == null)
                return false;

            try
            {
                value = System.Convert.ToInt32(raw);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
