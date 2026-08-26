namespace Hollowmere.Hud
{
    /// <summary>
    /// Runtime pixel geometry for one widget after host layout resolution.
    /// </summary>
    public readonly struct HudResolvedWidgetLayout
    {
        public HudWidgetState State { get; }
        public HudRect Bounds { get; }

        public HudResolvedWidgetLayout(HudWidgetState state, HudRect bounds)
        {
            State = state;
            Bounds = bounds;
        }
    }
}
