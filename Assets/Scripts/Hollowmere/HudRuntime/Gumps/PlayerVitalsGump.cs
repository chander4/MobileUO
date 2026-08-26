using ClassicUO.Game;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;

namespace Hollowmere.HudRuntime.Gumps
{
    /// <summary>
    /// Multiline player health/mana/stamina bar for the HUD. Reuses
    /// <see cref="HealthBarGumpCustom"/> rendering and update logic.
    /// </summary>
    internal sealed class PlayerVitalsGump : HealthBarGumpCustom
    {
        public PlayerVitalsGump(int x, int y)
            : base(World.Player)
        {
            X = x;
            Y = y;
            CanMove = false;
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            // HUD vitals stay visible; skip BaseHealthBarGump long-press-to-close.
        }
    }
}
