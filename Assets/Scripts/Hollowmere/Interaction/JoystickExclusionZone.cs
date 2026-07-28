using PreferenceEnums;
using UnityEngine;

namespace Hollowmere.Interaction
{
    /// <summary>
    /// Excludes whichever screen half the floating joystick currently occupies.
    /// Matches MobileInputController.IsPointInLeftHalf's left-half check for the
    /// default (LeftHandedMode.Off) case exactly. Mirrors to the right half when
    /// LeftHandedMode is On.
    ///
    /// Known gap this does not create: FloatingJoystickSettings.ApplyLeftHandedMode
    /// is currently a documented placeholder ("For now, we just track the
    /// setting") - MobileInputController's actual touch zone does not yet move
    /// for left-handed players. This zone is built to react correctly today so
    /// it needs no changes once that placeholder is implemented; until then, with
    /// LeftHandedMode On, this zone excludes the right half while the real
    /// joystick still only appears on the left.
    /// </summary>
    public class JoystickExclusionZone : IInputExclusionZone
    {
        public bool IsExcluded(Vector2 screenPosition)
        {
            var isLeftHalf = screenPosition.x < Screen.width * 0.5f;
            var isLeftHanded = UserPreferences.LeftHandedMode != null
                                && UserPreferences.LeftHandedMode.CurrentValue == (int)LeftHandedMode.On;

            return isLeftHanded ? !isLeftHalf : isLeftHalf;
        }
    }
}
