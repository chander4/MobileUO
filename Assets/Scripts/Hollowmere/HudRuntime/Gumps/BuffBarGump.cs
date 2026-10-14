using System;
using System.Collections.Generic;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.UI.Controls;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;

namespace Hollowmere.HudRuntime.Gumps
{
    /// <summary>
    /// Compact mobile status-effects strip: a read-only grid of the player's
    /// active buff/debuff icons, sourced entirely from the existing
    /// PlayerMobile.BuffIcons dictionary - the same collection the desktop
    /// BuffGump reads (see PacketHandlers.BuffDebuff, which populates it via
    /// World.Player.AddBuff/RemoveBuff, and BuffGump.BuildGump/AddBuff/RemoveBuff).
    /// Not a new status-effect model: every icon graphic (BuffIcon.Graphic,
    /// GumpsLoader art - same bridge SpellSkillItemView already uses),
    /// duration (BuffIcon.Timer, same Time.Ticks expiry math
    /// BuffGump.BuffControlEntry already performs), and description
    /// (BuffIcon.Text, already Cliloc-translated by the packet handler) is
    /// read as-is, never invented.
    ///
    /// Tap-to-reveal detail panel in place of desktop's hover tooltip - tap
    /// is the mobile-native equivalent (see ActionButtonGump's SetTooltip/#24
    /// note: icon-styled controls with a tooltip already work fine on touch,
    /// but a dedicated tap panel is the intended mobile-native interaction
    /// per the design manifesto's "modernize interactions" principle, not a
    /// hover emulation).
    /// </summary>
    internal sealed class BuffBarGump : Gump
    {
        internal const int IconSize = 28;
        private const int IconGap = 4;
        private const int Columns = 6;
        private const int Rows = 2;
        private const int GridWidth = Columns * (IconSize + IconGap);
        private const int GridHeight = Rows * (IconSize + IconGap);
        private const int DetailHeight = 44;

        public const int BarWidth = GridWidth;
        public const int BarHeight = GridHeight + DetailHeight;

        private readonly List<BuffIconControl> activeIcons = new List<BuffIconControl>();
        private readonly HashSet<BuffIconType> lastKeys = new HashSet<BuffIconType>();
        private readonly AlphaBlendControl detailBackground;
        private readonly Label detailLabel;
        private BuffIconType? detailShownFor;

        public BuffBarGump(int x, int y) : base(0, 0)
        {
            X = x;
            Y = y;
            CanMove = false;
            AcceptMouseInput = false;
            Width = BarWidth;
            Height = BarHeight;

            detailBackground = new AlphaBlendControl(0.35f)
            {
                X = 0,
                Y = GridHeight,
                Width = BarWidth,
                Height = DetailHeight,
                IsVisible = false
            };
            Add(detailBackground);

            detailLabel = new Label(string.Empty, true, 0x0386, BarWidth - 8, font: 1, ishtml: true)
            {
                X = 4,
                Y = GridHeight + 2,
                IsVisible = false
            };
            Add(detailLabel);
        }

        public void Refresh(PlayerMobile player)
        {
            if (player == null)
            {
                return;
            }

            // Rebuild only when the active buff *set* changed - icon controls
            // drive their own duration countdown per-frame via Update(), so a
            // per-frame rebuild here isn't needed (mirrors BuffGump.AddBuff
            // only firing when a type is genuinely new).
            bool changed = player.BuffIcons.Count != lastKeys.Count;
            if (!changed)
            {
                foreach (BuffIconType key in player.BuffIcons.Keys)
                {
                    if (!lastKeys.Contains(key))
                    {
                        changed = true;
                        break;
                    }
                }
            }

            if (!changed)
            {
                return;
            }

            foreach (BuffIconControl existing in activeIcons)
            {
                existing.Dispose();
            }
            activeIcons.Clear();
            lastKeys.Clear();

            int index = 0;
            bool detailStillValid = false;

            foreach (KeyValuePair<BuffIconType, BuffIcon> entry in player.BuffIcons)
            {
                lastKeys.Add(entry.Key);

                if (detailShownFor.HasValue && detailShownFor.Value == entry.Key)
                {
                    detailStillValid = true;
                }

                if (index >= Columns * Rows)
                {
                    continue;
                }

                int column = index % Columns;
                int row = index / Columns;

                var control = new BuffIconControl(entry.Value, OnIconTapped)
                {
                    X = column * (IconSize + IconGap),
                    Y = row * (IconSize + IconGap)
                };
                Add(control);
                activeIcons.Add(control);

                index++;
            }

            if (!detailStillValid)
            {
                HideDetail();
            }
        }

