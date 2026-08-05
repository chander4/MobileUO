using System;

namespace Hollowmere.Interaction
{
    /// <summary>
    /// Canonical representation of a single gameplay action a player can
    /// trigger from a touch-friendly control. Target Last is only the
    /// first consumer - future actions (Attack Last, War Mode, Peace Mode,
    /// Bandage, Hide, Meditation, custom macros, ...) require only another
    /// instance of this class. Deliberately presentation-agnostic: nothing
    /// here assumes it's rendered as a Gump button - a future hotbar,
    /// radial menu, or keyboard-shortcut binding can consume the same
    /// definition.
    /// </summary>
    public class GameActionDefinition
    {
        public string Label { get; }
        public Action Action { get; }
        public Func<bool> CanExecute { get; }
        public string Tooltip { get; }
        public ushort? IconGraphic { get; }
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Stable, source-agnostic reference (e.g. "spell:14", "skill:Tactics",
        /// "pet:All Follow", "phrase:Yes") - lets ActionBar persist *which* actions a
        /// player has on a bar (#22 Deliverable 4) by saving this string and
        /// re-resolving it on next login (ActionCatalog.TryResolve for catalog
        /// entries; a bootstrap's own small lookup for its hardcoded defaults like
        /// QuickChat's phrases) instead of trying to serialize the Action delegate
        /// itself. Null only for actions that must never be removable/persisted -
        /// Target Last and each bar's "+" trigger, since removing "+" would strand the
        /// player with no way back to the picker.
        /// </summary>
        public string CatalogRefId { get; }

        public GameActionDefinition(
            string label,
            Action action,
            Func<bool> canExecute = null,
            string tooltip = null,
            ushort? iconGraphic = null,
            string catalogRefId = null)
        {
            Label = label;
            Action = action ?? throw new ArgumentNullException(nameof(action));
            CanExecute = canExecute;
            Tooltip = tooltip;
            IconGraphic = iconGraphic;
            CatalogRefId = catalogRefId;
        }

        public bool IsAvailable => Enabled && (CanExecute == null || CanExecute());

        public bool TryExecute()
        {
            if (!IsAvailable)
            {
                return false;
            }

            Action();
            return true;
        }
    }
}
