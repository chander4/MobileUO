using UnityEngine;
using ClassicUO.Game.Scenes;

namespace Hollowmere.Interaction
{
    /// <summary>
    /// Mobile Spell/Skill UI v1: a single, always-present, non-removable "Book"
    /// trigger that opens SpellSkillPresenter. Same shape as
    /// BackpackTriggerBootstrap/PaperdollTriggerBootstrap - a self-contained,
    /// one-button ActionBar (not the real Action Bar widget) stacked one row
    /// above Paperdoll's, its own persistence key. Zero changes to
    /// ActionBarPresenter, ActionBarBootstrap, QuickChatBarBootstrap,
    /// BackpackTriggerBootstrap, or PaperdollTriggerBootstrap.
    /// </summary>
    internal static class SpellSkillTriggerBootstrap
    {
        internal const string PersistenceKey = "HollowmereActionBar.Book";

        // Row 4: above Main (row 0), QuickChat (row 1), Backpack (row 2), Gear (row 3).
        private const int RightMargin = 20;
        private const int BottomMargin = 24;
        private const int RowGap = 12;

        private static readonly Microsoft.Xna.Framework.Color Tint = new Microsoft.Xna.Framework.Color(30, 20, 40);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var go = new GameObject("HollowmereSpellSkillTriggerBootstrap");
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
                        - (ActionButtonGump.ButtonHeight + RowGap) * 4;

                    bar = new ActionBar(
                        PersistenceKey,
                        ActionBarLayout.HorizontalRightToLeft,
                        defaultX,
                        defaultY,
                        new PlayerPrefsActionBarPersistence(),
                        maxPerRow: 1,
                        tint: Tint);

                    bar.AddButton(new GameActionDefinition("Book", () => SpellSkillPresenter.Instance?.Open()));

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
