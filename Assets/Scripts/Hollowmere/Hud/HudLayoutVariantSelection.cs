namespace Hollowmere.Hud
{
    public enum HudLayoutVariantFallbackStep
    {
        ExactDeviceClassAndOrientation = 1,
        OrientationOnly = 2,
        DeviceClassBase = 3,
        ProfileBase = 4
    }

    public sealed class HudLayoutVariantSelection
    {
        public HudLayoutVariant Variant { get; }
        public HudLayoutVariantKey ResolvedKey { get; }
        public HudLayoutVariantFallbackStep FallbackStep { get; }

        internal HudLayoutVariantSelection(
            HudLayoutVariant variant,
            HudLayoutVariantKey resolvedKey,
            HudLayoutVariantFallbackStep fallbackStep)
        {
            Variant = variant;
            ResolvedKey = resolvedKey;
            FallbackStep = fallbackStep;
        }
    }
}
