using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LegendsOfTheUniverse.Presentation
{
    /// <summary>
    /// 3D dojo visuals: wall FBX + Meshy purple mat mesh from Assets/Arenas/10thPlanetDojo/.
    /// The flat JPG playmat is hidden automatically once the mesh playmat is visible.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-260)]
    public class DojoArenaView : MonoBehaviour
    {
        public const string ArenaFolder = "Assets/Arenas/10thPlanetDojo";
        public const string WallsFolder = ArenaFolder + "/DojoWalls";
        public const string PurpleMatFolder = ArenaFolder + "/PurpleMat";

        [Header("Sources (auto-assigned in editor)")]
        [SerializeField] GameObject wallsModel;
        [SerializeField] GameObject purpleMatModel;
        [SerializeField] Texture2D purpleMatAlbedo;

        [Header("Layout")]
        [SerializeField] bool autoFitMatToPlaymat = true;
        [SerializeField] Vector3 wallsLocalPosition = Vector3.zero;
        [SerializeField] Vector3 wallsLocalEuler = Vector3.zero;
        [SerializeField] float wallsUniformScale = 1f;

        [SerializeField] Vector3 matLocalPosition = new(0f, 0.03f, 1.5f);
        [SerializeField] Vector3 matLocalEuler = new(-90f, 0f, 0f);
        [SerializeField] float matUniformScale = 1f;

        [Tooltip("When off, the flat JPG playmat stays visible even if the mesh loaded.")]
        [SerializeField] bool hideFlatPlaymatWhenMeshReady = true;

        Transform wallsRoot;
        Transform matRoot;
        bool meshPlaymatReady;

        public bool HasVisibleMeshPlaymat =>
            hideFlatPlaymatWhenMeshReady && meshPlaymatReady && matRoot != null;
        public Transform WallsRoot => wallsRoot;
        public Transform MatRoot => matRoot;

        void Awake()
        {
            BuildArena();
        }

        public void BuildArena()
        {
            EnsureAssets();
            ClearBuilt();
            meshPlaymatReady = false;

            if (wallsModel != null)
            {
                var walls = InstantiateModel(wallsModel, "DojoWalls");
                wallsRoot = walls.transform;
                wallsRoot.localPosition = wallsLocalPosition;
                wallsRoot.localRotation = Quaternion.Euler(wallsLocalEuler);
                wallsRoot.localScale = Vector3.one * wallsUniformScale;
            }

            if (purpleMatModel != null)
            {
                var mat = InstantiateModel(purpleMatModel, "PurpleMat");
                matRoot = mat.transform;
                matRoot.localPosition = matLocalPosition;
                matRoot.localRotation = Quaternion.Euler(matLocalEuler);
                matRoot.localScale = Vector3.one * matUniformScale;

                ApplyMatMaterials(mat);
                EnsureMeshVisibleFromAbove(matRoot);

                if (autoFitMatToPlaymat)
                    FitMatToPlaymat(matRoot);

                meshPlaymatReady = IsMeshPlaymatVisible(matRoot);
            }
        }

        void EnsureAssets()
        {
#if UNITY_EDITOR
            if (wallsModel == null)
                wallsModel = LoadModelInFolder(WallsFolder, "wall");
            if (purpleMatModel == null)
                purpleMatModel = LoadModelInFolder(PurpleMatFolder, "purple_mat");
            if (purpleMatModel == null)
                purpleMatModel = LoadModelInFolder(PurpleMatFolder, "floor");
            if (purpleMatAlbedo == null)
                purpleMatAlbedo = LoadTextureInFolder(PurpleMatFolder);
#endif
        }

        static void EnsureMeshVisibleFromAbove(Transform matTransform)
        {
            var bounds = GetWorldBounds(matTransform);
            if (bounds.size.sqrMagnitude < 0.0001f)
                return;

            var size = bounds.size;
            var horizontal = Mathf.Max(size.x, size.z);
            var vertical = size.y;

            // Meshy panels often import as a vertical slab — rotate flat for the top-down camera.
            if (vertical > horizontal * 1.1f)
            {
                matTransform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
                bounds = GetWorldBounds(matTransform);
                size = bounds.size;
            }

            // Still edge-on? Try the other horizontal axis.
            horizontal = Mathf.Max(size.x, size.z);
            if (Mathf.Min(size.x, size.z) < horizontal * 0.05f)
                matTransform.localRotation *= Quaternion.Euler(0f, 90f, 0f);
        }

        static bool IsMeshPlaymatVisible(Transform matTransform)
        {
            var bounds = GetWorldBounds(matTransform);
            if (bounds.size.sqrMagnitude < 0.0001f)
                return false;

            var size = bounds.size;
            var footprint = Mathf.Max(size.x, size.z);
            var thickness = Mathf.Min(size.x, size.y, size.z);
            return footprint > 1f && thickness < footprint * 0.5f;
        }

        static void FitMatToPlaymat(Transform matTransform)
        {
            var bounds = GetWorldBounds(matTransform);
            if (bounds.size.sqrMagnitude < 0.0001f)
                return;

            var size = bounds.size;
            var width = Mathf.Max(size.x, 0.001f);
            var depth = Mathf.Max(size.z, 0.001f);
            var scaleX = PlaymatZones.MatWidth / width;
            var scaleZ = PlaymatZones.MatDepth / depth;
            var uniform = Mathf.Min(scaleX, scaleZ) * matTransform.localScale.x;

            matTransform.localScale = Vector3.one * uniform;

            bounds = GetWorldBounds(matTransform);
            var target = new Vector3(PlaymatZones.MatCenter.x, matTransform.position.y, PlaymatZones.MatCenter.z);
            var delta = target - bounds.center;
            delta.y = 0f;
            matTransform.position += delta;
        }

        static Bounds GetWorldBounds(Transform root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return new Bounds(root.position, Vector3.zero);

            var bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        void ApplyMatMaterials(GameObject root)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>())
            {
                if (renderer == null)
                    continue;

                var material = renderer.material;
                if (material == null || material.shader == null || material.shader.name.Contains("InternalError"))
                    material = CreateFallbackMaterial();

                if (purpleMatAlbedo != null)
                {
                    material.mainTexture = purpleMatAlbedo;
                    if (material.HasProperty("_BaseMap"))
                        material.SetTexture("_BaseMap", purpleMatAlbedo);
                }

                if (material.HasProperty("_BaseColor"))
                    material.SetColor("_BaseColor", Color.white);
                if (material.HasProperty("_Color"))
                    material.color = Color.white;

                renderer.material = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        static Material CreateFallbackMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null || shader.name.Contains("InternalError"))
                shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null || shader.name.Contains("InternalError"))
                shader = Shader.Find("Standard");

            return new Material(shader);
        }

        GameObject InstantiateModel(GameObject source, string objectName)
        {
            var instance = Instantiate(source, transform);
            instance.name = objectName;
            instance.SetActive(true);
            DisableColliders(instance);
            return instance;
        }

        static void DisableColliders(GameObject root)
        {
            foreach (var collider in root.GetComponentsInChildren<Collider>())
                collider.enabled = false;
        }

        void ClearBuilt()
        {
            if (wallsRoot != null)
            {
                DestroyBuilt(wallsRoot.gameObject);
                wallsRoot = null;
            }

            if (matRoot != null)
            {
                DestroyBuilt(matRoot.gameObject);
                matRoot = null;
            }
        }

        void DestroyBuilt(GameObject obj)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(obj);
                return;
            }
#endif
            Destroy(obj);
        }

#if UNITY_EDITOR
        static GameObject LoadModelInFolder(string folder, string nameHint)
        {
            if (string.IsNullOrEmpty(folder))
                return null;

            GameObject fallback = null;
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { folder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (model == null)
                    continue;

                fallback ??= model;
                if (!string.IsNullOrEmpty(nameHint)
                    && path.Replace('\\', '/').ToLowerInvariant().Contains(nameHint))
                    return model;
            }

            return fallback;
        }

        static Texture2D LoadTextureInFolder(string folder)
        {
            Texture2D best = null;
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var lower = path.ToLowerInvariant();
                if (lower.Contains("normal") || lower.Contains("roughness") || lower.Contains("metallic"))
                    continue;

                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture == null)
                    continue;

                if (lower.Contains("purple_mat") || lower.EndsWith("_texture.png") || lower.EndsWith(".png"))
                    return texture;

                best ??= texture;
            }

            return best;
        }

        [ContextMenu("Rebuild Dojo Arena")]
        void RebuildContextMenu()
        {
            BuildArena();
        }

        void OnValidate()
        {
            if (!isActiveAndEnabled)
                return;

            EnsureAssets();
        }
#endif
    }
}
