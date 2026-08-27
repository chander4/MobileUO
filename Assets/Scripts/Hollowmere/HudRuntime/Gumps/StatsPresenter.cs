using System;
using ClassicUO.Game;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using Hollowmere.Hud;
using Hollowmere.Hud.Unity;

namespace Hollowmere.HudRuntime.Gumps
{
    /// <summary>
    /// Gump presenter for the compact player stats panel (Str/Dex/Int/Weight/Gold).
    /// </summary>
    public sealed class StatsPresenter : IHudWidgetPresenter, IHudWidgetPresenterTick
    {
        public const int StatsWidth = PlayerStatsGump.StatsWidth;
        public const int StatsHeight = PlayerStatsGump.StatsHeight;

        private readonly string widgetId;
        private PlayerStatsGump gump;

        public static int LiveInstanceCount { get; private set; }

        public StatsPresenter(HudWidgetDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            widgetId = definition.Id;
        }

        public string WidgetId => widgetId;

        public HudSize GetIntrinsicSize() => new HudSize(StatsWidth, StatsHeight);

        public void Apply(HudResolvedWidgetLayout layout)
        {
            if (layout.State != null && !layout.State.IsVisible)
            {
                if (gump != null)
                {
                    gump.IsVisible = false;
                }

                return;
            }

            EnsureMaterialized(layout.Bounds);

            if (gump == null)
            {
                return;
            }

            gump.IsVisible = true;
            gump.X = (int)Math.Round(layout.Bounds.X);
            gump.Y = (int)Math.Round(layout.Bounds.Y);
        }

        public void Tick()
        {
            if (gump == null)
            {
                return;
            }

            if (ClassicUO.Client.Game == null || !(ClassicUO.Client.Game.Scene is GameScene))
            {
                TeardownGump();
                return;
            }

            var player = World.Player;
            if (player == null || player.IsDestroyed)
            {
                return;
            }

            gump.Refresh(player);
        }

        public void Dispose()
        {
            TeardownGump();
        }

        private void EnsureMaterialized(HudRect bounds)
        {
            if (gump != null)
            {
                return;
            }

            if (ClassicUO.Client.Game == null || !(ClassicUO.Client.Game.Scene is GameScene) || World.Player == null)
            {
                return;
            }

            int x = (int)Math.Round(bounds.X);
            int y = (int)Math.Round(bounds.Y);

            gump = new PlayerStatsGump(x, y);
            gump.Refresh(World.Player);
            UIManager.Add(gump);
            LiveInstanceCount++;
            HudSmokeTestBridge.RegisterStatsLive(1);
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
            HudSmokeTestBridge.RegisterStatsLive(-1);
        }
    }
}
