using System;
using UnityEngine;

namespace Hollowmere.Interaction
{
    /// <summary>
    /// Tuning for LongPressDetector, centralized here rather than as fields on
    /// the detector so future accessibility/interaction tuning has one place
    /// to live regardless of how many detectors exist.
    /// </summary>
    [Serializable]
    public class LongPressSettings
    {
        [SerializeField] private float holdDurationSeconds = 0.5f;
        [SerializeField] private float movementTolerancePixels = 24f;

        public LongPressSettings()
        {
        }

        public LongPressSettings(float holdDurationSeconds, float movementTolerancePixels)
        {
            this.holdDurationSeconds = holdDurationSeconds;
            this.movementTolerancePixels = movementTolerancePixels;
        }

        public float HoldDurationSeconds => holdDurationSeconds;
        public float MovementTolerancePixels => movementTolerancePixels;
    }
}
