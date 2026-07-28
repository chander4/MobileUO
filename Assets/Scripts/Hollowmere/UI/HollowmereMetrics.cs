using UnityEngine;

namespace Hollowmere.UI
{
    /// <summary>
    /// Shared layout tokens. Plain constants, not a ScriptableObject: spacing and
    /// corner radius are physical layout values every theme shares, not something
    /// a theme swaps.
    /// </summary>
    public static class HollowmereMetrics
    {
        public static class Spacing
        {
            public const float Tiny = 4f;
            public const float Small = 8f;
            public const float Medium = 16f;
            public const float Large = 24f;
            public const float ExtraLarge = 32f;
        }

        /// <summary>
        /// Pixel radius tokens. Small/Medium/Large currently render as square
        /// corners: no existing sprite in Assets/Content/UI/Textures has baked-in
        /// rounded corners, and this pass intentionally does not generate new art.
        /// The values exist so layout math and future real art can consume them
        /// without a code change. Round is the one variant with real visual
        /// rounding right now, via the existing circle.png sprite.
        /// </summary>
        public static class CornerRadius
        {
            public const float Small = 4f;
            public const float Medium = 8f;
            public const float Large = 16f;
            public const float Round = -1f;
        }

        public readonly struct ShadowPreset
        {
            public readonly Vector2 distance;
            public readonly float alpha;

            public ShadowPreset(Vector2 distance, float alpha)
            {
                this.distance = distance;
                this.alpha = alpha;
            }
        }

        public static class Shadow
        {
            public static readonly ShadowPreset Soft = new ShadowPreset(new Vector2(1f, -1f), 0.2f);
            public static readonly ShadowPreset Medium = new ShadowPreset(new Vector2(2f, -2f), 0.35f);
            public static readonly ShadowPreset Strong = new ShadowPreset(new Vector2(3f, -3f), 0.5f);
        }
    }
}
