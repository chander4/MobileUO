using ClassicUO.Game;
using ClassicUO.Game.GameObjects;

namespace Hollowmere.Interaction
{
    /// <summary>
    /// #34: mobile-friendly "who is threatening me / who do I attack back" helper.
    /// Precisely tapping a small, moving enemy on a touchscreen is hard, especially
    /// mid-fight - this scans already-tracked engine state (NotorietyFlag, InWarMode,
    /// Distance) for the nearest hostile mobile so both Target Vitals and the Attack
    /// Nearest action can react to a threat before the player manages to tap it
    /// directly. Server-agnostic: only reads flags every server already sends, never
    /// requests anything or hardcodes shard-specific rules.
    /// </summary>
    internal static class NearestThreatFinder
    {
        // Heuristic combat-awareness radius (tiles), not melee-only - a PK/ranged
        // creature can be actively engaging before they're adjacent. Tunable; not a
        // protocol constant.
        private const int ThreatRadiusTiles = 10;

        public static Mobile FindNearest()
        {
            Mobile player = World.Player;
            if (player == null)
            {
                return null;
            }

            Mobile nearest = null;
            int nearestDistance = int.MaxValue;

            foreach (Mobile mobile in World.Mobiles)
            {
                if (mobile == null || mobile.IsDestroyed || mobile == player)
                {
                    continue;
                }

                if (!mobile.InWarMode || !PrimaryActionResolver.IsHostile(mobile.NotorietyFlag))
                {
                    continue;
                }

                int distance = mobile.Distance;
                if (distance > ThreatRadiusTiles || distance >= nearestDistance)
                {
                    continue;
                }

                nearestDistance = distance;
                nearest = mobile;
            }

            return nearest;
        }
    }
}
