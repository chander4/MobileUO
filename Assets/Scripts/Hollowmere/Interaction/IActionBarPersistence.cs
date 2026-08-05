using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Hollowmere.Interaction
{
    /// <summary>
    /// Storage for an ActionBar's on-screen position, keyed by the bar's
    /// persistenceKey. Kept separate from ActionBar so the bar's layout
    /// logic doesn't depend on how/where position is stored - a future
    /// milestone can swap in a different implementation without touching
    /// ActionBar.
    /// </summary>
    public interface IActionBarPersistence
    {
        bool TryLoadPosition(string key, out Point position);
        void SavePosition(string key, Point position);
        void DeletePosition(string key);

        bool TryLoadLayout(string key, out ActionBarLayout layout);
        void SaveLayout(string key, ActionBarLayout layout);

        // #22 D4: which ref-tagged removable actions are on the bar, in order - see
        // GameActionDefinition.CatalogRefId for why this is a list of stable string
        // references rather than the actions themselves. Try-pattern (not a plain
        // getter defaulting to empty) so a bootstrap can distinguish "never saved
        // before, seed the hardcoded defaults" from "explicitly saved as empty because
        // the player removed everything" - those need different behavior.
        bool TryLoadContents(string key, out IReadOnlyList<string> refIds);
        void SaveContents(string key, IReadOnlyList<string> refIds);
    }
}
