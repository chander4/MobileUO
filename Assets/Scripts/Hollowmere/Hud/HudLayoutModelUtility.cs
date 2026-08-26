using System;
using System.Collections.Generic;

namespace Hollowmere.Hud
{
    internal static class HudLayoutModelUtility
    {
        public static HudWidgetState CloneState(HudWidgetState state)
        {
            if (state == null)
            {
                return null;
            }

            HudPlacement placement = state.Placement;
            return new HudWidgetState
            {
                WidgetId = state.WidgetId,
                IsVisible = state.IsVisible,
                IsLocked = state.IsLocked,
                SizePolicy = state.SizePolicy,
                Placement = placement == null
                    ? new HudPlacement()
                    : new HudPlacement
                    {
                        Anchor = placement.Anchor,
                        Pivot = placement.Pivot,
                        LogicalOffset = placement.LogicalOffset,
                        Scale = placement.Scale,
                        DockEdge = placement.DockEdge,
                        DockOrder = placement.DockOrder
                    }
            };
        }

        public static List<HudWidgetState> CloneStates(IEnumerable<HudWidgetState> states)
        {
            var copy = new List<HudWidgetState>();

            foreach (HudWidgetState state in states ?? Array.Empty<HudWidgetState>())
            {
                if (state != null)
                {
                    copy.Add(CloneState(state));
                }
            }

            return copy;
        }
    }
}
