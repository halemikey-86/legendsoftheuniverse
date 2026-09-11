using UnityEngine;

namespace LegendsOfTheUniverse.Presentation
{
    /// <summary>
    /// Card footprint helpers — scale is world width; spacing must exceed width to avoid overlap.
    /// </summary>
    public static class CardLayout
    {
        public const float DepthAspect = 1.397f;

        public static float Width(float scale) => scale;

        public static float Depth(float scale) => scale * DepthAspect;

        /// <summary>Center-to-center spacing for a horizontal row.</summary>
        public static float SpreadSpacing(float scale, float gapFraction = 0.12f) =>
            scale * (1f + gapFraction);
    }
}
