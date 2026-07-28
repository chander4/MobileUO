using Microsoft.Xna.Framework;
using UnityEngine;

namespace Hollowmere.Interaction
{
    /// <summary>
    /// First (and, for this milestone, only) IActionBarPersistence
    /// implementation - PlayerPrefs-backed, matching the existing
    /// UserPreferences convention (Assets/Scripts/UserPreferences.cs) of
    /// storing a Vector's components under "{key}X"/"{key}Y". A future
    /// milestone can swap in ClassicUO XML or a Hollowmere profile save by
    /// implementing IActionBarPersistence, with no ActionBar change.
    /// </summary>
    public class PlayerPrefsActionBarPersistence : IActionBarPersistence
    {
        public bool TryLoadPosition(string key, out Point position)
        {
            if (!PlayerPrefs.HasKey(key + "X") || !PlayerPrefs.HasKey(key + "Y"))
            {
                position = default;
                return false;
            }

            position = new Point(PlayerPrefs.GetInt(key + "X"), PlayerPrefs.GetInt(key + "Y"));
            return true;
        }

        public void SavePosition(string key, Point position)
        {
            PlayerPrefs.SetInt(key + "X", position.X);
            PlayerPrefs.SetInt(key + "Y", position.Y);
            PlayerPrefs.Save();
        }
    }
}
