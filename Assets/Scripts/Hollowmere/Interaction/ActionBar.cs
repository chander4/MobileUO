using System;
using System.Collections.Generic;
using ClassicUO.Game.Managers;
using Microsoft.Xna.Framework;

namespace Hollowmere.Interaction
{
    public enum ActionBarLayout
    {
        Horizontal,
        Vertical,

        // Grows leftward from origin instead of rightward - for a bar docked to the
        // right edge of the screen (thumb-reachable action zone; movement joystick
        // owns the left half via LeftTouchZone), so adding buttons extends away from
        // the edge instead of running off it.
        HorizontalRightToLeft
    }

    /// <summary>
    /// Arranges a sequence of ActionButtonGumps using UIManager.AnchorManager
    /// - the same AnchorGroup/AnchorControlAt primitives a manual drag-to-anchor
    /// already uses (see AnchorManager.DropControl), so bars built from this
    /// class interoperate with existing Assistant buttons through the same
    /// shared anchor state instead of a parallel layout system. Not a
    /// MonoBehaviour - like TargetLastButtonBootstrap, it orchestrates
    /// ClassicUO/XNA-side Gump objects directly, which aren't Unity objects.
    /// </summary>
    internal class ActionBar
    {
        private readonly string persistenceKey;
        private ActionBarLayout layout;
        private readonly IActionBarPersistence persistence;
        private readonly List<ActionButtonGump> buttons = new List<ActionButtonGump>();

        // Caps how many buttons grow in the "long" direction before wrapping to a new
        // row/column - unbounded growth on a Horizontal/HorizontalRightToLeft bar would
        // eventually run back across the screen into the movement joystick's zone (the
        // exact problem right-docking was meant to fix), just from the picker letting
        // players keep adding actions instead of dragging. int.MaxValue = no wrapping,
        // matching the old unbounded behavior for any future bar that doesn't need it.
        private readonly int maxPerRow;

        // Visual identity for this bar - every button it creates gets this tint, so two
        // bars read as distinct at a glance regardless of drag position or row-wrap
        // state (a one-off label Gump would need constant repositioning as rows grow;
        // a per-button tint doesn't).
        private readonly Color? tint;

        private Point origin;
        private Point lastPersistedPosition;

        public IReadOnlyList<ActionButtonGump> Buttons => buttons;
        public int Count => buttons.Count;
        public ActionBarLayout Layout => layout;

        public ActionBar(string persistenceKey, ActionBarLayout layout, int defaultX, int defaultY, IActionBarPersistence persistence, int maxPerRow = int.MaxValue, Color? tint = null)
        {
            this.persistenceKey = persistenceKey ?? throw new ArgumentNullException(nameof(persistenceKey));
            this.persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
            this.maxPerRow = maxPerRow > 0 ? maxPerRow : int.MaxValue;
            this.tint = tint;

            this.layout = persistence.TryLoadLayout(persistenceKey, out ActionBarLayout loadedLayout)
                ? loadedLayout
                : layout;

            origin = persistence.TryLoadPosition(persistenceKey, out Point loaded)
                ? loaded
                : new Point(defaultX, defaultY);

            lastPersistedPosition = origin;
        }

        // removable: true wires long-press-to-remove (#22 D5) - opt-in so a bar's
        // fixed/default buttons (Target Last, the "+" trigger itself) can't be
        // long-pressed away by accident, while player-added actions (via the
        // picker) can be.
        public ActionButtonGump AddButton(GameActionDefinition definition, bool removable = false)
        {
            ActionButtonGump button;

            if (buttons.Count == 0)
            {
                button = new ActionButtonGump(definition, origin.X, origin.Y, tint);
                UIManager.Add(button);
                UIManager.AnchorManager[button] = new AnchorManager.AnchorGroup(button);
            }
            else
            {
                // Anchored relative to the bar's own origin (buttons[0]) rather than the
                // most-recently-added button, so wrapping to a new row is just "compute
                // col/row from index" instead of needing to track a separate per-row
                // chain. Safe even if buttons[0] itself later gets long-press-removed
                // (phrases are removable too) - AnchorGroup.DetachControl only clears
                // the removed control's own matrix cells, it doesn't invalidate the
                // group or the other members' positions, and the next buttons[0] is
                // already a tracked, anchored member of the same group.
                ActionButtonGump anchor = buttons[0];
                int index = buttons.Count;
                int col = index % maxPerRow;
                int row = index / maxPerRow;

                Point relativePosition;

                switch (layout)
                {
                    case ActionBarLayout.HorizontalRightToLeft:
                        // New rows stack upward (negative Y), away from the bottom-docked
                        // origin, toward open screen space instead of off an edge.
                        relativePosition = new Point(-col * anchor.WidthMultiplier, -row * anchor.HeightMultiplier);
                        break;
                    case ActionBarLayout.Vertical:
                        relativePosition = new Point(0, index * anchor.HeightMultiplier);
                        break;
                    default:
                        relativePosition = new Point(col * anchor.WidthMultiplier, row * anchor.HeightMultiplier);
                        break;
                }

                // relativePosition is already expressed in cell-multiple units (col/row
                // scaled by Width/HeightMultiplier, same convention the original
                // adjacent-only version used) - GroupMatrixWidth/Height converts cells to
                // pixels directly, no further scaling.
                int x = anchor.X + relativePosition.X * anchor.GroupMatrixWidth;
                int y = anchor.Y + relativePosition.Y * anchor.GroupMatrixHeight;

                button = new ActionButtonGump(definition, x, y, tint);
                UIManager.Add(button);

                AnchorManager.AnchorGroup group = UIManager.AnchorManager[anchor];
                group.AnchorControlAt(button, anchor, relativePosition);
                UIManager.AnchorManager[button] = group;
            }

            if (removable)
            {
                button.OnLongPress = () => RemoveButton(button);
            }

            buttons.Add(button);
            SaveContentsSnapshot();
            return button;
        }

