using System;
using UnityEngine;

namespace Hollowmere.Interaction
{
    /// <summary>
    /// Pure gesture-state logic for tap detection: given finger down/move/up
    /// events and elapsed time, decides when a short tap (not a drag, not a
    /// long press) has occurred. No Unity input or LeanTouch dependency, same
    /// split as LongPressStateMachine/LongPressDetector.
    ///
    /// Deliberately the mirror image of LongPressStateMachine: that one fires
    /// while still pressed once a hold duration elapses; this one fires on
    /// release, only if the hold duration never got that far - so a genuine
    /// long-press's release never also fires a tap.
    /// </summary>
    public class WorldTapStateMachine
    {
        public enum GestureState
        {
            Idle,
            Excluded,
            Pressing
        }

        private readonly IInputExclusionZone exclusionZone;
        private readonly float movementTolerancePixels;
        private readonly float maxTapDurationSeconds;

        private Vector2 startPosition;
        private float startTime;

        public GestureState State { get; private set; } = GestureState.Idle;

        /// <summary>Fired on finger-up with the release position, only for a genuine tap.</summary>
        public event Action<Vector2> TapDetected;

        public WorldTapStateMachine(IInputExclusionZone exclusionZone, float movementTolerancePixels = 24f, float maxTapDurationSeconds = 0.5f)
        {
            this.exclusionZone = exclusionZone;
            this.movementTolerancePixels = movementTolerancePixels;
            this.maxTapDurationSeconds = maxTapDurationSeconds;
        }

        public void OnFingerDown(Vector2 position, float time)
        {
            startPosition = position;
            startTime = time;
            State = exclusionZone != null && exclusionZone.IsExcluded(position)
                ? GestureState.Excluded
                : GestureState.Pressing;
        }

        public void OnFingerMove(Vector2 position, float time)
        {
            if (State != GestureState.Pressing)
            {
                return;
            }

            if (Vector2.Distance(position, startPosition) > movementTolerancePixels)
            {
                State = GestureState.Idle;
            }
        }

        public void OnFingerUp(Vector2 position, float time)
        {
            if (State == GestureState.Pressing && (time - startTime) < maxTapDurationSeconds)
            {
                TapDetected?.Invoke(position);
            }

            State = GestureState.Idle;
        }
    }
}
