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
        // #22 D5: long-press-to-remove. Gump-land has no LeanTouch/LongPressDetector
        // (that's Unity UI only - see #21A) - held-duration is checked directly against
        // ClassicUO's own frame clock, the same source AnchorableGump/Control already use.
        private const uint LongPressMillis = 500;

        private readonly GameActionDefinition definition;
        private readonly Color normalColor;
        private readonly Color hoverColor;
        private Texture2D backgroundTexture;
        private Label label;
        private GumpPic icon;
        private uint mouseDownTick;

        /// <summary>
        /// Set by whoever owns this button's lifecycle (ActionBar, for removable
        /// buttons) to react to a long-press instead of a normal tap. Null means
        /// long-press does nothing - e.g. the bar's fixed "+"/default buttons.
        /// </summary>
        public Action OnLongPress { get; set; }

        /// <summary>
        /// Exposed so ActionBar can snapshot+rebuild its buttons when switching
        /// orientation (SetLayout) - the definition itself isn't reconstructible
        /// from Gump state, so the owning bar needs the original object back.
        /// </summary>
        public GameActionDefinition Definition => definition;

        public ActionButtonGump(GameActionDefinition definition, int x, int y, Color? barTint = null) : this(definition, barTint)
        {
            X = x;
            Y = y;
            BuildGump();
        }

        public ActionButtonGump(GameActionDefinition definition, Color? barTint = null) : base(0, 0)
        {
            this.definition = definition ?? throw new ArgumentNullException(nameof(definition));

            // Lets two bars (e.g. Main vs QuickChat) read as visually distinct at a
            // glance without needing a separate always-visible label Gump, which would
            // need its own dynamic repositioning as rows wrap/grow - a per-button tint
            // has none of that complexity and survives dragging/wrapping for free.
            normalColor = barTint ?? new Color(30, 30, 30);
            hoverColor = barTint.HasValue ? Color.Lerp(barTint.Value, Color.White, 0.35f) : Color.DimGray;

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

            // Icon-first when the action has one (spells - see ActionCatalog.GetSpells)
            // - a real hotbar reads by icon, not by reading text on a 44px-tall button.
            // Skills/pet commands don't carry an art ID today, so they keep the text
            // label as a fallback instead of showing nothing.
            if (definition.IconGraphic.HasValue)
            {
                icon = new GumpPic((Width - 44) >> 1, 0, definition.IconGraphic.Value, 0);

                // GumpPic.Draw scales to whatever Width/Height are set to, not the source
                // art's native size - clamp explicitly so a mismatched/large graphic ID
                // (e.g. a spellbook-page icon instead of a hotbar one) can never blow past
                // the button's own bounds again.
                icon.Width = 44;
                icon.Height = 44;

                Add(icon);
                SetTooltip(definition.Label);
            }
            else
            {
                label = new Label(definition.Label, true, 1001, Width, 255, FontStyle.BlackBorder, TEXT_ALIGN_TYPE.TS_CENTER)
                {
                    X = 0,
                    Width = Width - 10,
                };
                label.Y = (Height >> 1) - (label.Height >> 1);
                Add(label);
            }

            backgroundTexture = Texture2DCache.GetTexture(normalColor);
        }

        protected override void OnMouseEnter(int x, int y)
        {
            ushort hue = definition.IsAvailable ? (ushort) 53 : (ushort) 1001;

            if (label != null)
            {
                label.Hue = hue;
            }

            if (icon != null)
            {
                icon.Hue = hue;
            }

            backgroundTexture = Texture2DCache.GetTexture(hoverColor);
            base.OnMouseEnter(x, y);
        }

        protected override void OnMouseExit(int x, int y)
        {
            if (label != null)
            {
                label.Hue = 1001;
            }

            if (icon != null)
            {
                icon.Hue = 0;
            }

            backgroundTexture = Texture2DCache.GetTexture(normalColor);
            base.OnMouseExit(x, y);
        }

        protected override void OnMouseDown(int x, int y, MouseButtonType button)
        {
            base.OnMouseDown(x, y, button);

            if (button == MouseButtonType.Left)
            {
                mouseDownTick = ClassicUO.Time.Ticks;
            }
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            base.OnMouseUp(x, y, MouseButtonType.Left);

            Point offset = Mouse.LDroppedOffset;

            if (button != MouseButtonType.Left || Math.Abs(offset.X) >= 5 || Math.Abs(offset.Y) >= 5)
            {
                return;
            }

            if (OnLongPress != null && ClassicUO.Time.Ticks - mouseDownTick >= LongPressMillis)
            {
                OnLongPress();
                return;
            }

            definition.TryExecute();
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
