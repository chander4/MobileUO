using UnityEngine;
using ClassicUO.Game;
using ClassicUO.Game.Scenes;

namespace Hollowmere.Interaction
{
    /// <summary>
    /// Creates a second ActionBar populated with a handful of quick-phrase
    /// buttons that call the same GameActions.Say path the text-entry chat
    /// already uses - additive on top of the already-working mobile
    /// keyboard entry, not a replacement for it. This is the first consumer
    /// of "multiple ActionBars at once" (see docs/action-bar.md).
    /// </summary>
    internal static class QuickChatBarBootstrap
    {
        private const string PersistenceKey = "HollowmereActionBar.QuickChat";
        private const int DefaultX = 20;
        private const int DefaultY = 260;

        private static readonly (string Label, string Phrase)[] Phrases =
        {
            ("Yes", "Yes"),
            ("No", "No"),
            ("Thanks!", "Thanks!"),
            ("Help!", "Help!"),
            ("Wait", "Wait"),
            ("Follow me", "Follow me"),
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var go = new GameObject("HollowmereQuickChatBarBootstrap");
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

                    foreach (var (label, phrase) in Phrases)
                    {
                        bar.AddButton(new GameActionDefinition(label, () => GameActions.Say(phrase)));
                    }

                    return;
                }

                if (ClassicUO.Client.Game == null || !(ClassicUO.Client.Game.Scene is GameScene))
                {
                    bar = null;
                    return;
                }

                bar.Tick();
            }
        }
    }
}
