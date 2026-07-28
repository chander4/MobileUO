using UnityEngine;
using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;

namespace Hollowmere.Interaction
{
    /// <summary>
    /// Long-press equivalent of opening a world object's context menu.
    /// GameActions.OpenPopupMenu is the actual call that requests/opens a
    /// PopupMenuGump for a world entity - it's the same call
    /// DelayedObjectClickManager uses for the native tap-then-popup path.
    /// Simulating a right-click (UIManager.OnRightMouseButtonDown/Up, or
    /// even the active scene's OnRightMouseDown/Up) does not reach this -
    /// those only manage UI-control interaction and drag/follow state, not
    /// world-object popups. GameController/PopupMenuGump/networking are
    /// untouched; this reuses the existing public call, and the existing
    /// two-finger tap keeps working unchanged alongside this.
    /// </summary>
    [RequireComponent(typeof(LongPressDetector))]
    public class ContextMenuLongPressTrigger : MonoBehaviour
    {
        private LongPressDetector detector;
        private bool subscribed;
        private LongPressStateMachine.GestureState previousGestureState = LongPressStateMachine.GestureState.Idle;

        private void Awake()
        {
            detector = GetComponent<LongPressDetector>();
            detector.ExclusionZone = new JoystickExclusionZone();
        }

        private void Update()
        {
            if (detector.Gesture == null)
            {
                return;
            }

            // LongPressDetector builds its Gesture in Start(); subscribe as
            // soon as it exists rather than depending on cross-script Start
            // ordering, which Unity does not guarantee.
            if (!subscribed)
            {
                detector.Gesture.LongPressDetected += OnLongPressDetected;
                subscribed = true;
            }

            var currentState = detector.Gesture.State;

            // The finger that triggered our long press was just released.
            // Releasing a touch over an entity always schedules the engine's
            // own delayed single-click popup request too, regardless of what
            // our long press already opened - cancel that pending duplicate
            // so it doesn't replace/flicker the popup we already showed.
            if (previousGestureState == LongPressStateMachine.GestureState.Fired
                && currentState == LongPressStateMachine.GestureState.Idle)
            {
                DelayedObjectClickManager.Clear();
            }

            previousGestureState = currentState;
        }

        private void OnDestroy()
        {
            if (subscribed && detector != null && detector.Gesture != null)
            {
                detector.Gesture.LongPressDetected -= OnLongPressDetected;
            }
        }

        private void OnLongPressDetected(Vector2 screenPosition)
        {
            // shift: true bypasses ProfileManager.Current.HoldShiftForContext -
            // long-press is itself the deliberate "I want the context menu"
            // gesture, the touch equivalent of a player choosing to hold shift.
            if (SelectedObject.Object is Entity entity)
            {
                GameActions.OpenPopupMenu(entity.Serial, true);
            }
        }
    }
}
