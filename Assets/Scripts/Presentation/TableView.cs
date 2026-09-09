using UnityEngine;

namespace LegendsOfTheUniverse.Presentation
{
    /// <summary>
    /// TableView — camera, lighting, playmat background.
    /// GRE.cs: empty playmat background (gothic storm city); slot prefabs spawn on top later.
    /// </summary>
    [DisallowMultipleComponent]
    public class TableView : MonoBehaviour
    {
        static readonly Color MidnightNavy = new(0.05f, 0.08f, 0.18f);

        [Header("Camera")]
        [SerializeField] Camera tableCamera;
        [SerializeField] float cameraHeight = 10f;
        [SerializeField] bool orthographic = true;
        [SerializeField] float orthographicSize = 6f;

        [Header("Playmat")]
        [SerializeField] Transform matRoot;
        [SerializeField] Material playmatMaterial;
        [SerializeField] Texture2D playmatTexture;
        [SerializeField] Vector2 matSize = new(12f, 12f);

        void Awake()
        {
            SetupCamera();
            SetupMat();
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

            var cameraTransform = tableCamera.transform;
            cameraTransform.SetParent(transform, false);
            cameraTransform.localPosition = new Vector3(0f, cameraHeight, 0f);
            cameraTransform.localRotation = Quaternion.Euler(90f, 175f, 0f);

            tableCamera.orthographic = orthographic;
            if (orthographic)
                tableCamera.orthographicSize = orthographicSize;

            tableCamera.clearFlags = CameraClearFlags.SolidColor;
            tableCamera.backgroundColor = MidnightNavy;
            tableCamera.tag = "MainCamera";
        }

        void SetupMat()
        {
            if (matRoot == null)
            {
                var matObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
                matObject.name = "Playmat";
                matObject.transform.SetParent(transform, false);
                matObject.transform.localPosition = Vector3.zero;
                matObject.transform.localRotation = Quaternion.identity;
                matObject.transform.localScale = new Vector3(matSize.x / 10f, 1f, matSize.y / 10f);

                var collider = matObject.GetComponent<Collider>();
                if (collider != null)
                    Destroy(collider);

                matRoot = matObject.transform;
            }

            var renderer = matRoot.GetComponent<Renderer>();
            if (renderer == null)
                return;

            if (playmatMaterial != null)
            {
                renderer.sharedMaterial = playmatMaterial;
                return;
            }

            var runtimeMaterial = CreatePlaymatMaterial();
            renderer.sharedMaterial = runtimeMaterial;
        }

        Material CreatePlaymatMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null || shader.name == "Hidden/InternalErrorShader")
                shader = Shader.Find("Standard");

            var material = new Material(shader) { color = MidnightNavy };

            if (playmatTexture != null)
                material.mainTexture = playmatTexture;

            return material;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (!isActiveAndEnabled)
                return;

            SetupCamera();
            SetupMat();
        }
#endif
    }
}
