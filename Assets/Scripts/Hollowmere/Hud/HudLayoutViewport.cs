namespace Hollowmere.Hud
{
    /// <summary>
    /// Runtime viewport classification for variant selection. Does not contain pixels.
    /// </summary>
    public readonly struct HudLayoutViewport : System.IEquatable<HudLayoutViewport>
    {
        public HudOrientation Orientation { get; }
        public HudDeviceClass DeviceClass { get; }
        public string InputMode { get; }

        public HudLayoutViewport(HudOrientation orientation, HudDeviceClass deviceClass, string inputMode = null)
        {
            Orientation = orientation;
            DeviceClass = deviceClass;
            InputMode = inputMode;
        }

        public bool Equals(HudLayoutViewport other)
        {
            return Orientation == other.Orientation
                && DeviceClass == other.DeviceClass
                && string.Equals(InputMode, other.InputMode, System.StringComparison.Ordinal);
        }

        public override bool Equals(object obj) => obj is HudLayoutViewport other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Orientation;
                hash = (hash * 397) ^ (int)DeviceClass;
                hash = (hash * 397) ^ (InputMode == null ? 0 : System.StringComparer.Ordinal.GetHashCode(InputMode));
                return hash;
            }
        }
    }
}
