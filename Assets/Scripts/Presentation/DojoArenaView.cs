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
        public const string PlaymatTexturePath = ArenaFolder + "/rUp0b.jpg";
        public const string WallsFolder = ArenaFolder + "/DojoWalls";
        public const string DarkWallFolder = ArenaFolder + "/Dark Wall";
        public const string PurpleMatFolder = ArenaFolder + "/PurpleMat";
        public const string DarkMayFolder = ArenaFolder + "/DarkMay";
        public const string TowelFolder = ArenaFolder + "/Towel and Gym bag";
        public const string BeltFolder = ArenaFolder + "/Belt and Shoes";

        [Header("Sources (auto-assigned in editor)")]
        [SerializeField] GameObject wallsModel;
        [SerializeField] GameObject purpleMatModel;
        [SerializeField] Texture2D purpleMatAlbedo;

        [Header("Props (towels, belt, etc.)")]
        [SerializeField] DojoPropPlacement[] props = System.Array.Empty<DojoPropPlacement>();

        [Header("Layout")]
        [SerializeField] bool autoFitMatToPlaymat = false;
        [SerializeField] Vector3 wallsLocalPosition = Vector3.zero;
        [SerializeField] Vector3 wallsLocalEuler = Vector3.zero;
        [SerializeField] float wallsUniformScale = 1f;

        [SerializeField] Vector3 matLocalPosition = new(0f, -1f, 0.5f);
        [SerializeField] Vector3 matLocalEuler = new(-90f, 0f, 0f);
        [SerializeField] float matUniformScale = 580f;

        [Tooltip("When off, the flat JPG playmat stays visible even if the mesh loaded.")]
        [SerializeField] bool hideFlatPlaymatWhenMeshReady = true;

        Transform wallsRoot;
        Transform matRoot;
        Transform propsRoot;
        bool meshPlaymatReady;

        public bool HasVisibleMeshPlaymat =>
            hideFlatPlaymatWhenMeshReady && meshPlaymatReady && matRoot != null;

        /// <summary>Flat photo playmat (rUp0b) — skip 3D walls/props that would cover cards.</summary>
        public bool UseFlatPhotoPlaymat => !hideFlatPlaymatWhenMeshReady;

        public bool HasBuiltGeometry =>
            wallsRoot != null || matRoot != null || propsRoot != null;

        public Transform WallsRoot => wallsRoot;
        public Transform MatRoot => matRoot;

        void Awake()
        {
            BuildArena();
        }

        public void BuildArena()
        {
            ClearBuilt();
            meshPlaymatReady = false;

            if (UseFlatPhotoPlaymat)
                return;

            EnsureAssets();

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

            if (!UseFlatPhotoPlaymat)
                BuildProps();
        }

        void BuildProps()
        {
            if (UseFlatPhotoPlaymat || props == null || props.Length == 0)
                return;

            var propsObject = new GameObject("DojoProps");
            propsObject.transform.SetParent(transform, false);
            propsRoot = propsObject.transform;

            for (var i = 0; i < props.Length; i++)
            {
                var placement = props[i];
                if (placement.model == null)
                {
                    Debug.LogWarning($"DojoArenaView: Prop slot {i} has no model assigned.");
                    continue;
                }

                var instance = InstantiateModel(placement.model, placement.model.name);
                var propTransform = instance.transform;
                propTransform.SetParent(propsRoot, false);
                propTransform.localPosition = placement.localPosition;
                propTransform.localRotation = Quaternion.Euler(placement.localEuler);
                propTransform.localScale = Vector3.one * Mathf.Max(0.01f, placement.uniformScale);

                EnsurePropLiesFlat(propTransform);
                FitPropFootprint(propTransform, placement.targetFootprint, placement.uniformScale);
                ApplyRendererMaterials(instance, placement.albedo);
            }
        }

        void EnsureAssets()
        {
            if (UseFlatPhotoPlaymat)
                return;

#if UNITY_EDITOR
            if (wallsModel == null)
                wallsModel = LoadModelInFolder(WallsFolder, "wall");
            if (wallsModel == null)
                wallsModel = LoadModelInFolder(DarkWallFolder, "wall");

            if (hideFlatPlaymatWhenMeshReady)
            {
                if (purpleMatModel == null)
                    purpleMatModel = LoadModelInFolder(DarkMayFolder, "purple_mat");
                if (purpleMatModel == null)
                    purpleMatModel = LoadModelInFolder(DarkMayFolder, "floor");
                if (purpleMatModel == null)
                    purpleMatModel = LoadModelInFolder(PurpleMatFolder, "purple_mat");
                if (purpleMatModel == null)
                    purpleMatModel = LoadModelInFolder(PurpleMatFolder, "floor");
            }

            if (purpleMatAlbedo == null)
                purpleMatAlbedo = AssetDatabase.LoadAssetAtPath<Texture2D>(PlaymatTexturePath);
            if (purpleMatAlbedo == null)
                purpleMatAlbedo = LoadTextureInFolder(DarkMayFolder);
            if (purpleMatAlbedo == null)
                purpleMatAlbedo = LoadTextureInFolder(PurpleMatFolder);

            EnsureDefaultProps();
#endif
            if (wallsModel == null || purpleMatModel == null)
                Debug.LogWarning("DojoArenaView: 3D arena models missing — assign walls/mat on Table or reimport Assets/Arenas/10thPlanetDojo.");
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

        void ApplyMatMaterials(GameObject root) => ApplyRendererMaterials(root, purpleMatAlbedo);

        void ApplyRendererMaterials(GameObject root, Texture2D albedo)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>())
            {
                if (renderer == null)
                    continue;

                var material = renderer.material;
                if (material == null || material.shader == null || material.shader.name.Contains("InternalError"))
                    material = CreateFallbackMaterial();

                if (albedo != null)
                {
                    material.mainTexture = albedo;
                    if (material.HasProperty("_BaseMap"))
                        material.SetTexture("_BaseMap", albedo);
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

        static void EnsurePropLiesFlat(Transform propTransform)
        {
            var bounds = GetWorldBounds(propTransform);
            if (bounds.size.sqrMagnitude < 0.0001f)
                return;

            var size = bounds.size;
            var vertical = size.y;
            var horizontal = Mathf.Max(size.x, size.z);
            if (vertical > horizontal * 1.15f)
                propTransform.localRotation *= Quaternion.Euler(-90f, 0f, 0f);
        }

        static void FitPropFootprint(Transform propTransform, float targetFootprint, float scaleMultiplier)
        {
            if (targetFootprint <= 0f)
                return;

            var bounds = GetWorldBounds(propTransform);
            var footprint = Mathf.Max(bounds.size.x, bounds.size.z, bounds.size.y);
            if (footprint < 0.0001f)
                return;

            var fit = targetFootprint / footprint;
            propTransform.localScale *= fit * Mathf.Max(0.01f, scaleMultiplier);
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

            if (propsRoot != null)
            {
                DestroyBuilt(propsRoot.gameObject);
                propsRoot = null;
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

        [ContextMenu("Auto Assign Dojo Props")]
        void AutoAssignPropsContextMenu()
        {
            props = BuildDefaultPropPlacements();
            EditorUtility.SetDirty(this);
        }

        void EnsureDefaultProps()
        {
            if (props != null && props.Length > 0)
                return;

            props = BuildDefaultPropPlacements();
        }

        static DojoPropPlacement[] BuildDefaultPropPlacements()
        {
            return new[]
            {
                CreateProp(TowelFolder, "towel", LoadTextureInFolder(TowelFolder),
                    new Vector3(-14.5f, 0.06f, 6.4f), Vector3.zero, 1f, 2.4f),
                CreateProp(BeltFolder, "belt", LoadTextureInFolder(BeltFolder),
                    new Vector3(-17.2f, 0.06f, 5.6f), new Vector3(0f, 35f, 0f), 1f, 1.6f),
            };
        }

        static DojoPropPlacement CreateProp(
            string folder,
            string nameHint,
            Texture2D albedo,
            Vector3 localPosition,
            Vector3 localEuler,
            float uniformScale,
            float targetFootprint)
        {
            return new DojoPropPlacement
            {
                model = LoadModelInFolder(folder, nameHint),
                albedo = albedo,
                localPosition = localPosition,
                localEuler = localEuler,
                uniformScale = uniformScale,
                targetFootprint = targetFootprint,
            };
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
