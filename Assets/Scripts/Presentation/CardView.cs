using LegendsOfTheUniverse.Presentation.EngineBridge;
using UnityEngine;
using Willbound.Engine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LegendsOfTheUniverse.Presentation
{
    [DisallowMultipleComponent]
    public class CardView : MonoBehaviour
    {
        const string DefaultBackTexturePath = "Assets/Cards/CardBacks/CardBack_OriginalDesign.png";
        const string DefaultBackMaterialPath = "Assets/Materials/CardBack.mat";
        const string DefaultFaceMaterialPath = "Assets/Materials/CardFace.mat";
        const float PlayableGlowSpread = 0.32f;
        const float PlayableGlowPulseSpeed = 2.6f;

        public static readonly Quaternion TableRotation = Quaternion.identity;
        public static readonly Quaternion PileRootRotation = Quaternion.Euler(0f, 180f, 0f);
        public static readonly Quaternion StackRotation = PileRootRotation;
        static readonly Quaternion FrontFaceRotation = Quaternion.Euler(0f, 180f, 0f);
        static readonly Quaternion BackFaceRotation = Quaternion.Euler(0f, 180f, 0f);
        static readonly Quaternion FaceUpShellRotation = Quaternion.identity;
        static readonly Quaternion FaceDownShellRotation = Quaternion.Euler(180f, 0f, 0f);

        [Header("Shell")]
        [SerializeField] Transform shell;
        [SerializeField] Renderer frontRenderer;
        [SerializeField] Renderer backRenderer;

        [Header("Assets")]
        [SerializeField] Material cardBackMaterial;
        [SerializeField] Material cardFaceMaterial;
        [SerializeField] Texture2D cardBackTexture;
        [SerializeField] Texture2D frontTexture;

        [Header("Size")]
        [SerializeField] float cardScale = PlaymatZones.CardScale;
        [SerializeField] bool faceUp;

        Material frontMaterialInstance;
        Material backMaterialInstance;
        Material playableOutlineMaterial;
        BoxCollider clickCollider;
        Transform playableOutline;
        bool playableOutlineEnabled;

        public bool IsFaceUp => faceUp;
        public float CardScale => cardScale;
        public Texture2D BackTexture => cardBackTexture;

        void Reset()
        {
            EnsureCardAssets();
            EnsureShell();
            ApplyTableOrientation();
        }

        void Awake()
        {
            EnsureCardAssets();
            EnsureShell();
            ApplyTableOrientation();
            ApplySessionBack();
            ApplyMaterials();
            SetFaceUpImmediate(faceUp);
        }

        void ApplySessionBack()
        {
            if (CardDeck.SelectedCardBack != null)
                cardBackTexture = CardDeck.SelectedCardBack;
        }

        void EnsureCardAssets()
        {
            if (cardBackTexture == null)
            {
#if UNITY_EDITOR
                cardBackTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(DefaultBackTexturePath);
#endif
            }

            if (cardBackMaterial == null)
            {
#if UNITY_EDITOR
                cardBackMaterial = AssetDatabase.LoadAssetAtPath<Material>(DefaultBackMaterialPath);
#endif
            }

            if (cardFaceMaterial == null)
            {
#if UNITY_EDITOR
                cardFaceMaterial = AssetDatabase.LoadAssetAtPath<Material>(DefaultFaceMaterialPath);
#endif
            }
        }

        public void ApplyTableOrientation()
        {
            transform.localRotation = TableRotation;
        }

        public void ApplyStackOrientation()
        {
            ApplyTableOrientation();
        }

        void EnsureShell()
        {
            if (shell == null)
            {
                var shellObject = new GameObject("Shell");
                shellObject.transform.SetParent(transform, false);
                shell = shellObject.transform;
            }

            if (frontRenderer == null)
                frontRenderer = CreateCardFace("Front", FrontFaceRotation, 0.001f);
            else
                frontRenderer.transform.localRotation = FrontFaceRotation;

            if (backRenderer == null)
                backRenderer = CreateCardFace("Back", BackFaceRotation, 0f);
            else
                backRenderer.transform.localRotation = BackFaceRotation;

            if (frontRenderer != null)
                ApplyCardScale(frontRenderer.transform);
            if (backRenderer != null)
                ApplyCardScale(backRenderer.transform);
        }

        Renderer CreateCardFace(string faceName, Quaternion localRotation, float localY)
        {
            var faceObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
            faceObject.name = faceName;
            faceObject.transform.SetParent(shell, false);
            faceObject.transform.localPosition = new Vector3(0f, localY, 0f);
            faceObject.transform.localRotation = localRotation;

            var collider = faceObject.GetComponent<Collider>();
            if (collider != null)
                collider.enabled = false;

            return faceObject.GetComponent<Renderer>();
        }

        void ApplyCardScale(Transform faceTransform)
        {
            if (faceTransform == null)
                return;

            var scale = cardScale / 10f;
            faceTransform.localScale = new Vector3(scale, 1f, scale * 1.397f);
        }

        public void SetCardScale(float scale)
        {
            cardScale = scale;
            if (frontRenderer != null)
                ApplyCardScale(frontRenderer.transform);
            if (backRenderer != null)
                ApplyCardScale(backRenderer.transform);
            ApplyPlayableOutlineScale();
            UpdateClickCollider();
        }

        public Vector3 GetWorldSize()
        {
            return new Vector3(cardScale, 0.4f, cardScale * 1.397f);
        }

        public void SetClickable(bool clickable)
        {
            EnsureClickCollider();
            clickCollider.enabled = clickable;
        }

        /// <summary>Non-null when this card represents a real Willbound.Engine.CardInstance in a player's hand.</summary>
        public int? EngineCardInstanceId { get; private set; }

        public CardPrinting BoundPrinting { get; private set; }

        public void SetEngineCardInstanceId(int? instanceId)
        {
            EngineCardInstanceId = instanceId;
        }

        public void BindPrinting(CardPrinting printing)
        {
            BoundPrinting = printing;
        }

        void EnsureClickCollider()
        {
            if (clickCollider == null)
            {
                clickCollider = GetComponent<BoxCollider>();
                if (clickCollider == null)
                    clickCollider = gameObject.AddComponent<BoxCollider>();
            }

            UpdateClickCollider();
        }

        void UpdateClickCollider()
        {
            if (clickCollider == null)
                return;

            var size = GetWorldSize();
            clickCollider.size = size;
            clickCollider.center = new Vector3(0f, 0.15f, 0f);
        }

        public void SetFrontTexture(Texture2D texture)
        {
            frontTexture = texture;
            BoundPrinting = EngineCatalog.TryGetPrintingByArt(texture, out var printing) ? printing : null;
            ApplyMaterials();
        }

        public void SetBackTexture(Texture2D texture)
        {
            cardBackTexture = texture;
            ApplyMaterials();
        }

        public Texture2D FrontTexture => frontTexture;

        public void SetFaceUpImmediate(bool faceUp)
        {
            this.faceUp = faceUp;
            if (shell != null)
                shell.localRotation = faceUp ? FaceUpShellRotation : FaceDownShellRotation;
            RefreshPlayableOutlineVisibility();
        }

        /// <summary>Soft white glow around a face-up hand card that can currently be played.</summary>
        public void SetPlayableOutline(bool enabled)
        {
            playableOutlineEnabled = enabled;
            EnsurePlayableOutline();
            RefreshPlayableOutlineVisibility();
        }

        public Transform Shell => shell;

        void ApplyMaterials()
        {
            EnsureCardAssets();
            EnsureShell();

            if (backRenderer != null && cardBackMaterial != null)
            {
                if (backMaterialInstance == null)
                    backMaterialInstance = new Material(cardBackMaterial);

                TablePresentation.ConfigureCardMaterial(backMaterialInstance, cardBackTexture);
                backRenderer.sharedMaterial = backMaterialInstance;
                TablePresentation.EnsureRendererVisible(backRenderer);
            }

            if (frontRenderer == null || cardFaceMaterial == null)
                return;

            if (frontMaterialInstance == null)
                frontMaterialInstance = new Material(cardFaceMaterial);

            TablePresentation.ConfigureCardMaterial(frontMaterialInstance, frontTexture);
            frontRenderer.sharedMaterial = frontMaterialInstance;
            TablePresentation.EnsureRendererVisible(frontRenderer);
        }

        void EnsurePlayableOutline()
        {
            if (!Application.isPlaying || shell == null)
                return;

            if (playableOutline == null)
            {
                var existing = shell.Find("PlayableGlow") ?? shell.Find("PlayableOutline");
                if (existing != null)
                    playableOutline = existing;
            }

            if (playableOutline == null)
            {
                var outlineObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
                outlineObject.name = "PlayableGlow";
                outlineObject.SetActive(false);
                outlineObject.transform.SetParent(shell, false);
                outlineObject.transform.localPosition = new Vector3(0f, -0.0012f, 0f);
                outlineObject.transform.localRotation = FrontFaceRotation;

                var collider = outlineObject.GetComponent<Collider>();
                if (collider != null)
                    collider.enabled = false;

                playableOutline = outlineObject.transform;
            }

            var outlineRenderer = playableOutline.GetComponent<Renderer>();
            if (outlineRenderer != null)
            {
                if (playableOutlineMaterial == null)
                {
                    var shader = Shader.Find("Universal Render Pipeline/Unlit")
                        ?? Shader.Find("Unlit/Transparent")
                        ?? Shader.Find("Unlit/Color");
                    playableOutlineMaterial = shader != null
                        ? new Material(shader)
                        : new Material(outlineRenderer.sharedMaterial);
                    TablePresentation.ConfigurePlayableGlowMaterial(
                        playableOutlineMaterial,
                        TablePresentation.PlayableGlowTexture());
                }

                outlineRenderer.sharedMaterial = playableOutlineMaterial;
                TablePresentation.EnsureRendererVisible(outlineRenderer);
            }

            ApplyPlayableOutlineScale();
            RefreshPlayableOutlineVisibility();
        }

        void ApplyPlayableOutlineScale()
        {
            if (playableOutline == null)
                return;

            var width = (cardScale + PlayableGlowSpread * 2f) / 10f;
            var depth = (cardScale * 1.397f + PlayableGlowSpread * 2f) / 10f;
            playableOutline.localScale = new Vector3(width, 1f, depth);
        }

        void Update()
        {
            if (!playableOutlineEnabled || playableOutlineMaterial == null)
                return;

            var pulse = 1.22f + 0.28f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * PlayableGlowPulseSpeed));
            var color = new Color(pulse, pulse, pulse, 1f);
            playableOutlineMaterial.color = color;
            if (playableOutlineMaterial.HasProperty("_BaseColor"))
                playableOutlineMaterial.SetColor("_BaseColor", color);
            if (playableOutlineMaterial.HasProperty("_Color"))
                playableOutlineMaterial.SetColor("_Color", color);
        }

        void RefreshPlayableOutlineVisibility()
        {
            if (playableOutline != null)
                playableOutline.gameObject.SetActive(playableOutlineEnabled && faceUp);
        }

        void OnDestroy()
        {
            if (playableOutlineMaterial != null)
                Destroy(playableOutlineMaterial);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (!isActiveAndEnabled)
                return;

            EnsureCardAssets();
            if (shell == null)
                return;

            ApplyTableOrientation();
            if (frontRenderer != null)
            {
                frontRenderer.transform.localRotation = FrontFaceRotation;
                ApplyCardScale(frontRenderer.transform);
            }
            if (backRenderer != null)
            {
                backRenderer.transform.localRotation = BackFaceRotation;
                ApplyCardScale(backRenderer.transform);
            }
            ApplyPlayableOutlineScale();
            ApplyMaterials();
            SetFaceUpImmediate(faceUp);
        }
#endif
    }
}
