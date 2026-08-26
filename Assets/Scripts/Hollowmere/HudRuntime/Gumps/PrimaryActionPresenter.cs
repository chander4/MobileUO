using System;
using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using Hollowmere.Hud;
using Hollowmere.Hud.Unity;
using Hollowmere.Interaction;

namespace Hollowmere.HudRuntime.Gumps
{
    /// <summary>
    /// Gump presenter for the context-sensitive Primary Action button. Wraps
    /// <see cref="ActionButtonGump"/> and the existing resolver/factory pipeline.
    /// </summary>
    public sealed class PrimaryActionPresenter : IHudWidgetPresenter, IHudWidgetPresenterTick
    {
        public const int ButtonWidth = 88;
        public const int ButtonHeight = 44;

        private readonly string widgetId;
        private ActionButtonGump button;
        private BaseGameObject lastSeenSelection;
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

        public HudSize GetIntrinsicSize() => new HudSize(ButtonWidth, ButtonHeight);

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

            BaseGameObject current = SelectedObject.LastObject;

            if (!ReferenceEquals(current, lastSeenSelection))
            {
                lastSeenSelection = current;
                PrimaryAction action = PrimaryActionResolver.Resolve(current);
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

            PrimaryAction initial = PrimaryActionResolver.Resolve(SelectedObject.LastObject);
            lastSeenSelection = SelectedObject.LastObject;

            int x = (int)Math.Round(bounds.X);
            int y = (int)Math.Round(bounds.Y);

            button = new ActionButtonGump(PrimaryActionDefinitionFactory.Create(initial), x, y);
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
            LiveInstanceCount = Math.Max(0, LiveInstanceCount - 1);
            HudSmokeTestBridge.RegisterPrimaryActionLive(-1);
        }
    }
}
