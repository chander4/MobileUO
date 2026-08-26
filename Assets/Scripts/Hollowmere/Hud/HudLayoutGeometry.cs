namespace Hollowmere.Hud
{
    /// <summary>
    /// Viewport and safe-area context used to resolve persistent placement into pixels.
    /// </summary>
    public readonly struct HudLayoutGeometry
    {
        public HudSize ViewportSize { get; }
        public HudInsets SafeAreaInsets { get; }

        public HudLayoutGeometry(HudSize viewportSize, HudInsets safeAreaInsets)
        {
            ViewportSize = viewportSize;
            SafeAreaInsets = safeAreaInsets;
        }

        public HudRect SafeArea => HudPlacementMath.CalculateSafeArea(ViewportSize, SafeAreaInsets);
    }
}
