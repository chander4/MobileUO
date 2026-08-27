using System.Collections.Generic;

namespace Hollowmere.Hud.Unity
{
    /// <summary>
    /// Lightweight PlayMode smoke-test surface. Production code updates this; tests
    /// read it without referencing Assembly-CSharp composition types.
    /// </summary>
    public static class HudSmokeTestBridge
    {
        public static HudWidgetHost Host { get; private set; }

        public static bool HasResolvedLayout { get; private set; }

        public static int LastResolvedWidgetCount { get; private set; }

        public static int CompositionRunnerCount { get; private set; }

        public static int PrimaryActionLiveInstances { get; private set; }

        public static int ActionBarLiveInstances { get; private set; }

        public static int VitalsLiveInstances { get; private set; }

        public static void RegisterPrimaryActionLive(int delta)
        {
            PrimaryActionLiveInstances = System.Math.Max(0, PrimaryActionLiveInstances + delta);
        }

        public static void RegisterActionBarLive(int delta)
        {
            ActionBarLiveInstances = System.Math.Max(0, ActionBarLiveInstances + delta);
        }

        public static void RegisterVitalsLive(int delta)
        {
            VitalsLiveInstances = System.Math.Max(0, VitalsLiveInstances + delta);
        }

        public static void NotifyHostCreated(HudWidgetHost host)
        {
            Host = host;
        }

        public static void NotifyLayoutResolved(HudLayoutResolution resolution)
        {
            HasResolvedLayout = resolution != null;
            LastResolvedWidgetCount = resolution?.WidgetStates?.Count ?? 0;
        }

        public static void NotifyCompositionRunnerSpawned()
        {
            CompositionRunnerCount++;
        }

        public static void NotifyCompositionRunnerDestroyed()
        {
            CompositionRunnerCount = System.Math.Max(0, CompositionRunnerCount - 1);
        }

        public static IReadOnlyList<string> GetActivePresenterWidgetIds()
        {
            return Host == null
                ? (IReadOnlyList<string>)new string[0]
                : Host.GetActivePresenterWidgetIds();
        }

        public static void ResetForTests()
        {
            Host = null;
            HasResolvedLayout = false;
            LastResolvedWidgetCount = 0;
            CompositionRunnerCount = 0;
            PrimaryActionLiveInstances = 0;
            ActionBarLiveInstances = 0;
            VitalsLiveInstances = 0;
        }
    }
}
