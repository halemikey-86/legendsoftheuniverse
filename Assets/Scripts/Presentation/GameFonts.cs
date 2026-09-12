using UnityEngine;

namespace LegendsOfTheUniverse.Presentation
{
    /// <summary>
    /// Atkinson Hyperlegible family from Assets/Fonts (runtime copies in Resources/Fonts).
    /// </summary>
    public static class GameFonts
    {
        const string FontRoot = "Fonts/AtkinsonHyperlegible";

        static Font regular;
        static Font bold;
        static Font italic;
        static Font boldItalic;

        public static Font Regular => regular ??= Load("Regular");
        public static Font Bold => bold ??= Load("Bold");
        public static Font Italic => italic ??= Load("Italic");
        public static Font BoldItalic => boldItalic ??= Load("BoldItalic");

        /// <summary>Primary UI typeface — falls back to Unity legacy font if import missing.</summary>
        public static Font Default => Regular != null ? Regular : Fallback;

        static Font Fallback => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        static Font Load(string variant)
        {
            var font = Resources.Load<Font>($"{FontRoot}-{variant}");
            if (font == null)
                Debug.LogWarning($"GameFonts: Missing Resources/{FontRoot}-{variant}. Using legacy font.");
            return font;
        }
    }
}
