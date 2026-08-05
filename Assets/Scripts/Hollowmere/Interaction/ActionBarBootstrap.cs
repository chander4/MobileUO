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
        // internal, not private: ActionCatalog's "Reset Action Bars" entry needs the
        // same key to clear the right PlayerPrefs slot.
        internal const string PersistenceKey = "HollowmereActionBar.Main";

        // internal, not private: lets OptionsGump's Hollowmere section reach the live
        // bar to toggle orientation without this bootstrap needing to know about
        // OptionsGump. Null whenever no GameScene bar exists (login screen, or before
        // first login) - same lifetime as the private `bar` field it mirrors.
        internal static ActionBar Current { get; private set; }

        // Dock to the bottom-RIGHT, not left: LeftTouchZone/FloatingJoystick own the
        // left half of the screen for movement, so a left-docked action bar sits right
        // under the player's movement thumb and is hard to hit mid-combat. Right thumb
        // = actions is the standard mobile convention. Grows leftward
        // (HorizontalRightToLeft) from the right-edge origin so adding buttons (via the
        // picker) never runs the bar off-screen. Still a draggable floating Gump
        // underneath - only the *default* spawn position changes (see
        // HOLLOWMERE_MOBILE_MODERNIZATION_VISION.md Phase 1 for the real fix, a proper
        // docked-region layout).
        private const int RightMargin = 20;
        private const int BottomMargin = 24;
        private const int ButtonWidth = 88;
        private const int ButtonHeight = 44;

        // Caps the bar to roughly the right 45% of the screen before wrapping to a new
        // row above itself - without this, enough picker-added buttons eventually grow
        // the bar back across the whole screen and into the joystick's zone, the exact
        // thing right-docking was meant to prevent. Scales with device width so tablets
        // (wider screens) naturally get more columns per row than phones.
        private const float MaxRowWidthFraction = 0.45f;

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

                    var persistence = new PlayerPrefsActionBarPersistence();

                    // #22 D4: read before ActionBar/AddButton touch persistence at all -
                    // every AddButton call re-saves contents from current state, so
                    // reading late would see our own just-added defaults' (empty) write
                    // instead of what the player actually left on the bar last session.
                    persistence.TryLoadContents(PersistenceKey, out System.Collections.Generic.IReadOnlyList<string> savedContents);

                    int viewportWidth = ClassicUO.Client.Game.Window.ClientBounds.Width;
                    int defaultX = viewportWidth - ButtonWidth - RightMargin;
                    int defaultY = ClassicUO.Client.Game.Window.ClientBounds.Height - ButtonHeight - BottomMargin;
                    int maxPerRow = Mathf.Max(1, Mathf.FloorToInt(viewportWidth * MaxRowWidthFraction / ButtonWidth));

                    // Default tint (null) - this is the bar's "own" look; QuickChatBarBootstrap
                    // gets a distinct tint so the two are told apart at a glance.
                    bar = new ActionBar(PersistenceKey, ActionBarLayout.HorizontalRightToLeft, defaultX, defaultY, persistence, maxPerRow);
                    Current = bar;

                    var targetLast = new GameActionDefinition(
                        "Target Last",
                        TargetManager.TargetLast,
                        () => TargetManager.IsTargeting);

                    bar.AddButton(targetLast);

                    // #22 D5: persistent "+" trigger - opens the Add Action picker for
                    // this bar. Not removable (see ActionBar.AddButton) since it's the
                    // only way to reach the picker in the first place.
                    var addAction = new GameActionDefinition("+", () => ActionPickerPresenter.Instance?.Open(bar));

                    bar.AddButton(addAction);

                    // #22 D4: restore whatever the player added via the picker last
                    // session. A stale/unrecognized ref (game update removed a spell,
                    // etc.) is silently skipped rather than breaking the whole bar.
                    foreach (string refId in savedContents)
                    {
                        if (ActionCatalog.TryResolve(refId, out CatalogEntry entry))
                        {
                            bar.AddButton(entry.Definition, removable: true);
                        }
                    }

                    return;
                }

                if (ClassicUO.Client.Game == null || !(ClassicUO.Client.Game.Scene is GameScene))
                {
                    // Previous GameScene's UIManager/Gumps are gone once we're back at the
                    // login screen - drop the bar so a fresh one is built on the next login.
                    bar = null;
                    Current = null;
                    return;
                }

                bar.Tick();
            }
        }
    }
}
