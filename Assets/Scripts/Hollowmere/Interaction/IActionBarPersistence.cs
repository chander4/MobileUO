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
    }
}
