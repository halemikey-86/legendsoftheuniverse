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
