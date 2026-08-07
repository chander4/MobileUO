using System;
using UnityEngine;

namespace Hollowmere.Diagnostics
{
    /// <summary>
    /// #24: permanent diagnostic categories - the successor to the throwaway Debug.Log
    /// scaffolding used to chase this issue's touch-dispatch and ItemHold bugs. Add a
    /// new category here rather than reusing an unrelated one, so a developer can trace
    /// exactly one subsystem at a time.
    /// </summary>
    internal enum DiagCategory
    {
        Input,
        UI,
        ItemHold,
        Actions,
        Networking,
    }

    /// <summary>
    /// Permanent, per-category toggleable diagnostic logging. Every category is off by
    /// default, so a fresh build/session pays nothing extra. Flip one on at runtime
    /// (Diag.SetEnabled(DiagCategory.ItemHold, true)) to trace that subsystem without
    /// recompiling.
    ///
    /// Call sites MUST guard with IsEnabled(category) before building the log message:
    ///
    ///   if (Diag.IsEnabled(DiagCategory.UI))
    ///       Diag.Log(DiagCategory.UI, $"Down label='{definition?.Label}'");
    ///
    /// Log() also checks internally as a safety net, but that alone doesn't help - by
    /// the time Log() runs, the caller has already paid for the $"..." interpolation.
    /// The outer guard is what makes a disabled category effectively free: a single
    /// bool-array read, nothing else evaluated.
    /// </summary>
    internal static class Diag
    {
        private static readonly bool[] enabledCategories = new bool[Enum.GetValues(typeof(DiagCategory)).Length];

        public static bool IsEnabled(DiagCategory category)
        {
            return enabledCategories[(int) category];
        }

        public static void SetEnabled(DiagCategory category, bool enabled)
        {
            enabledCategories[(int) category] = enabled;
        }

        public static void Log(DiagCategory category, string message)
        {
            if (!enabledCategories[(int) category])
            {
                return;
            }

            Debug.Log($"[{category}] {message}");
        }
    }
}
