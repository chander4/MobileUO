using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using Microsoft.Xna.Framework;

namespace Hollowmere.HudRuntime.Gumps
{
    /// <summary>
    /// Compact, read-only character-identity panel: Strength/Dexterity/Intelligence,
    /// carry weight, and gold. Not a character sheet - no editing, no paperdoll, no
    /// skills. A new Hollowmere-owned Gump rather than a wrap of the engine's
    /// StatusGumpModern/StatusGumpOld (external/ClassicUO), which are full desktop
    /// character sheets and not a clean fit for this compact mobile widget.
    /// </summary>
    internal sealed class PlayerStatsGump : Gump
    {
        public const int StatsWidth = 160;
        public const int StatsHeight = 112;

        private const int RowHeight = 20;
        private const ushort TextHue = 0x0386;

        private readonly Label strengthLabel;
        private readonly Label dexterityLabel;
        private readonly Label intelligenceLabel;
        private readonly Label weightLabel;
        private readonly Label goldLabel;

        public PlayerStatsGump(int x, int y) : base(0, 0)
        {
            X = x;
            Y = y;
            CanMove = false;
            AcceptMouseInput = false;
            Width = StatsWidth;
            Height = StatsHeight;

            Add(new AlphaBlendControl(0.3f) { Width = Width, Height = Height });

            strengthLabel = AddRow(0);
            dexterityLabel = AddRow(1);
            intelligenceLabel = AddRow(2);
            weightLabel = AddRow(3);
            goldLabel = AddRow(4);
        }

        private Label AddRow(int rowIndex)
        {
            var label = new Label(string.Empty, false, TextHue, StatsWidth - 8, font: 1)
            {
                X = 4,
                Y = 4 + (rowIndex * RowHeight)
            };
            Add(label);
            return label;
        }

        public void Refresh(PlayerMobile player)
        {
            if (player == null)
            {
                return;
            }

            strengthLabel.Text = $"Str: {player.Strength}";
            dexterityLabel.Text = $"Dex: {player.Dexterity}";
            intelligenceLabel.Text = $"Int: {player.Intelligence}";
            weightLabel.Text = $"Weight: {player.Weight}/{player.WeightMax}";
            goldLabel.Text = $"Gold: {player.Gold}";
        }
    }
}
