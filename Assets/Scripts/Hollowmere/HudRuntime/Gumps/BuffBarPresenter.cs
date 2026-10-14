using System;
using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using Hollowmere.Hud;
using Hollowmere.Hud.Unity;

namespace Hollowmere.HudRuntime.Gumps
{
    /// <summary>
    /// Gump presenter for the compact mobile status-effects strip. Visible
    /// only while the player has at least one active buff/debuff in
    /// PlayerMobile.BuffIcons - same appear/disappear-with-state shape as
    /// TargetVitalsPresenter (visible only while there's a resolved target),
    /// not a permanent fixture like Vitals/Stats.
    /// </summary>
    public sealed class BuffBarPresenter : IHudWidgetPresenter, IHudWidgetPresenterTick
    {
        public const int BuffBarWidth = BuffBarGump.BarWidth;
        public const int BuffBarHeight = BuffBarGump.BarHeight;

        private readonly string widgetId;
        private BuffBarGump gump;
        private bool layoutVisible = true;
        private HudRect lastBounds;

        public static int LiveInstanceCount { get; private set; }

        public BuffBarPresenter(HudWidgetDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            widgetId = definition.Id;
        }

        public string WidgetId => widgetId;

        public HudSize GetIntrinsicSize() => new HudSize(BuffBarWidth, BuffBarHeight);

        public void Apply(HudResolvedWidgetLayout layout)
        {
            layoutVisible = layout.State == null || layout.State.IsVisible;
            lastBounds = layout.Bounds;

            if (gump == null)
            {
                return;
            }

            gump.X = (int)Math.Round(layout.Bounds.X);
            gump.Y = (int)Math.Round(layout.Bounds.Y);
            gump.IsVisible = layoutVisible && HasActiveBuffs();
        }

        public void Tick()
        {
            if (ClassicUO.Client.Game == null || !(ClassicUO.Client.Game.Scene is GameScene))
            {
                TeardownGump();
                return;
            }

            PlayerMobile player = World.Player;
            if (player == null || player.IsDestroyed)
            {
                return;
            }

            if (player.BuffIcons.Count == 0)
            {
                TeardownGump();
                return;
            }

            EnsureMaterialized();

            if (gump == null)
            {
                return;
            }

            gump.IsVisible = layoutVisible;
            gump.Refresh(player);
        }

        public void Dispose()
        {
            TeardownGump();
        }

        private static bool HasActiveBuffs()
        {
            PlayerMobile player = World.Player;
            return player != null && !player.IsDestroyed && player.BuffIcons.Count > 0;
        }

        private void EnsureMaterialized()
        {
            if (gump != null)
            {
                return;
            }

            if (World.Player == null)
            {
                return;
            }

            int x = (int)Math.Round(lastBounds.X);
            int y = (int)Math.Round(lastBounds.Y);

            gump = new BuffBarGump(x, y);
            UIManager.Add(gump);
            LiveInstanceCount++;
            HudSmokeTestBridge.RegisterStatusEffectsLive(1);
        }

        private void TeardownGump()
        {
            if (gump == null)
            {
                return;
            }

            gump.Dispose();
            gump = null;
            LiveInstanceCount = Math.Max(0, LiveInstanceCount - 1);
            HudSmokeTestBridge.RegisterStatusEffectsLive(-1);
        }
    }
}
