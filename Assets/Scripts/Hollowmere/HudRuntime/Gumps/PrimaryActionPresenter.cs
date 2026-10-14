using System;
using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using Hollowmere.Hud;
using Hollowmere.Hud.Unity;
using Hollowmere.Interaction;
using Microsoft.Xna.Framework;

namespace Hollowmere.HudRuntime.Gumps
{
    /// <summary>
    /// Gump presenter for the context-sensitive Primary Action button. Wraps
    /// <see cref="ActionButtonGump"/> and the existing resolver/factory pipeline.
    /// </summary>
    public sealed class PrimaryActionPresenter : IHudWidgetPresenter, IHudWidgetPresenterTick
    {
        // Muted brick-red - distinct from ActionButtonGump's default dark-gray tint
        // and from ActionBar's own tint, without being a jarring saturated color.
        private static readonly Color PrimaryActionTint = new Color(120, 35, 30);

        private readonly string widgetId;
        private ActionButtonGump button;
        private BaseGameObject lastSeenSelection;
        private uint? lastSeenBackpackSerial;
        private bool lastItemHoldEnabled;

        public static int LiveInstanceCount { get; private set; }

        public PrimaryActionPresenter(HudWidgetDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            widgetId = definition.Id;
        }

        public string WidgetId => widgetId;

        public HudSize GetIntrinsicSize() => new HudSize(ActionButtonGump.ButtonWidth, ActionButtonGump.ButtonHeight);

        public void Apply(HudResolvedWidgetLayout layout)
        {
            if (layout.State != null && !layout.State.IsVisible)
            {
                if (button != null)
                {
                    button.IsVisible = false;
                }

                return;
            }

            EnsureMaterialized(layout.Bounds);

            if (button == null)
            {
                return;
            }

            button.IsVisible = true;
            button.X = (int)Math.Round(layout.Bounds.X);
            button.Y = (int)Math.Round(layout.Bounds.Y);
        }

        public void Tick()
        {
            if (button == null)
            {
                return;
            }

            if (ClassicUO.Client.Game == null || !(ClassicUO.Client.Game.Scene is GameScene))
            {
                TeardownGump();
                return;
            }

            if (ItemHold.Enabled != lastItemHoldEnabled)
            {
                lastItemHoldEnabled = ItemHold.Enabled;
            }

            if (button.IsPressed || button.IsHovered)
            {
                return;
            }

            BaseGameObject current = WorldTapTargeting.CurrentSelection;
            uint? backpackSerial = BackpackPresenter.SelectedSerial;

            // Re-resolve on either source changing - world selection alone isn't
            // enough, since tapping a backpack item while nothing's selected in the
            // world (the common case) leaves SelectedObject.LastObject unchanged.
            if (!ReferenceEquals(current, lastSeenSelection) || backpackSerial != lastSeenBackpackSerial)
            {
                lastSeenSelection = current;
                lastSeenBackpackSerial = backpackSerial;
                PrimaryAction action = PrimaryActionResolver.Resolve(current, backpackSerial);
                button.SetDefinition(PrimaryActionDefinitionFactory.Create(action));
            }
        }

        public void Dispose()
        {
            TeardownGump();
        }

        private void EnsureMaterialized(HudRect bounds)
        {
            if (button != null)
            {
                return;
            }

            if (ClassicUO.Client.Game == null || !(ClassicUO.Client.Game.Scene is GameScene))
            {
                return;
            }

            lastSeenBackpackSerial = BackpackPresenter.SelectedSerial;
            BaseGameObject current = WorldTapTargeting.CurrentSelection;
            PrimaryAction initial = PrimaryActionResolver.Resolve(current, lastSeenBackpackSerial);
            lastSeenSelection = current;

            int x = (int)Math.Round(bounds.X);
            int y = (int)Math.Round(bounds.Y);

            // Mobile UX slice: a distinct tint (existing ActionButtonGump/ActionBar
            // parameter, not a new capability) so this reads as *the* primary button
            // rather than one more Action Bar entry - "clearly accessible" without
            // resizing the Gump rendering shared with ActionBar's own buttons.
            // Mobile Action Feedback v1: enableTapFeedback is opt-in per instance -
            // only Primary Action requests it, so Action Bar's own buttons (built
            // without this argument) render exactly as they did before.
            button = new ActionButtonGump(PrimaryActionDefinitionFactory.Create(initial), x, y, PrimaryActionTint, enableTapFeedback: true);
            button.CanMove = false;
            UIManager.Add(button);
            LiveInstanceCount++;
            HudSmokeTestBridge.RegisterPrimaryActionLive(1);
        }

        private void TeardownGump()
        {
            if (button == null)
            {
                return;
            }

            button.Dispose();
            button = null;
            lastSeenSelection = null;
            lastSeenBackpackSerial = null;
            LiveInstanceCount = Math.Max(0, LiveInstanceCount - 1);
            HudSmokeTestBridge.RegisterPrimaryActionLive(-1);
        }
    }
}
