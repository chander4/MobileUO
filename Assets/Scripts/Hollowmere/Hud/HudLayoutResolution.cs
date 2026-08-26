using System.Collections.Generic;

namespace Hollowmere.Hud
{
    /// <summary>
    /// Resolved layout for one viewport: selected variant metadata plus widget states
    /// ready for geometry application. Mutating this list does not change the profile
    /// until the host persists through <see cref="HudLayoutService"/>.
    /// </summary>
    public sealed class HudLayoutResolution
    {
        public HudLayoutVariantFallbackStep FallbackStep { get; }
        public HudLayoutVariantKey VariantKey { get; }
        public IReadOnlyList<HudWidgetState> WidgetStates { get; }
        public IReadOnlyList<string> Warnings { get; }

        internal HudLayoutResolution(
            HudLayoutVariantFallbackStep fallbackStep,
            HudLayoutVariantKey variantKey,
            IReadOnlyList<HudWidgetState> widgetStates,
            IReadOnlyList<string> warnings)
        {
            FallbackStep = fallbackStep;
            VariantKey = variantKey;
            WidgetStates = widgetStates;
            Warnings = warnings;
        }
    }
}
