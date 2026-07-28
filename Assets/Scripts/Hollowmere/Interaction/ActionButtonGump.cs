using System;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Hollowmere.Interaction
{
    /// <summary>
    /// Reusable touch-friendly Gump button that executes a supplied
    /// GameActionDefinition. Knows nothing about what a specific action
    /// does - Target Last is only the first definition wired to an
    /// instance of this class; future actions are new definitions, not
    /// new button types.
    ///
    /// Matches AssistantHotkeyButtonGump's dimensions/anchor setup
    /// (Assets/Scripts/AssistantHotkeyButtonGump.cs, not modified) so it
    /// drops into the same AnchorManager grid-snap ecosystem as existing
    /// Assistant buttons.
    ///
    /// Not overriding GumpType leaves it at the base class default
    /// (GUMP_TYPE.NONE), so CanBeSaved is false - this button is not
    /// persisted across sessions. That's deliberate: a GameActionDefinition
    /// carries a C# delegate that can't be meaningfully reconstructed from
    /// a saved XML layout, so there is nothing safe to restore.
    /// </summary>
    internal class ActionButtonGump : AnchorableGump
    {
        private readonly GameActionDefinition definition;
        private Texture2D backgroundTexture;
        private Label label;

        public ActionButtonGump(GameActionDefinition definition, int x, int y) : this(definition)
        {
            X = x;
            Y = y;
            BuildGump();
        }

        public ActionButtonGump(GameActionDefinition definition) : base(0, 0)
        {
            this.definition = definition ?? throw new ArgumentNullException(nameof(definition));

            CanMove = true;
            AcceptMouseInput = true;
            CanCloseWithRightClick = true;
            WantUpdateSize = false;
            WidthMultiplier = 2;
            HeightMultiplier = 1;
            GroupMatrixWidth = 44;
            GroupMatrixHeight = 44;
            AnchorType = ANCHOR_TYPE.SPELL;
        }

        private void BuildGump()
        {
            Width = 88;
            Height = 44;

            label = new Label(definition.Label, true, 1001, Width, 255, FontStyle.BlackBorder, TEXT_ALIGN_TYPE.TS_CENTER)
            {
                X = 0,
                Width = Width - 10,
            };
            label.Y = (Height >> 1) - (label.Height >> 1);
            Add(label);

            backgroundTexture = Texture2DCache.GetTexture(new Color(30, 30, 30));
        }

        protected override void OnMouseEnter(int x, int y)
        {
            label.Hue = definition.IsAvailable ? (ushort) 53 : (ushort) 1001;
            backgroundTexture = Texture2DCache.GetTexture(Color.DimGray);
            base.OnMouseEnter(x, y);
        }

        protected override void OnMouseExit(int x, int y)
        {
            label.Hue = 1001;
            backgroundTexture = Texture2DCache.GetTexture(new Color(30, 30, 30));
            base.OnMouseExit(x, y);
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            base.OnMouseUp(x, y, MouseButtonType.Left);

            Point offset = Mouse.LDroppedOffset;

            if (button == MouseButtonType.Left && Math.Abs(offset.X) < 5 && Math.Abs(offset.Y) < 5)
            {
                definition.TryExecute();
            }
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            ResetHueVector();
            _hueVector.Z = definition.IsAvailable ? 0.1f : 0.5f;

            batcher.Draw2D(backgroundTexture, x, y, Width, Height, ref _hueVector);

            _hueVector.Z = 0;
            batcher.DrawRectangle(Texture2DCache.GetTexture(Color.Gray), x, y, Width, Height, ref _hueVector);

            base.Draw(batcher, x, y);
            return true;
        }
    }
}
