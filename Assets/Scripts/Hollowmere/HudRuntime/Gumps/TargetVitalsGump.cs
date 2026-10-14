using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using Hollowmere.Hud;
using Hollowmere.Interaction;

namespace Hollowmere.HudRuntime.Gumps
{
    /// <summary>
    /// Multiline health/mana/stamina bar for the current combat target. Same wrap as
    /// <see cref="PlayerVitalsGump"/>, generalized to any Entity via
    /// HealthBarGumpCustom(Entity) rather than the World.Player-only base constructor -
    /// the underlying Gump already tracks its entity by serial and re-resolves it every
    /// frame (see HealthBarGump.cs), so no per-frame re-wiring is needed here.
    ///
    /// #34 follow-up: adds a "how far away is this" readout below the bar. UO already
    /// computes this as GameObject.Distance (whole tiles, Chebyshev distance from
    /// World.Player - external/ClassicUO/src/Game/GameObjects/GameObject.cs) - this
    /// class only displays it. Label row styled after PlayerStatsGump's existing
    /// text-row pattern for visual consistency with the rest of the HUD.
    ///
    /// ADR-009 rule 1 validation: the distance label's color also reflects
    /// PrimaryActionResolver's existing actionability classification for this target
    /// (Attack/Talk/Loot/PickUp/None) - not a second "importance" system, just the
    /// existing answer to "what can I do about this" read for presentation instead of
    /// only for the Primary Action button. Deliberately minimal: a color swap on an
    /// element that already exists, not a new visual element or a general scheme.
    /// </summary>
    internal sealed class TargetVitalsGump : HealthBarGumpCustom
    {
        // internal, not private: TargetVitalsPresenter.TargetVitalsHeight needs
        // this to report the gump's real rendered height (see that constant).
        internal const int DistanceLabelHeight = 16;
        private const ushort DistanceTextHue = 0x0386;

        // Reuses the engine's existing alert/attention hue (see e.g. Profile.EmoteHue/
        // YellHue, GameCursor.cs, LoginGump's field-error color) rather than inventing
        // a new color - same value already means "pay attention to this" elsewhere in
        // this codebase.
        private const ushort AlertTextHue = 0x0021;

        private readonly Label distanceLabel;

        public TargetVitalsGump(Entity target, int x, int y)
            : base(target)
        {
            X = x;
            Y = y;
            CanMove = false;

            distanceLabel = new Label(string.Empty, false, DistanceTextHue, HPB_WIDTH - 8, font: 1)
            {
                X = 4,
                Y = HPB_HEIGHT_MULTILINE
            };
            Add(distanceLabel);

            Height = HPB_HEIGHT_MULTILINE + DistanceLabelHeight;
        }

        public void SetDistanceTiles(int tiles)
        {
            distanceLabel.Text = HudDistanceFormatter.FormatTiles(tiles);
        }

        // Attack is the only tier that means "this can hurt you" - the one distinction
        // worth a visual difference here. Talk/Loot/PickUp/None all keep the existing
        // neutral color; PrimaryActionResolver only ever returns Loot/PickUp for a
        // corpse/ground-item target, which this Mobile-only widget never resolves to
        // in practice, so treating them the same as the neutral default is correct,
        // not an oversight.
        public void SetActionability(PrimaryActionType actionType)
        {
            distanceLabel.Hue = actionType == PrimaryActionType.Attack ? AlertTextHue : DistanceTextHue;
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            // HUD-managed widget, not player-closeable - same reasoning as PlayerVitalsGump.
        }
    }
}
