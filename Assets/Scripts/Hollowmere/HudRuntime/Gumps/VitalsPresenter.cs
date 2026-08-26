using System;
using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps;
using Hollowmere.Hud;
using Hollowmere.Hud.Unity;

namespace Hollowmere.HudRuntime.Gumps
{
    /// <summary>
    /// Gump presenter for always-on player vitals (health, mana, stamina).
    /// </summary>
    public sealed class VitalsPresenter : IHudWidgetPresenter, IHudWidgetPresenterTick
    {
        public const int VitalsWidth = HealthBarGumpCustom.HPB_WIDTH;
        public const int VitalsHeight = HealthBarGumpCustom.HPB_HEIGHT_MULTILINE;

        private readonly string widgetId;
        private PlayerVitalsGump gump;
        private bool requestedInitialStatus;

        public static int LiveInstanceCount { get; private set; }

        public VitalsPresenter(HudWidgetDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            widgetId = definition.Id;
        }

        public string WidgetId => widgetId;

        public HudSize GetIntrinsicSize() => new HudSize(VitalsWidth, VitalsHeight);

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

            Mobile player = World.Player;
            if (player == null || player.IsDestroyed)
            {
                return;
            }

            if (!requestedInitialStatus && player.HitsMax == 0)
            {
                GameActions.RequestMobileStatus(player);
                requestedInitialStatus = true;
            }
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

            gump = new PlayerVitalsGump(x, y);
            UIManager.Add(gump);
            requestedInitialStatus = false;
            LiveInstanceCount++;
            HudSmokeTestBridge.RegisterVitalsLive(1);
        }

        private void TeardownGump()
        {
            if (gump == null)
            {
                return;
            }

            gump.Dispose();
            gump = null;
            requestedInitialStatus = false;
            LiveInstanceCount = Math.Max(0, LiveInstanceCount - 1);
            HudSmokeTestBridge.RegisterVitalsLive(-1);
        }
    }
}
