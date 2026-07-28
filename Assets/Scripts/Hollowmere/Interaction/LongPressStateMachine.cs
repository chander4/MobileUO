using System;
using UnityEngine;

namespace Hollowmere.Interaction
{
    /// <summary>
    /// Pure gesture-state logic for long-press detection: given finger
    /// down/move/up events and elapsed time, decides when a long press has
    /// occurred. No Unity input or LeanTouch dependency, so it can be driven
    /// with synthetic events for runtime validation without a real touch
    /// device. Deliberately minimal - down/move/up/elapsed is the whole job.
    /// </summary>
    public class LongPressStateMachine
    {
        public enum GestureState
        {
            Idle,
            Excluded,
            Pressing,
            Fired
        }

        private readonly LongPressSettings settings;
        private readonly IInputExclusionZone exclusionZone;

        private Vector2 startPosition;
        private float startTime;

        public GestureState State { get; private set; } = GestureState.Idle;

        public event Action<Vector2> LongPressDetected;

        public LongPressStateMachine(LongPressSettings settings, IInputExclusionZone exclusionZone)
        {
            this.settings = settings ?? new LongPressSettings();
            this.exclusionZone = exclusionZone;
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

            if (Vector2.Distance(position, startPosition) > settings.MovementTolerancePixels)
            {
                State = GestureState.Idle;
            }
        }

        public void Tick(float time)
        {
            if (State != GestureState.Pressing)
            {
                return;
            }

            if (time - startTime >= settings.HoldDurationSeconds)
            {
                State = GestureState.Fired;
                LongPressDetected?.Invoke(startPosition);
            }
        }

        public void OnFingerUp(float time)
        {
            State = GestureState.Idle;
        }
    }
}
