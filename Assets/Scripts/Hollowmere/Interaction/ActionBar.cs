using System;
using System.Collections.Generic;
using ClassicUO.Game.Managers;
using Microsoft.Xna.Framework;

namespace Hollowmere.Interaction
{
    public enum ActionBarLayout
    {
        Horizontal,
        Vertical
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
        private readonly ActionBarLayout layout;
        private readonly IActionBarPersistence persistence;
        private readonly List<ActionButtonGump> buttons = new List<ActionButtonGump>();

        private Point origin;
        private Point lastPersistedPosition;

        public IReadOnlyList<ActionButtonGump> Buttons => buttons;
        public int Count => buttons.Count;

        public ActionBar(string persistenceKey, ActionBarLayout layout, int defaultX, int defaultY, IActionBarPersistence persistence)
        {
            this.persistenceKey = persistenceKey ?? throw new ArgumentNullException(nameof(persistenceKey));
            this.layout = layout;
            this.persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));

            origin = persistence.TryLoadPosition(persistenceKey, out Point loaded)
                ? loaded
                : new Point(defaultX, defaultY);

            lastPersistedPosition = origin;
        }

        public ActionButtonGump AddButton(GameActionDefinition definition)
        {
            ActionButtonGump button;

            if (buttons.Count == 0)
            {
                button = new ActionButtonGump(definition, origin.X, origin.Y);
                UIManager.Add(button);
                UIManager.AnchorManager[button] = new AnchorManager.AnchorGroup(button);
            }
            else
            {
                ActionButtonGump host = buttons[buttons.Count - 1];

                Point relativePosition = layout == ActionBarLayout.Horizontal
                    ? new Point(host.WidthMultiplier, 0)
                    : new Point(0, host.HeightMultiplier);

                int x = host.X + relativePosition.X * host.GroupMatrixWidth;
                int y = host.Y + relativePosition.Y * host.GroupMatrixHeight;

                button = new ActionButtonGump(definition, x, y);
                UIManager.Add(button);

                AnchorManager.AnchorGroup group = UIManager.AnchorManager[host];
                group.AnchorControlAt(button, host, relativePosition);
                UIManager.AnchorManager[button] = group;
            }

            buttons.Add(button);
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
