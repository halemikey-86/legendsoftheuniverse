using UnityEngine;

namespace LegendsOfTheUniverse.Presentation
{
    /// <summary>
    /// Renders Deck, Supply, Store, and Icon zone art on the playmat surface.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-290)]
    public sealed class BoardZoneArtView : MonoBehaviour
    {
        const int IgnoreRaycastLayer = 2;
        const int ArtRenderQueue = 1500;

        [Header("Overlay")]
        [SerializeField] bool showSeparateZoneArt;

        [Header("Textures")]
        [SerializeField] Texture2D storeArt;
        [SerializeField] Texture2D deckArt;
        [SerializeField] Texture2D supplyArt;
        [SerializeField] Texture2D iconZoneArt;

        [Header("Layout")]
        [SerializeField] float storeWorldWidth = 22f;
        [SerializeField] float deckWorldWidth = 4.1f;
        [SerializeField] float supplyWorldWidth = 4.1f;
        [SerializeField] float iconZoneWorldWidth = 5.2f;
        [SerializeField] float matSurfaceY = 0.015f;

        Transform artRoot;

        void Awake()
        {
            ResolveTextures();
            BuildZoneArt();
        }

#if UNITY_EDITOR
        void Reset()
        {
            ResolveTextures();
        }
#endif

        void ResolveTextures()
        {
            if (storeArt == null)
                storeArt = BoardZoneArt.LoadStore();
            if (deckArt == null)
                deckArt = BoardZoneArt.LoadDeck();
            if (supplyArt == null)
                supplyArt = BoardZoneArt.LoadSupply();
            if (iconZoneArt == null)
                iconZoneArt = BoardZoneArt.LoadIconZone();
        }

        void BuildZoneArt()
        {
            if (!showSeparateZoneArt)
                return;

            if (artRoot == null)
            {
                var existing = transform.Find("BoardZoneArt");
                artRoot = existing != null ? existing : new GameObject("BoardZoneArt").transform;
                artRoot.SetParent(transform, false);
            }

            for (var i = artRoot.childCount - 1; i >= 0; i--)
                Destroy(artRoot.GetChild(i).gameObject);

            CreateZoneQuad("StoreArt", storeArt, PlaymatZones.StoreRowCenter, storeWorldWidth);
            CreateZoneQuad("DeckArt", deckArt, PlaymatZones.Deck, deckWorldWidth);
            CreateZoneQuad("SupplyArt", supplyArt, PlaymatZones.Supply, supplyWorldWidth);
            CreateZoneQuad("IconZoneArt", iconZoneArt, PlaymatZones.Icon, iconZoneWorldWidth);
        }

        void CreateZoneQuad(string name, Texture2D texture, Vector3 anchor, float worldWidth)
        {
            if (texture == null)
                return;

            var quadObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quadObject.name = name;
            quadObject.layer = IgnoreRaycastLayer;
            quadObject.transform.SetParent(artRoot, false);

            var collider = quadObject.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);

            var aspect = BoardZoneArt.GetAspect(texture);
            var worldHeight = worldWidth / aspect;
            quadObject.transform.position = new Vector3(anchor.x, matSurfaceY, anchor.z);
            quadObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            quadObject.transform.localScale = new Vector3(worldWidth, worldHeight, 1f);

            var renderer = quadObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = CreateArtMaterial(texture);
                TablePresentation.EnsureRendererVisible(renderer);
            }
        }

        static Material CreateArtMaterial(Texture2D texture)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Texture");

            var material = new Material(shader);
            material.mainTexture = texture;
            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", Color.white);
            if (material.HasProperty("_Cull"))
                material.SetFloat("_Cull", 0f);

            material.renderQueue = ArtRenderQueue;
            return material;
        }
    }
}
