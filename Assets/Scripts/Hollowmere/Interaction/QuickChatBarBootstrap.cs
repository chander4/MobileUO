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
        // internal, not private: ActionCatalog's "Reset Action Bars" entry needs the
        // same key to clear the right PlayerPrefs slot.
        internal const string PersistenceKey = "HollowmereActionBar.QuickChat";

        // internal, not private: same reasoning as ActionBarBootstrap.Current.
        internal static ActionBar Current { get; private set; }

        // Right-docked, same reasoning as ActionBarBootstrap (left half of the screen is
        // the movement joystick's zone) - stacked above the Main bar's row, growing
        // leftward from the same right-edge origin.
        private const int RightMargin = 20;
        private const int BottomMargin = 24;
        private const int ButtonWidth = 88;
        private const int ButtonHeight = 44;
        private const int RowGap = 12;

        // See ActionBarBootstrap.MaxRowWidthFraction - same reasoning, this bar has even
        // more fixed buttons (Chat + "+", plus however many phrases remain) before any
        // picker additions.
        private const float MaxRowWidthFraction = 0.45f;

        // Distinct from the Main bar's default look (plain dark gray) so the two are
        // told apart at a glance - see ActionBar's tint doc comment.
        private static readonly Microsoft.Xna.Framework.Color Tint = new Microsoft.Xna.Framework.Color(20, 35, 60);

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

        // Phrases are removable/persisted like picker-added actions (#22 D4), but
        // aren't sourced from ActionCatalog - they're this bootstrap's own hardcoded
        // defaults. Checked first, falling back to ActionCatalog for anything the
        // player added via the picker (a QuickChat bar can hold spells/skills too).
        private static bool TryResolveRef(string refId, out GameActionDefinition definition)
        {
            foreach (var (label, phrase) in Phrases)
            {
                if (refId == "phrase:" + phrase)
                {
                    definition = new GameActionDefinition(label, () => GameActions.Say(phrase), catalogRefId: refId);
                    return true;
                }
            }

            if (ActionCatalog.TryResolve(refId, out CatalogEntry entry))
            {
                definition = entry.Definition;
                return true;
            }

            definition = null;
            return false;
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

                    // #22 D4: read before any AddButton call touches persistence - see
                    // ActionBarBootstrap for why this has to happen first. hasSavedContents
                    // distinguishes "never saved, seed the hardcoded phrase defaults" from
                    // "saved (possibly as an empty list) because the player removed some or
                    // all of them" - those need different behavior, not just an empty list
                    // either way.
                    bool hasSavedContents = persistence.TryLoadContents(PersistenceKey, out System.Collections.Generic.IReadOnlyList<string> savedContents);

                    int viewportWidth = ClassicUO.Client.Game.Window.ClientBounds.Width;
                    int defaultX = viewportWidth - ButtonWidth - RightMargin;
                    int defaultY = ClassicUO.Client.Game.Window.ClientBounds.Height - ButtonHeight - BottomMargin - ButtonHeight - RowGap;
                    int maxPerRow = Mathf.Max(1, Mathf.FloorToInt(viewportWidth * MaxRowWidthFraction / ButtonWidth));
                    bar = new ActionBar(PersistenceKey, ActionBarLayout.HorizontalRightToLeft, defaultX, defaultY, persistence, maxPerRow, Tint);
                    Current = bar;

                    if (!hasSavedContents)
                    {
                        foreach (var (label, phrase) in Phrases)
                        {
                            bar.AddButton(new GameActionDefinition(label, () => GameActions.Say(phrase), catalogRefId: "phrase:" + phrase), removable: true);
                        }
                    }
                    else
                    {
                        foreach (string refId in savedContents)
                        {
                            if (TryResolveRef(refId, out GameActionDefinition definition))
                            {
                                bar.AddButton(definition, removable: true);
                            }
                        }
                    }

                    // Explicit trigger for chat input, now that the Say text box is
                    // hidden by default on mobile (ClientRunner.OnProfileLoaded sets
                    // ActivateChatAfterEnter) - was an easy accidental-tap target and
                    // its IsActive flag doesn't toggle cleanly. Opens a small dedicated
                    // popup (ChatPopupPresenter) instead of reusing that strip at all.
                    // Not removable - always the one guaranteed way to reach chat.
                    bar.AddButton(new GameActionDefinition("Chat", () => ChatPopupPresenter.Instance?.Open()));

                    // #22 D5: persistent "+" trigger, same as the Main bar. Not removable -
                    // the only way to reach the picker for this bar.
                    bar.AddButton(new GameActionDefinition("+", () => ActionPickerPresenter.Instance?.Open(bar)));

                    return;
                }

                if (ClassicUO.Client.Game == null || !(ClassicUO.Client.Game.Scene is GameScene))
                {
                    bar = null;
                    Current = null;
                    return;
                }

                bar.Tick();
            }
        }
    }
}
