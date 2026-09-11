using UnityEngine;
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
        [SerializeField] float cardThickness = 0.012f;
        [SerializeField] bool faceUp;

        Material frontMaterialInstance;
        Material backMaterialInstance;
        BoxCollider clickCollider;

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
            UpdateClickCollider();
        }

        public Vector3 GetWorldSize()
        {
            return new Vector3(cardScale, 0.1f, cardScale * 1.397f);
        }

        public void SetClickable(bool clickable)
        {
            EnsureClickCollider();
            clickCollider.enabled = clickable;
        }

        /// <summary>Non-null when this card represents a real Willbound.Engine.CardInstance in a player's hand.</summary>
        public int? EngineCardInstanceId { get; private set; }

        public void SetEngineCardInstanceId(int? instanceId)
        {
            EngineCardInstanceId = instanceId;
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
            clickCollider.center = new Vector3(0f, 0.05f, 0f);
        }

        public void SetFrontTexture(Texture2D texture)
        {
            frontTexture = texture;
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
            ApplyMaterials();
            SetFaceUpImmediate(faceUp);
        }
#endif
    }
}
