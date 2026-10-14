using UnityEngine;
using ClassicUO.Game.Scenes;

namespace Hollowmere.Interaction
{
    /// <summary>
    /// Mobile Paperdoll v1: a single, always-present, non-removable "Gear" trigger
    /// that opens PaperdollPresenter. Same shape as BackpackTriggerBootstrap - a
    /// self-contained, one-button ActionBar (not the real Action Bar widget) stacked
    /// one row above Backpack's, its own persistence key. Zero changes to
    /// ActionBarPresenter, ActionBarBootstrap, QuickChatBarBootstrap, or
    /// BackpackTriggerBootstrap itself.
    /// </summary>
    internal static class PaperdollTriggerBootstrap
    {
        internal const string PersistenceKey = "HollowmereActionBar.Gear";

        // Row 3: above Main (row 0), QuickChat (row 1), Backpack (row 2).
        private const int RightMargin = 20;
        private const int BottomMargin = 24;
        private const int RowGap = 12;

        private static readonly Microsoft.Xna.Framework.Color Tint = new Microsoft.Xna.Framework.Color(30, 30, 45);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var go = new GameObject("HollowmerePaperdollTriggerBootstrap");
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
                        - (ActionButtonGump.ButtonHeight + RowGap) * 3;

                    bar = new ActionBar(
                        PersistenceKey,
                        ActionBarLayout.HorizontalRightToLeft,
                        defaultX,
                        defaultY,
                        new PlayerPrefsActionBarPersistence(),
                        maxPerRow: 1,
                        tint: Tint);

                    bar.AddButton(new GameActionDefinition("Gear", () => PaperdollPresenter.Instance?.Open()));

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
