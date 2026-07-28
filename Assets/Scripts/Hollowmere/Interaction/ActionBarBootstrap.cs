using UnityEngine;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;

namespace Hollowmere.Interaction
{
    /// <summary>
    /// Creates the main ActionBar and populates it with Target Last - the
    /// only consumer so far. Future actions (Attack Last, War Mode, ...) are
    /// added here as more AddButton calls, or via additional bars; this file
    /// is the only place that knows what buttons belong on the default bar.
    /// Runs via RuntimeInitializeOnLoadMethod instead of a scene-attached
    /// MonoBehaviour, so it needs no GameObject wiring.
    /// </summary>
    internal static class ActionBarBootstrap
    {
        private const string PersistenceKey = "HollowmereActionBar.Main";
        private const int DefaultX = 20;
        private const int DefaultY = 200;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var go = new GameObject("HollowmereActionBarBootstrap");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<Runner>();
        }

        private class Runner : MonoBehaviour
        {
            private ActionBar bar;

            private void Update()
            {
                if (bar == null)
                {
                    if (ClassicUO.Client.Game == null || !(ClassicUO.Client.Game.Scene is GameScene))
                    {
                        return;
                    }

                    bar = new ActionBar(PersistenceKey, ActionBarLayout.Horizontal, DefaultX, DefaultY, new PlayerPrefsActionBarPersistence());

                    var targetLast = new GameActionDefinition(
                        "Target Last",
                        TargetManager.TargetLast,
                        () => TargetManager.IsTargeting);

                    bar.AddButton(targetLast);
                    return;
                }

                if (ClassicUO.Client.Game == null || !(ClassicUO.Client.Game.Scene is GameScene))
                {
                    // Previous GameScene's UIManager/Gumps are gone once we're back at the
                    // login screen - drop the bar so a fresh one is built on the next login.
                    bar = null;
                    return;
                }

                bar.Tick();
            }
        }
    }
}
