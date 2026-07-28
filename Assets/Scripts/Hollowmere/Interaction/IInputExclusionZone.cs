using UnityEngine;

namespace Hollowmere.Interaction
{
    /// <summary>
    /// A region of the screen where long-press (and future gesture) detection
    /// should not begin. LongPressDetector never encodes which region that is -
    /// callers inject whichever zone applies to the active input layout
    /// (joystick side, tablet chrome, controller virtual-cursor safe area, ...).
    /// </summary>
    public interface IInputExclusionZone
    {
        bool IsExcluded(Vector2 screenPosition);
    }
}
