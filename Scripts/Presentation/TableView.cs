using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LegendsOfTheUniverse.Presentation
{
    /// <summary>
    /// TableView — camera, lighting, playmat background.
    /// GRE.cs: empty playmat background (gothic storm city); slot prefabs spawn on top later.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-250)]
    public class TableView : MonoBehaviour
    {
        const string DefaultPlaymatTexturePath = "Assets/Playmats/10th Planet.jpg";
        const string FallbackPlaymatTexturePath = "Assets/Cards/TableBackground.png";
        const string DefaultPlaymatMaterialPath = "Assets/Materials/Playmat.mat";
        const string DefaultArenaPrefabPath = "Assets/Arenas/10thPlanetDojo/10thPlanetDojo";
        static readonly Color MidnightNavy = new(0.05f, 0.08f, 0.18f);
        static readonly Quaternion TopDownRotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
        static readonly Quaternion PlaymatRotation = Quaternion.Euler(0f, 180f, 0f);

        [Header("Camera")]
        [SerializeField] Camera tableCamera;
        [SerializeField] float cameraHeight = 10f;
        [SerializeField] bool orthographic = true;
        [SerializeField] float orthographicSize = PlaymatZones.RecommendedOrthoSize;

        [Header("Arena (optional — 3D environment under the mat)")]
        [SerializeField] Transform arenaRoot;
        [SerializeField] GameObject arenaPrefab;
        [SerializeField] float playmatLiftWhenArenaPresent = 0.025f;

        [Header("Playmat")]
        [SerializeField] Transform matRoot;
        [SerializeField] Material playmatMaterial;
        [SerializeField] Texture2D playmatTexture;
        [Tooltip("Plane size in world units. X = width, Y = depth (vertical on screen).")]
        [SerializeField] Vector2 matSize = new(PlaymatZones.MatWidth, PlaymatZones.MatDepth);
        [SerializeField] Vector3 matPosition = PlaymatZones.MatCenter;

        float lastFitAspect;

#if UNITY_EDITOR
        bool validateQueued;
#endif

        void Reset()
        {
            EnsurePlaymatAssets();
        }

        DojoArenaView dojoArena;

        void Awake()
        {
            dojoArena = GetComponent<DojoArenaView>();
            EnsurePlaymatAssets();
            EnsureArenaPrefab();
            SetupCamera();
            SetupArena();
            SetupMat();
        }

        bool HideFlatPlaymatForMesh =>
            dojoArena != null && dojoArena.HasVisibleMeshPlaymat;

        void EnsureArenaPrefab()
        {
            if (arenaPrefab != null)
                return;

#if UNITY_EDITOR
            arenaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultArenaPrefabPath);
            if (arenaPrefab == null)
                arenaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{DefaultArenaPrefabPath}.prefab");
#endif
        }

        void SetupArena()
        {
            if (dojoArena != null)
            {
                dojoArena.BuildArena();
                arenaRoot = dojoArena.transform;
                return;
            }

            if (arenaRoot == null && arenaPrefab != null)
            {
                var instance = Instantiate(arenaPrefab, transform);
                instance.name = arenaPrefab.name;
                arenaRoot = instance.transform;
                arenaRoot.localPosition = Vector3.zero;
                arenaRoot.localRotation = Quaternion.identity;
            }

            if (arenaRoot != null)
                arenaRoot.SetSiblingIndex(0);
        }

        Vector3 GetMatLocalPosition()
        {
            var pos = matPosition;
            if (arenaRoot != null)
                pos.y += playmatLiftWhenArenaPresent;
            return pos;
        }

        void LateUpdate()
        {
            if (!orthographic || tableCamera == null)
                return;

            var aspect = tableCamera.aspect > 0.01f ? tableCamera.aspect : (16f / 9f);
            if (Mathf.Abs(aspect - lastFitAspect) > 0.001f)
                FitCameraToPlaymat();
        }

        void EnsurePlaymatAssets()
        {
            if (playmatTexture == null)
            {
#if UNITY_EDITOR
                playmatTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(DefaultPlaymatTexturePath);
                if (playmatTexture == null)
                    playmatTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(FallbackPlaymatTexturePath);
#else
                playmatTexture = Resources.Load<Texture2D>("TableBackground");
#endif
            }

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
            var ownsCamera = false;

            if (tableCamera == null)
                tableCamera = Camera.main;

            if (tableCamera == null)
            {
                var cameraObject = new GameObject("TableCamera");
                cameraObject.transform.SetParent(transform, false);
                tableCamera = cameraObject.AddComponent<Camera>();
                ownsCamera = true;
            }

            var bounds = PlaymatZones.GetTableContentBounds(PlaymatZones.TableFitMargin);
            var lookAt = new Vector3(bounds.CenterX, 0f, bounds.CenterZ);

            if (ownsCamera || tableCamera.transform.IsChildOf(transform))
            {
                var cameraTransform = tableCamera.transform;
                cameraTransform.SetParent(transform, false);
                var localLookAt = transform.InverseTransformPoint(lookAt);
                cameraTransform.localPosition = new Vector3(localLookAt.x, cameraHeight, localLookAt.z);
                cameraTransform.localRotation = TopDownRotation;
            }
            else
            {
                tableCamera.transform.position = lookAt + new Vector3(0f, cameraHeight, 0f);
                tableCamera.transform.rotation = TopDownRotation;
            }

            tableCamera.orthographic = orthographic;
            FitCameraToPlaymat();

            tableCamera.clearFlags = CameraClearFlags.SolidColor;
            tableCamera.backgroundColor = MidnightNavy;
            tableCamera.tag = "MainCamera";
            tableCamera.enabled = true;
            tableCamera.gameObject.SetActive(true);
            AudioListenerBootstrap.AttachToCamera(tableCamera);
        }

        void SetupMat()
        {
            EnsurePlaymatAssets();

            if (matRoot == null)
                CreatePlaymatObject();

            if (matRoot == null)
                return;

            matRoot.gameObject.SetActive(!HideFlatPlaymatForMesh);
            matRoot.localPosition = GetMatLocalPosition();
            matRoot.localRotation = PlaymatRotation;
            matRoot.localScale = GetMatScale();
            ApplyMatMaterial();
            MarkDirty();
        }

        void CreatePlaymatObject()
        {
            var matObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            matObject.name = "Playmat";
            matObject.transform.SetParent(transform, false);
            matObject.transform.localPosition = GetMatLocalPosition();
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

        void DestroyPlaymatObject()
        {
            if (matRoot == null)
                return;

            var playmat = matRoot.gameObject;
            matRoot = null;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Undo.DestroyObjectImmediate(playmat);
                MarkDirty();
                return;
            }
#endif

            Destroy(playmat);
        }

        [ContextMenu("Reset Playmat")]
        void ResetPlaymat()
        {
            DestroyPlaymatObject();
            SetupMat();
        }

        [ContextMenu("Clear Arena Instance")]
        void ClearArenaInstance()
        {
            if (arenaRoot == null)
                return;

            var arena = arenaRoot.gameObject;
            arenaRoot = null;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Undo.DestroyObjectImmediate(arena);
                MarkDirty();
                return;
            }
#endif

            Destroy(arena);
        }

        [ContextMenu("Restore Playmat Assets")]
        void RestorePlaymatAssets()
        {
            playmatMaterial = null;
            playmatTexture = null;
            EnsurePlaymatAssets();
            SetupMat();
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

        void ApplyMatMaterial()
        {
            if (matRoot == null)
                return;

            EnsurePlaymatAssets();

            var renderer = matRoot.GetComponent<Renderer>();
            if (renderer == null)
                return;

            var material = playmatMaterial != null ? new Material(playmatMaterial) : CreatePlaymatMaterial();
            ApplyTextureToMaterial(material);
            renderer.material = material;
        }

        void ApplyTextureToMaterial(Material material)
        {
            if (material == null)
                return;

            var texture = ResolvePlaymatTexture();
            if (texture == null)
                return;

            material.mainTexture = texture;
            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", Color.white);
        }

        Texture2D ResolvePlaymatTexture()
        {
            if (playmatTexture != null)
                return playmatTexture;

#if UNITY_EDITOR
            playmatTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(DefaultPlaymatTexturePath);
            if (playmatTexture == null)
                playmatTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(FallbackPlaymatTexturePath);
#endif
            return playmatTexture;
        }

        Vector3 GetMatScale()
        {
            return new Vector3(matSize.x / 10f, 1f, matSize.y / 10f);
        }

        Material CreatePlaymatMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null || shader.name == "Hidden/InternalErrorShader")
                shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null || shader.name == "Hidden/InternalErrorShader")
                shader = Shader.Find("Standard");

            var material = new Material(shader) { color = Color.white };
            ApplyTextureToMaterial(material);

            if (ResolvePlaymatTexture() == null)
                material.color = MidnightNavy;

            return material;
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
            EnsureArenaPrefab();
            SetupCamera();

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
