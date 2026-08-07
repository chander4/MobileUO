using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using Hollowmere.Diagnostics;
using UnityEngine;

namespace Hollowmere.Interaction
{
    /// <summary>
    /// #24: creates the single always-present context-sensitive primary action
    /// button and keeps it in sync with SelectedObject.LastObject via
    /// PrimaryActionResolver -> PrimaryActionDefinitionFactory -> SetDefinition.
    /// Deliberately NOT an ActionBar entry - it's a single fixed HUD element, not
    /// something a player adds/removes/reorders via #22's picker, and ActionBar's
    /// row-wrapping/persistence machinery doesn't apply to one button.
    ///
    /// Placement is explicitly temporary (right edge, in the gap between the top
    /// menu bar and the bottom-right action bars) - the real position is a
    /// Persistent HUD (vision doc Phase 2) decision, not made here. Re-evaluate
    /// this bootstrap's position constants once that milestone is designed rather
    /// than treating this as final.
    ///
    /// NOT top-right: confirmed via an on-device screenshot that the top-right
    /// corner overlaps the existing top menu bar's NetStats button and hamburger
    /// icon cluster - the button rendered fine there but silently never received a
    /// single touch event (confirmed via logcat: zero OnMouseDown/OnMouseUp ever
    /// fired), because that screen region was already claimed by other UI chrome.
    /// </summary>
    internal static class PrimaryActionButtonBootstrap
    {
        private const int TopMargin = 100;
        private const int RightMargin = 100;
        private const int ButtonWidth = 88;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var go = new UnityEngine.GameObject("HollowmerePrimaryActionButtonBootstrap");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<Runner>();
        }

        private class Runner : MonoBehaviour
        {
            private ActionButtonGump button;

            // Reference-compare against the last-seen selection so SetDefinition only
            // runs on an actual change, not every frame - matches the doc's "swap on
            // change, not a per-frame poll-and-recreate" design.
            private BaseGameObject lastSeenSelection;

            // #24: edge-triggered (logs on change, not every frame) watch for ItemHold
            // re-engaging after a pickup - this is what caught the native
            // click-and-drag-to-pickup gesture re-grabbing an item the Primary Action
            // button had already picked up. Kept as a permanent DiagCategory.ItemHold
            // trace rather than deleted, since ItemHold state changes are exactly what
            // future pickup/drop bugs will need visibility into.
            private bool lastItemHoldEnabled;

            private void Update()
            {
                if (button == null)
                {
                    if (ClassicUO.Client.Game == null || !(ClassicUO.Client.Game.Scene is GameScene))
                    {
                        return;
                    }

                    int defaultX = ClassicUO.Client.Game.Window.ClientBounds.Width - ButtonWidth - RightMargin;

                    PrimaryAction initial = PrimaryActionResolver.Resolve(SelectedObject.LastObject);
                    lastSeenSelection = SelectedObject.LastObject;

                    button = new ActionButtonGump(PrimaryActionDefinitionFactory.Create(initial), defaultX, TopMargin);
                    UIManager.Add(button);
                    UIManager.AnchorManager[button] = new AnchorManager.AnchorGroup(button);

                    return;
                }

                if (ClassicUO.Client.Game == null || !(ClassicUO.Client.Game.Scene is GameScene))
                {
                    // Previous GameScene's UIManager/Gumps are gone once we're back at
                    // the login screen - drop the button so a fresh one is built next
                    // login, matching ActionBarBootstrap/QuickChatBarBootstrap.
                    button = null;
                    lastSeenSelection = null;
                    return;
                }

                if (ItemHold.Enabled != lastItemHoldEnabled)
                {
                    lastItemHoldEnabled = ItemHold.Enabled;
                    if (Diag.IsEnabled(DiagCategory.ItemHold))
                        Diag.Log(DiagCategory.ItemHold, $"Enabled -> {lastItemHoldEnabled} serial={ItemHold.Serial}");
                }

                // GameScene.Update nulls SelectedObject.LastObject every frame the
                // touch isn't over the game viewport - true the instant a finger is
                // anywhere near this button, since it's UI, not the viewport. Confirmed
                // via logcat that OnMouseEnter (hover) fires *before* OnMouseDown, so
                // guarding on IsPressed alone still left a gap: hover shows the right
                // verb, then the null-selection swap fires before the down event ever
                // arrives, and the press executes against a neutral definition instead.
                // IsHovered closes that gap - see ActionButtonGump.IsHovered's doc
                // comment. Also don't touch lastSeenSelection during either window, so
                // the comparison re-evaluates correctly once clear instead of the
                // change getting silently lost.
                if (button.IsPressed || button.IsHovered)
                {
                    return;
                }

                BaseGameObject current = SelectedObject.LastObject;

                if (!ReferenceEquals(current, lastSeenSelection))
                {
                    lastSeenSelection = current;
                    PrimaryAction action = PrimaryActionResolver.Resolve(current);
                    button.SetDefinition(PrimaryActionDefinitionFactory.Create(action));
                }
            }
        }
    }
}