        private void OnIconTapped(BuffIcon icon)
        {
            if (detailShownFor.HasValue && detailShownFor.Value == icon.Type)
            {
                HideDetail();
                return;
            }

            detailShownFor = icon.Type;
            detailLabel.Text = icon.Text;
            detailLabel.IsVisible = true;
            detailBackground.IsVisible = true;
        }

        private void HideDetail()
        {
            detailShownFor = null;
            detailLabel.IsVisible = false;
            detailLabel.Text = string.Empty;
            detailBackground.IsVisible = false;
        }

        private sealed class BuffIconControl : GumpPic
        {
            private readonly BuffIcon icon;
            private readonly Action<BuffIcon> onTapped;
            private readonly uint expireAtTicks;
            private readonly Label durationLabel;

            public BuffIconControl(BuffIcon icon, Action<BuffIcon> onTapped) : base(0, 0, icon.Graphic, 0)
            {
                this.icon = icon;
                this.onTapped = onTapped;

                CanMove = false;
                AcceptMouseInput = true;

                // Mobile HUD Interaction Polish v1: Control.Update's base
                // implementation (external/ClassicUO Control.cs) auto-resizes
                // WIDTH/HEIGHT to wrap this control's own CHILDREN on its first
                // tick while WantUpdateSize is still true (the default), then
                // locks that size in forever. GumpPicBase's Graphic setter above
                // already set Width/Height from the real 28x28 texture - without
                // this, the very next Update() silently overwrites that with the
                // bounds of durationLabel alone (still empty text at that first
                // tick, so near-zero), permanently shrinking the icon to an
                // invisible, untappable sliver. Confirmed live: a real icon's
                // _bounds read back as {Width:0 Height:16} instead of 28x28, and
                // Control.HitTest's Bounds.Contains(...) can never match a
                // zero-width rect, so real touch never reached OnMouseUp (only
                // direct reflection invocation did, which is why this passed
                // earlier validation). Matches the same guard ActionButtonGump
                // already sets for the identical reason.
                WantUpdateSize = false;

                // Same expiry math as BuffGump.BuffControlEntry: icon.Timer is
                // the server-reported remaining-seconds value as of the last
                // BuffDebuff packet; <= 0 means indefinite (no countdown).
                expireAtTicks = icon.Timer <= 0
                    ? 0xFFFF_FFFF
                    : (uint)(ClassicUO.Time.Ticks + (uint)icon.Timer * 1000);

                if (expireAtTicks != 0xFFFF_FFFF)
                {
                    durationLabel = new Label(string.Empty, false, 1001, IconSize, font: 1,
                        style: FontStyle.BlackBorder,
                        align: TEXT_ALIGN_TYPE.TS_CENTER)
                    {
                        X = 0,
                        Y = IconSize - 12
                    };
                    Add(durationLabel);
                }
            }

            public override void Update(double totalMS, double frameMS)
            {
                base.Update(totalMS, frameMS);

                if (durationLabel == null)
                {
                    return;
                }

                double delta = expireAtTicks - totalMS;

                if (delta <= 0)
                {
                    durationLabel.Text = "0";
                    return;
                }

                TimeSpan span = TimeSpan.FromMilliseconds(delta);
                durationLabel.Text = span.Hours > 0
                    ? $"+{span.Hours}hr"
                    : span.Minutes > 0 ? $"{span.Minutes}:{span.Seconds:00}" : $"{span.Seconds}";
            }

            protected override void OnMouseUp(int x, int y, MouseButtonType button)
            {
                base.OnMouseUp(x, y, button);

                if (button == MouseButtonType.Left)
                {
                    onTapped?.Invoke(icon);
                }
            }
        }
    }
}
