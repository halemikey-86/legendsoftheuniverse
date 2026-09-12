using UnityEngine;

namespace LegendsOfTheUniverse.Presentation
{
    /// <summary>
    /// Shared render and layout settings for the table scene.
    /// Uses URP mesh shaders only — never sprite/2D shader graphs (Unity 6 BRG incompatible).
    /// </summary>
    public static class TablePresentation
    {
        public static readonly Color MatBlack = new(0.035f, 0.035f, 0.042f, 1f);

        public const int PlaymatRenderQueue = 1000;
        public const int CardRenderQueue = 3000;

        static Shader cachedMeshShader;
        static Texture2D playableGlowTexture;

        public static bool IsReady { get; private set; }

        public static void ResetReady() => IsReady = false;

        public static void MarkReady() => IsReady = true;

        public static void ConfigurePlaymatMaterial(Material material, Color color)
        {
            if (material == null)
                return;

            EnsureCompatibleMeshShader(material);

            material.color = color;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
            if (material.HasProperty("_BaseMap"))
                material.SetTexture("_BaseMap", null);
            material.mainTexture = null;

            material.renderQueue = PlaymatRenderQueue;
            if (material.HasProperty("_ZWrite"))
                material.SetFloat("_ZWrite", 0f);

            material.enableInstancing = true;
        }

        public static void ConfigureCardMaterial(Material material, Texture2D texture)
        {
            if (material == null)
                return;

            EnsureCompatibleMeshShader(material);

            if (texture != null)
            {
                material.mainTexture = texture;
                if (material.HasProperty("_BaseMap"))
                    material.SetTexture("_BaseMap", texture);
            }

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", Color.white);
            if (material.HasProperty("_Cull"))
                material.SetFloat("_Cull", 0f);

            ApplyCardEmission(material, texture);
            material.renderQueue = CardRenderQueue;
            material.enableInstancing = true;
        }

        public static Texture2D PlayableGlowTexture()
        {
            if (playableGlowTexture != null)
                return playableGlowTexture;

            const int width = 128;
            const int height = 180;
            var pixels = new Color[width * height];

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var px = ((x + 0.5f) / width - 0.5f) * 2f;
                    var py = ((y + 0.5f) / height - 0.5f) * 2f;
                    const float half = 0.70f;
                    const float radius = 0.10f;
                    var dx = Mathf.Abs(px) - (half - radius);
                    var dy = Mathf.Abs(py) - (half - radius);
                    var ox = Mathf.Max(dx, 0f);
                    var oy = Mathf.Max(dy, 0f);
                    var sdf = Mathf.Min(Mathf.Max(dx, dy), 0f) + Mathf.Sqrt(ox * ox + oy * oy) - radius;
                    var alpha = sdf <= 0f
                        ? 0f
                        : Mathf.Exp(-sdf * 7.5f) * 1.2f + Mathf.Exp(-sdf * 18f) * 0.7f;
                    pixels[y * width + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
                }
            }

            playableGlowTexture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave,
            };
            playableGlowTexture.SetPixels(pixels);
            playableGlowTexture.Apply(false, true);
            return playableGlowTexture;
        }

        public static void ConfigurePlayableGlowMaterial(Material material, Texture2D texture)
        {
            if (material == null)
                return;

            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Transparent")
                ?? Shader.Find("Unlit/Color")
                ?? ResolveMeshShader();
            if (shader != null)
                material.shader = shader;

            if (texture != null)
            {
                material.mainTexture = texture;
                if (material.HasProperty("_BaseMap"))
                    material.SetTexture("_BaseMap", texture);
            }

            var color = Color.white;
            material.color = color;
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);

            material.SetOverrideTag("RenderType", "Transparent");
            material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_Blend"))
                material.SetFloat("_Blend", 2f);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            if (material.HasProperty("_Cull"))
                material.SetFloat("_Cull", 0f);

            material.renderQueue = CardRenderQueue + 1;
            material.enableInstancing = true;
        }

        static void ApplyCardEmission(Material material, Texture2D texture)
        {
            if (material.HasProperty("_EmissionColor"))
                material.SetColor("_EmissionColor", Color.white);

            if (texture != null && material.HasProperty("_EmissionMap"))
                material.SetTexture("_EmissionMap", texture);

            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        static void EnsureCompatibleMeshShader(Material material)
        {
            if (IsCompatibleMeshShader(material.shader))
                return;

            var shader = ResolveMeshShader();
            if (shader != null)
                material.shader = shader;
        }

        static bool IsCompatibleMeshShader(Shader shader)
        {
            if (shader == null || shader.name.Contains("InternalError"))
                return false;

            var name = shader.name;
            if (name.Contains("Sprite") || name.Contains("2D"))
                return false;

            return name.Contains("Universal Render Pipeline")
                || name.StartsWith("Unlit/")
                || name == "Standard";
        }

        static Shader ResolveMeshShader()
        {
            if (cachedMeshShader != null)
                return cachedMeshShader;

            string[] candidates =
            {
                "Universal Render Pipeline/Lit",
                "Universal Render Pipeline/Unlit",
                "Unlit/Texture",
                "Unlit/Color",
                "Standard",
            };

            for (var i = 0; i < candidates.Length; i++)
            {
                var shader = Shader.Find(candidates[i]);
                if (shader == null || shader.name.Contains("InternalError"))
                    continue;
                if (shader.name.Contains("Sprite") || shader.name.Contains("2D"))
                    continue;

                cachedMeshShader = shader;
                return shader;
            }

            Debug.LogWarning("TablePresentation: No compatible URP mesh shader found.");
            return null;
        }

        public static void EnsureRendererVisible(Renderer renderer)
        {
            if (renderer == null)
                return;

            renderer.enabled = true;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }
}
