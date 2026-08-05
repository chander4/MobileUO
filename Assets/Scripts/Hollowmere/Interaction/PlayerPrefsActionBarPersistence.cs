using System;
using System.Collections.Generic;
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

        public void DeletePosition(string key)
        {
            PlayerPrefs.DeleteKey(key + "X");
            PlayerPrefs.DeleteKey(key + "Y");
            PlayerPrefs.Save();
        }

        public bool TryLoadLayout(string key, out ActionBarLayout layout)
        {
            if (!PlayerPrefs.HasKey(key + "Layout"))
            {
                layout = default;
                return false;
            }

            layout = (ActionBarLayout) PlayerPrefs.GetInt(key + "Layout");
            return true;
        }

        public void SaveLayout(string key, ActionBarLayout layout)
        {
            PlayerPrefs.SetInt(key + "Layout", (int) layout);
            PlayerPrefs.Save();
        }

        // Comma-joined - ref IDs ("spell:14", "phrase:Yes") never contain a comma, so
        // no escaping needed. PlayerPrefs has no native array/list support, matching
        // the rest of this class's plain-primitive-key convention.
        public bool TryLoadContents(string key, out IReadOnlyList<string> refIds)
        {
            if (!PlayerPrefs.HasKey(key + "Contents"))
            {
                refIds = Array.Empty<string>();
                return false;
            }

            string raw = PlayerPrefs.GetString(key + "Contents");
            refIds = string.IsNullOrEmpty(raw) ? Array.Empty<string>() : raw.Split(',');
            return true;
        }

        public void SaveContents(string key, IReadOnlyList<string> refIds)
        {
            PlayerPrefs.SetString(key + "Contents", string.Join(",", refIds));
            PlayerPrefs.Save();
        }
    }
}
