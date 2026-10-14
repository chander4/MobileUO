namespace Hollowmere.Hud
{
    /// <summary>
    /// Pure formatting for the target-vitals distance readout (#34 follow-up). UO's
    /// engine already exposes distance as whole tiles (Chebyshev distance from
    /// World.Player - see GameObject.Distance in external/ClassicUO), so this only
    /// formats that existing value; it never computes distance itself.
    /// </summary>
    public static class HudDistanceFormatter
    {
        /// <summary>
        /// Matches GameObject.Distance's own sentinel for "no player in world".
        /// </summary>
        public const int UnknownDistance = ushort.MaxValue;

        public static string FormatTiles(int tiles)
        {
            if (tiles < 0 || tiles >= UnknownDistance)
            {
                return string.Empty;
            }

            return tiles == 1 ? "1 tile" : $"{tiles} tiles";
        }
    }
}
