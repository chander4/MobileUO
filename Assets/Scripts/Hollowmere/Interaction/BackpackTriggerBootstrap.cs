using UnityEngine;
using ClassicUO.Game.Scenes;

namespace Hollowmere.Interaction
{
    /// <summary>
    /// #32: a single, always-present, non-removable "Bag" trigger that opens
    /// BackpackPresenter. Self-contained (its own tiny ActionBar, own persistence
    /// key) rather than reusing the Main/QuickChat bars' bootstraps, so this stays
    /// purely additive - zero changes to ActionBarPresenter, ActionBarBootstrap, or
    /// QuickChatBarBootstrap. Same RuntimeInitializeOnLoadMethod + per-frame Runner
    /// shape as QuickChatBarBootstrap.
    /// </summary>
    internal static class BackpackTriggerBootstrap
    {
        internal const string PersistenceKey = "HollowmereActionBar.Bag";

        // Stacks a third row above the Main bar (row 0) and QuickChat bar (row 1),
        // same right-edge-origin convention QuickChatBarBootstrap already uses to
        // stack above Main.
        private const int RightMargin = 20;
        private const int BottomMargin = 24;
        private const int RowGap = 12;

        private static readonly Microsoft.Xna.Framework.Color Tint = new Microsoft.Xna.Framework.Color(45, 30, 15);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var go = new GameObject("HollowmereBackpackTriggerBootstrap");
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

                    int viewportWidth = ClassicUO.Client.Game.Window.ClientBounds.Width;
                    int defaultX = viewportWidth - ActionButtonGump.ButtonWidth - RightMargin;
                    int defaultY = ClassicUO.Client.Game.Window.ClientBounds.Height
                        - ActionButtonGump.ButtonHeight - BottomMargin
                        - (ActionButtonGump.ButtonHeight + RowGap) * 2;

                    bar = new ActionBar(
                        PersistenceKey,
                        ActionBarLayout.HorizontalRightToLeft,
                        defaultX,
                        defaultY,
                        new PlayerPrefsActionBarPersistence(),
                        maxPerRow: 1,
                        tint: Tint);

                    bar.AddButton(new GameActionDefinition("Bag", () => BackpackPresenter.Instance?.Open()));

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
