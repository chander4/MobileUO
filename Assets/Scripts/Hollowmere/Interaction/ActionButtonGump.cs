using System;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using Hollowmere.Diagnostics;
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

        // #24: mutable (was readonly) so SetDefinition can swap the verb in place -
        // e.g. a context-sensitive primary-action button that re-labels itself as
        // Attack/Talk/Loot/Pick Up based on selection, without disposing/recreating
        // the Gump (which would churn its AnchorGroup membership for no reason).
        private GameActionDefinition definition;
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

        /// <summary>
        /// #24: true from OnMouseDown until OnMouseUp. See IsHovered - on its own
        /// this doesn't close the whole gap, because the engine's hover event fires
        /// before the down event does. Kept as a separate flag from IsHovered (rather
        /// than merging them) since IsPressed also drives the long-press timer via
        /// mouseDownTick, which only makes sense once actually pressed.
        /// </summary>
        public bool IsPressed { get; private set; }

        /// <summary>
        /// #24: true from OnMouseEnter until OnMouseExit. GameScene.Update nulls
        /// SelectedObject.LastObject every frame the touch isn't over the game
        /// viewport (external/ClassicUO/src/Game/Scenes/GameScene.cs:607) - true the
        /// instant a finger is anywhere over this button, since it's UI, not the
        /// viewport. Confirmed via logcat that OnMouseEnter fires (still showing the
        /// correct verb) *before* OnMouseDown - so a guard that only started at
        /// IsPressed still left a gap between hover and press where a context-
        /// sensitive button watching SelectedObject (see PrimaryActionButtonBootstrap)
        /// would see the null, swap itself to neutral, and have OnMouseDown/Up fire
        /// against that neutral definition instead of the one that was showing a
        /// moment earlier. Callers must skip SetDefinition while EITHER this or
        /// IsPressed is true.
        /// </summary>
        public bool IsHovered { get; private set; }

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

            RebuildContent();

            backgroundTexture = Texture2DCache.GetTexture(normalColor);
        }

        // Split out of BuildGump so SetDefinition can re-run just the content part
        // (icon/label/tooltip) without touching Width/Height/position/AnchorGroup
        // membership, which must stay untouched across a verb swap.
        private void RebuildContent()
        {
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
                SetTooltip(definition.Tooltip ?? definition.Label);
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

                // #24: deliberately NOT calling SetTooltip here, even though
                // GameActionDefinition.Tooltip carries a value for these buttons
                // (Attack/Talk/Loot/Pick Up). Confirmed on-device: a text-label button
                // with a tooltip attached breaks tap-to-execute on touch - OnMouseUp's
                // drag-tolerance check (Mouse.LDroppedOffset, 5px) appears to get
                // tripped by the tooltip's hover/dwell behavior, so taps get read as a
                // drag and TryExecute() never runs. Icon-styled buttons (spells) have
                // always called SetTooltip above and work fine - this is specifically a
                // label+tooltip combination, which no button used before #24. Tooltip
                // stays on the definition as metadata for a future presentation (e.g. a
                // proper on-select label under the button) rather than this mechanism.
            }
        }

        /// <summary>
        /// #24: swaps this button's verb in place - same screen position, same
        /// AnchorGroup membership, no dispose/recreate. Used by a context-sensitive
        /// primary-action button whose GameActionDefinition changes as the player's
        /// selection changes; every other current consumer (Target Last, spells,
        /// skills, phrases) still just sets its definition once at construction.
        /// </summary>
        public void SetDefinition(GameActionDefinition newDefinition)
        {
            if (Diag.IsEnabled(DiagCategory.UI))
                Diag.Log(DiagCategory.UI, $"SetDefinition '{definition?.Label}'->'{newDefinition?.Label}' IsHovered={IsHovered} IsPressed={IsPressed}");

            definition = newDefinition ?? throw new ArgumentNullException(nameof(newDefinition));

            label?.Dispose();
            label = null;
            icon?.Dispose();
            icon = null;

            RebuildContent();
        }

        protected override void OnMouseEnter(int x, int y)
        {
            IsHovered = true;
            if (Diag.IsEnabled(DiagCategory.UI))
                Diag.Log(DiagCategory.UI, $"Enter label='{definition?.Label}'");

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
            IsHovered = false;

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
                IsPressed = true;
                if (Diag.IsEnabled(DiagCategory.UI))
                    Diag.Log(DiagCategory.UI, $"Down label='{definition?.Label}'");
            }
        }

        protected override void OnMouseUp(int x, int y, MouseButtonType button)
        {
            base.OnMouseUp(x, y, MouseButtonType.Left);

            if (button != MouseButtonType.Left)
            {
                return;
            }

            // Read IsPressed before clearing it below - TryExecute() itself must still
            // run using whatever definition was captured at press-time, not whatever
            // SetDefinition would swap in now that IsPressed is about to go false.
            IsPressed = false;

            Point offset = Mouse.LDroppedOffset;
            if (Diag.IsEnabled(DiagCategory.UI))
                Diag.Log(DiagCategory.UI, $"Up label='{definition?.Label}' offset=({offset.X},{offset.Y})");

            if (Math.Abs(offset.X) >= 5 || Math.Abs(offset.Y) >= 5)
            {
                return;
            }

            if (OnLongPress != null && ClassicUO.Time.Ticks - mouseDownTick >= LongPressMillis)
            {
                OnLongPress();
                return;
            }

            bool executed = definition.TryExecute();
            if (Diag.IsEnabled(DiagCategory.UI))
                Diag.Log(DiagCategory.UI, $"TryExecute={executed} label='{definition?.Label}'");
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
