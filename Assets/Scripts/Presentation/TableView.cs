using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LegendsOfTheUniverse.Presentation
{
    /// <summary>
    /// TableView — camera, lighting, and solid playmat background.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-300)]
    public class TableView : MonoBehaviour
    {
        const string DefaultPlaymatMaterialPath = "Assets/Materials/Playmat.mat";

        static readonly Quaternion TopDownRotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
        static readonly Quaternion PlaymatRotation = Quaternion.Euler(0f, 180f, 0f);

        [Header("Camera")]
        [SerializeField] Camera tableCamera;
        [SerializeField] float cameraHeight = 10f;
        [SerializeField] bool orthographic = true;
        [SerializeField] float orthographicSize = PlaymatZones.RecommendedOrthoSize;

        [Header("Playmat")]
        [SerializeField] Transform matRoot;
        [SerializeField] Material playmatMaterial;
        [SerializeField] Color playmatColor = TablePresentation.MatBlack;
        [Tooltip("Plane size in world units. X = width, Y = depth (vertical on screen).")]
        [SerializeField] Vector2 matSize = new(PlaymatZones.MatWidth, PlaymatZones.MatDepth);
        [SerializeField] Vector3 matPosition = PlaymatZones.MatCenter;

        float lastFitAspect;

#if UNITY_EDITOR
        bool validateQueued;
#endif

        public Camera TableCamera => tableCamera;

        void Reset()
        {
            EnsurePlaymatAssets();
        }

        void Awake()
        {
            TablePresentation.ResetReady();
            EnsurePlaymatAssets();
            SetupCamera();
            SetupMat();
            EnsureTableLight();
            TablePresentation.MarkReady();
        }

        void EnsurePlaymatAssets()
        {
            if (playmatMaterial == null)
            {
#if UNITY_EDITOR
                playmatMaterial = AssetDatabase.LoadAssetAtPath<Material>(DefaultPlaymatMaterialPath);
#else
                playmatMaterial = Resources.Load<Material>("Playmat");
#endif
            }
        }

        void SetupCamera()
        {
            if (tableCamera == null)
                tableCamera = Camera.main;

            if (tableCamera == null)
            {
                var cameraObject = new GameObject("TableCamera");
                cameraObject.transform.SetParent(transform, false);
                tableCamera = cameraObject.AddComponent<Camera>();
            }

            var bounds = PlaymatZones.GetTableContentBounds(PlaymatZones.TableFitMargin);
            var lookAt = new Vector3(bounds.CenterX, 0f, bounds.CenterZ);
            var cameraTransform = tableCamera.transform;

            if (!cameraTransform.IsChildOf(transform))
            {
                cameraTransform.position = lookAt + new Vector3(0f, cameraHeight, 0f);
                cameraTransform.rotation = TopDownRotation;
            }
            else
            {
                var localLookAt = transform.InverseTransformPoint(lookAt);
                cameraTransform.localPosition = new Vector3(localLookAt.x, cameraHeight, localLookAt.z);
                cameraTransform.localRotation = TopDownRotation;
            }

            tableCamera.orthographic = orthographic;
            tableCamera.nearClipPlane = 0.01f;
            tableCamera.farClipPlane = 500f;
            tableCamera.clearFlags = CameraClearFlags.SolidColor;
            tableCamera.backgroundColor = playmatColor;
            tableCamera.tag = "MainCamera";
            tableCamera.enabled = true;
            tableCamera.gameObject.SetActive(true);
            tableCamera.depth = 0;

            FitCameraToPlaymat();
            AudioListenerBootstrap.AttachToCamera(tableCamera);
        }

        void EnsureTableLight()
        {
            foreach (var light in FindObjectsByType<Light>())
            {
                if (light != null && light.enabled && light.type == LightType.Directional)
                    return;
            }

            var lightObject = new GameObject("TableLight");
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            var tableLight = lightObject.AddComponent<Light>();
            tableLight.type = LightType.Directional;
            tableLight.intensity = 1.15f;
            tableLight.color = new Color(0.96f, 0.95f, 0.92f, 1f);
            tableLight.shadows = LightShadows.None;
        }

        void SetupMat()
        {
            EnsurePlaymatAssets();

            if (matRoot == null)
                CreatePlaymatObject();

            if (matRoot == null)
                return;

            matRoot.gameObject.SetActive(true);
            matRoot.localPosition = matPosition;
            matRoot.localRotation = PlaymatRotation;
            matRoot.localScale = GetMatScale();
            ApplyMatMaterial();
            matRoot.SetSiblingIndex(0);
            MarkDirty();
        }

        void CreatePlaymatObject()
        {
            var matObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            matObject.name = "Playmat";
            matObject.transform.SetParent(transform, false);
            matObject.transform.localPosition = matPosition;
            matObject.transform.localRotation = PlaymatRotation;

            var collider = matObject.GetComponent<Collider>();
            if (collider != null)
                collider.enabled = false;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Undo.RegisterCreatedObjectUndo(matObject, "Create Playmat");
                Undo.RecordObject(this, "Create Playmat");
            }
#endif

            matRoot = matObject.transform;
        }

        void ApplyMatMaterial()
        {
            if (matRoot == null)
                return;

            var renderer = matRoot.GetComponent<Renderer>();
            if (renderer == null)
                return;

            if (playmatMaterial == null)
            {
                Debug.LogWarning("TableView: Playmat material missing.");
                return;
            }

            var material = new Material(playmatMaterial);
            TablePresentation.ConfigurePlaymatMaterial(material, playmatColor);
            renderer.sharedMaterial = material;
            TablePresentation.EnsureRendererVisible(renderer);
        }

        void FitCameraToPlaymat()
        {
            if (!orthographic || tableCamera == null)
                return;

            var bounds = PlaymatZones.GetTableContentBounds(PlaymatZones.TableFitMargin);
            var aspect = tableCamera.aspect > 0.01f ? tableCamera.aspect : (16f / 9f);
            var sizeForWidth = bounds.HalfWidth / aspect;
            tableCamera.orthographicSize = Mathf.Max(orthographicSize, bounds.HalfDepth, sizeForWidth);
            lastFitAspect = aspect;
        }

        void LateUpdate()
        {
            if (!orthographic || tableCamera == null)
                return;

            var aspect = tableCamera.aspect > 0.01f ? tableCamera.aspect : (16f / 9f);
            if (Mathf.Abs(aspect - lastFitAspect) > 0.001f)
                FitCameraToPlaymat();
        }

        Vector3 GetMatScale()
        {
            return new Vector3(matSize.x / 10f, 1f, matSize.y / 10f);
        }

        void MarkDirty()
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
                return;

            EditorUtility.SetDirty(this);
#endif
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (!isActiveAndEnabled || validateQueued)
                return;

            EnsurePlaymatAssets();
            validateQueued = true;
            EditorApplication.delayCall += OnDelayedValidate;
        }

        void OnDelayedValidate()
        {
            validateQueued = false;
            if (this == null)
                return;

            SetupMat();
        }
#endif
    }
}