        public void RemoveButton(ActionButtonGump button)
        {
            if (!buttons.Remove(button))
            {
                return;
            }

            UIManager.AnchorManager.DetachControl(button);
            button.Dispose();
            SaveContentsSnapshot();
        }

        // Lets the picker mark entries already on this bar (avoids silent duplicate
        // adds) without needing its own copy of what's here - same underlying data
        // SaveContentsSnapshot persists, just not written to PlayerPrefs.
        public IEnumerable<string> GetCatalogRefIds()
        {
            foreach (ActionButtonGump button in buttons)
            {
                if (button.Definition.CatalogRefId != null)
                {
                    yield return button.Definition.CatalogRefId;
                }
            }
        }

        // #22 D4: persists which catalog-sourced actions (spells/skills/pet commands
        // added via the picker) are on this bar, in order - bar-builtin actions (Target
        // Last, "+", QuickChat phrases) have no CatalogRefId and are silently excluded,
        // since their own bootstrap re-adds them unconditionally every login anyway.
        private void SaveContentsSnapshot()
        {
            var refIds = new List<string>();

            foreach (ActionButtonGump button in buttons)
            {
                if (button.Definition.CatalogRefId != null)
                {
                    refIds.Add(button.Definition.CatalogRefId);
                }
            }

            persistence.SaveContents(persistenceKey, refIds);
        }

        public bool Contains(ActionButtonGump button)
        {
            return buttons.Contains(button);
        }

        public void Clear()
        {
            foreach (ActionButtonGump button in buttons.ToArray())
            {
                RemoveButton(button);
            }
        }

        // Rebuilds the bar in a new growth direction, keeping the same buttons (in the
        // same order, same removable-ness) at the same on-screen origin. There's no
        // in-place "re-anchor a whole group" primitive on AnchorManager, so this tears
        // down and re-adds through the normal AddButton path rather than fighting that -
        // same reasoning as Clear()/RemoveButton already not touching UIManager.Gumps
        // directly.
        public void SetLayout(ActionBarLayout newLayout)
        {
            if (layout == newLayout)
            {
                return;
            }

            // Saved immediately, unlike position (which only writes on drift, via Tick) -
            // an explicit settings-screen choice should stick right away, not wait for a
            // drag gesture that will never come for this particular change.
            persistence.SaveLayout(persistenceKey, newLayout);

            if (buttons.Count == 0)
            {
                layout = newLayout;
                return;
            }

            origin = new Point(buttons[0].X, buttons[0].Y);

            var snapshot = new List<(GameActionDefinition Definition, bool Removable)>(buttons.Count);

            foreach (ActionButtonGump button in buttons)
            {
                snapshot.Add((button.Definition, button.OnLongPress != null));
            }

            Clear();
            layout = newLayout;

            foreach (var (definition, removable) in snapshot)
            {
                AddButton(definition, removable);
            }
        }

        // Temporary polling until AnchorManager exposes movement events.
        // Dragging any member of an anchor group moves the whole group via
        // AnchorGroup.UpdateLocation, so checking the first (anchor) button's
        // position each tick is enough to detect a manual drag. Only writes
        // to persistence when the position actually changed, never every
        // frame. Called by whatever MonoBehaviour drives it (ActionBarBootstrap
        // in Deliverable 2) - kept out of the constructor/AddButton so this
        // class has no Unity lifecycle dependency of its own. This is
        // documented, intentional technical debt, not an oversight.
        public void Tick()
        {
            if (buttons.Count == 0)
            {
                return;
            }

            ActionButtonGump anchor = buttons[0];
            var currentPosition = new Point(anchor.X, anchor.Y);

            if (currentPosition != lastPersistedPosition)
            {
                lastPersistedPosition = currentPosition;
                persistence.SavePosition(persistenceKey, currentPosition);
            }
        }
    }
}
