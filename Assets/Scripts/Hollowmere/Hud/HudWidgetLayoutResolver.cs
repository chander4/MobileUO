using System;

namespace Hollowmere.Hud
{
    public static class HudWidgetLayoutResolver
    {
        public static HudRect ResolveBounds(HudWidgetState state, HudLayoutGeometry geometry, HudSize intrinsicSize)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            HudRect safeArea = geometry.SafeArea;
            HudPlacement placement = state.Placement ?? new HudPlacement();

            if (placement.DockEdge != HudDockEdge.None)
            {
                return HudPlacementMath.ResolveDocked(
                    safeArea,
                    new HudSize(intrinsicSize.Width * placement.Scale, intrinsicSize.Height * placement.Scale),
                    placement.DockEdge,
                    placement.DockOrder,
                    spacing: 8);
            }

            return HudPlacementMath.Resolve(placement, safeArea, intrinsicSize);
        }
    }
}
