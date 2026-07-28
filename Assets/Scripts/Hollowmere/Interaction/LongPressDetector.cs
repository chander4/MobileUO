using UnityEngine;

namespace Hollowmere.Interaction
{
    /// <summary>
    /// Thin Unity/LeanTouch adapter: feeds real finger data into a
    /// LongPressStateMachine each frame. Owns Unity/LeanTouch integration
    /// only - all gesture-state logic lives in the state machine. Never
    /// knows what an exclusion zone represents (joystick, tablet layout,
    /// controller, ...); callers assign whichever IInputExclusionZone
    /// applies before Start runs.
    /// </summary>
    public class LongPressDetector : MonoBehaviour
    {
        [SerializeField] private LongPressSettings settings = new LongPressSettings();

        private int trackedFingerIndex = -1;

        public IInputExclusionZone ExclusionZone { get; set; }

        public LongPressStateMachine Gesture { get; private set; }

        private void Start()
        {
            Gesture = new LongPressStateMachine(settings, ExclusionZone);
        }

        private void Update()
        {
            if (Gesture == null)
            {
                return;
            }

            var fingers = Lean.Touch.LeanTouch.GetFingers(true, false);

            if (trackedFingerIndex != -1)
            {
                var finger = FindFinger(fingers, trackedFingerIndex);
                if (finger == null || finger.Up)
                {
                    Gesture.OnFingerUp(Time.unscaledTime);
                    trackedFingerIndex = -1;
                    return;
                }

                Gesture.OnFingerMove(finger.ScreenPosition, Time.unscaledTime);
                Gesture.Tick(Time.unscaledTime);
                return;
            }

            foreach (var finger in fingers)
            {
                if (finger.Down)
                {
                    trackedFingerIndex = finger.Index;
                    Gesture.OnFingerDown(finger.ScreenPosition, Time.unscaledTime);
                    break;
                }
            }
        }

        private static Lean.Touch.LeanFinger FindFinger(System.Collections.Generic.List<Lean.Touch.LeanFinger> fingers, int index)
        {
            for (var i = 0; i < fingers.Count; i++)
            {
                if (fingers[i].Index == index)
                {
                    return fingers[i];
                }
            }

            return null;
        }
    }
}
