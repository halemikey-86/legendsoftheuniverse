using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace LegendsOfTheUniverse.Presentation
{
    public enum LayoutManipulatorMode
    {
        Move,
        Rotate,
        Scale,
    }

    /// <summary>
    /// Click scene objects while layout mode is open to move, rotate, or scale them.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TableLayoutSceneManipulator : MonoBehaviour
    {
        const int LayoutPickLayer = 31;
        const float PickColliderPadding = 0.02f;

        [SerializeField] Transform sceneRoot;
        [SerializeField] Camera tableCamera;
        [SerializeField] float movePlaneHeight = 0.45f;

        readonly List<Collider> tempColliders = new();
        readonly List<GameObject> tempColliderHosts = new();

        Transform selected;
        LayoutManipulatorMode mode = LayoutManipulatorMode.Move;
        bool isDragging;
        Vector3 dragStartWorld;
        Vector3 dragStartPosition;
        Vector3 dragStartLocalPosition;
        Vector3 dragStartLocalEuler;
        Vector3 dragStartLocalScale;
        float rotateStartX;
        float scaleStartY;

        public bool IsEditModeActive { get; private set; }
        public Transform Selected => selected;
        public LayoutManipulatorMode Mode => mode;

        public event System.Action<Transform> SelectionChanged;
        public event System.Action TransformChanged;

        void Awake()
        {
            if (sceneRoot == null)
                sceneRoot = transform;

            if (tableCamera == null)
            {
                var tableView = GetComponent<TableView>();
                tableCamera = tableView != null ? tableView.TableCamera : Camera.main;
            }
        }

        void Update()
        {
            if (!IsEditModeActive)
                return;

            HandleModeHotkeys();

            if (IsPointerOverLayoutPanel())
                return;

            if (WasPrimaryDownThisFrame())
            {
                TrySelectAtPointer();
                BeginDrag();
            }
            else if (isDragging && IsPrimaryHeld())
                UpdateDrag();
            else if (isDragging && WasPrimaryUpThisFrame())
                EndDrag();
        }

        public void SetEditModeActive(bool active)
        {
            if (IsEditModeActive == active)
                return;

            IsEditModeActive = active;
            if (active)
                EnablePickColliders();
            else
            {
                DisablePickColliders();
                SetSelected(null);
            }
        }

        public void SetMode(LayoutManipulatorMode newMode) => mode = newMode;

        public void SetSelected(Transform target)
        {
            selected = target;
            SelectionChanged?.Invoke(selected);
        }

        public void ApplyInspectorValues(Vector3 localPosition, Vector3 localEuler, Vector3 localScale)
        {
            if (selected == null)
                return;

            selected.localPosition = localPosition;
            selected.localEulerAngles = localEuler;
            selected.localScale = localScale;
            CommitSelectedTransform();
        }

        void HandleModeHotkeys()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb == null)
                return;
            if (kb.wKey.wasPressedThisFrame)
                mode = LayoutManipulatorMode.Move;
            else if (kb.eKey.wasPressedThisFrame)
                mode = LayoutManipulatorMode.Rotate;
            else if (kb.rKey.wasPressedThisFrame)
                mode = LayoutManipulatorMode.Scale;
#else
            if (Input.GetKeyDown(KeyCode.W))
                mode = LayoutManipulatorMode.Move;
            else if (Input.GetKeyDown(KeyCode.E))
                mode = LayoutManipulatorMode.Rotate;
            else if (Input.GetKeyDown(KeyCode.R))
                mode = LayoutManipulatorMode.Scale;
#endif
        }

        void TrySelectAtPointer()
        {
            if (tableCamera == null)
                return;

            var ray = tableCamera.ScreenPointToRay(TablePointerInput.ScreenPosition);
            var hits = Physics.RaycastAll(ray, 500f, ~0, QueryTriggerInteraction.Collide);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            Transform best = null;
            var bestDepth = -1;

            for (var i = 0; i < hits.Length; i++)
            {
                var target = hits[i].collider.transform;
                if (target == null)
                    continue;

                if (IsCardBlockingHit(target))
                {
                    SetSelected(null);
                    return;
                }

                if (target.gameObject.layer != LayoutPickLayer)
                    continue;

                if (target == sceneRoot)
                    continue;

                if (!target.IsChildOf(sceneRoot))
                    continue;

                if (IsPickExcluded(target))
                    continue;

                var depth = GetDepthUnderRoot(target, sceneRoot);
                if (depth <= bestDepth)
                    continue;

                bestDepth = depth;
                best = target;
            }

            SetSelected(best);
        }

        void BeginDrag()
        {
            if (selected == null)
                return;

            isDragging = true;
            dragStartPosition = selected.position;
            dragStartLocalPosition = selected.localPosition;
            dragStartLocalEuler = selected.localEulerAngles;
            dragStartLocalScale = selected.localScale;
            rotateStartX = TablePointerInput.ScreenPosition.x;
            scaleStartY = TablePointerInput.ScreenPosition.y;

            if (TryGetPointerWorldOnPlane(GetMovePlaneY(), out dragStartWorld))
                return;

            dragStartWorld = selected.position;
        }

        void UpdateDrag()
        {
            if (selected == null)
                return;

            switch (mode)
            {
                case LayoutManipulatorMode.Move:
                {
                    if (IsShiftHeld())
                    {
                        var moveDeltaY = (TablePointerInput.ScreenPosition.y - scaleStartY) * 0.03f;
                        selected.position = dragStartPosition + new Vector3(0f, moveDeltaY, 0f);
                    }
                    else if (TryGetPointerWorldOnPlane(GetMovePlaneY(), out var world))
                    {
                        selected.position = new Vector3(world.x, selected.position.y, world.z);
                    }

                    break;
                }

                case LayoutManipulatorMode.Rotate:
                {
                    var deltaX = TablePointerInput.ScreenPosition.x - rotateStartX;
                    selected.localEulerAngles = dragStartLocalEuler + new Vector3(0f, deltaX * 0.5f, 0f);
                    break;
                }

                case LayoutManipulatorMode.Scale:
                {
                    var scaleDelta = (TablePointerInput.ScreenPosition.y - scaleStartY) * 0.01f;
                    var uniform = Mathf.Max(0.05f, dragStartLocalScale.x + scaleDelta);
                    selected.localScale = new Vector3(uniform, uniform, uniform);
                    break;
                }
            }

            TransformChanged?.Invoke();
        }

        void EndDrag()
        {
            isDragging = false;
            CommitSelectedTransform();
        }

        public void CommitSelectedTransform()
        {
            if (selected == null)
                return;

            var data = TableLayoutSettings.Active;
            TableLayoutSceneCapture.CommitTransform(sceneRoot, selected, data);
            TransformChanged?.Invoke();
        }

        float GetMovePlaneY()
        {
            if (selected != null)
                return selected.position.y;

            return movePlaneHeight;
        }

        bool TryGetPointerWorldOnPlane(float planeY, out Vector3 worldPoint)
        {
            worldPoint = default;
            if (tableCamera == null)
                return false;

            var ray = tableCamera.ScreenPointToRay(TablePointerInput.ScreenPosition);
            var plane = new Plane(Vector3.up, new Vector3(0f, planeY, 0f));
            if (!plane.Raycast(ray, out var distance))
                return false;

            worldPoint = ray.GetPoint(distance);
            return true;
        }

        void EnablePickColliders()
        {
            DisablePickColliders();
            for (var i = 0; i < sceneRoot.childCount; i++)
                AddPickCollidersRecursive(sceneRoot.GetChild(i));
        }

        void AddPickCollidersRecursive(Transform current)
        {
            if (IsPickExcluded(current))
                return;

            if (current.GetComponent<Collider>() == null)
            {
                var bounds = ComputeBounds(current);
                if (bounds.hasBounds)
                {
                    var host = current.gameObject;
                    var box = host.AddComponent<BoxCollider>();
                    box.center = bounds.center;
                    box.size = bounds.size + Vector3.one * PickColliderPadding;
                    box.isTrigger = true;
                    host.layer = LayoutPickLayer;
                    tempColliders.Add(box);
                    tempColliderHosts.Add(host);
                }
            }
            else if (current.gameObject.layer != LayoutPickLayer)
            {
                current.gameObject.layer = LayoutPickLayer;
                tempColliderHosts.Add(current.gameObject);
            }

            for (var i = 0; i < current.childCount; i++)
                AddPickCollidersRecursive(current.GetChild(i));
        }

        static (bool hasBounds, Vector3 center, Vector3 size) ComputeBounds(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                if (root.GetComponent<Collider>() != null)
                    return (true, Vector3.zero, Vector3.one * 0.5f);

                return (true, Vector3.zero, Vector3.one * 0.35f);
            }

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            var localCenter = root.InverseTransformPoint(bounds.center);
            var localSize = root.InverseTransformVector(bounds.size);
            localSize = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
            return (true, localCenter, localSize);
        }

        void DisablePickColliders()
        {
            for (var i = 0; i < tempColliders.Count; i++)
            {
                if (tempColliders[i] != null)
                    Destroy(tempColliders[i]);
            }

            tempColliders.Clear();
            tempColliderHosts.Clear();
        }

        static bool IsPointerOverLayoutPanel()
        {
            var pos = TablePointerInput.ScreenPosition;
            return pos.x <= TableLayoutTunerView.PanelPixelWidth + 12f;
        }

        static bool IsPickExcluded(Transform target)
        {
            var name = target.name;
            if (name is "HandUI" or "TableLayoutTuner" or "PlaymatZoneUi" or "BoardZoneArt" or "Playmat")
                return true;

            if (name.EndsWith("Label") && target.parent != null && target.parent.GetComponent<WorldAnchoredUi>() != null)
                return true;

            if (target.GetComponent<CardView>() != null)
                return true;

            if (target.GetComponent<Camera>() != null)
                return true;

            return false;
        }

        static bool IsCardBlockingHit(Transform target)
        {
            return target.GetComponentInParent<CardView>() != null;
        }

        static bool IsShiftHeld()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            return kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed);
#else
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#endif
        }

        static int GetDepthUnderRoot(Transform target, Transform root)
        {
            var depth = 0;
            var current = target;
            while (current != null && current != root)
            {
                depth++;
                current = current.parent;
            }

            return depth;
        }

        static bool WasPrimaryDownThisFrame() => TablePointerInput.WasPrimaryDownThisFrame();
        static bool WasPrimaryUpThisFrame() => TablePointerInput.WasPrimaryUpThisFrame();
        static bool IsPrimaryHeld() => TablePointerInput.IsPrimaryHeld();
    }
}
