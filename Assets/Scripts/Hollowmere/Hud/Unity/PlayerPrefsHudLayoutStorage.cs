using System;
using UnityEngine;

namespace Hollowmere.Hud.Unity
{
    /// <summary>
    /// Initial local storage implementation. The key format is deliberately local
    /// to this adapter so future account/cloud storage can replace it without
    /// changing the pure profile model or its callers.
    /// </summary>
    public sealed class PlayerPrefsHudLayoutStorage : IHudLayoutStorage
    {
        private const string KeyPrefix = "Hollowmere.Hud.Layout.";

        public bool TryLoad(string profileId, out HudLayoutProfile profile, out string error)
        {
            profile = null;
            error = null;

            if (string.IsNullOrWhiteSpace(profileId))
            {
                error = "A HUD layout profile ID is required.";
                return false;
            }

            string key = BuildKey(profileId);

            if (!PlayerPrefs.HasKey(key))
            {
                error = "HUD layout profile was not found.";
                return false;
            }

            return HudLayoutProfileSerializer.TryDeserialize(PlayerPrefs.GetString(key), out profile, out error);
        }

        public void Save(HudLayoutProfile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            PlayerPrefs.SetString(BuildKey(profile.Id), HudLayoutProfileSerializer.Serialize(profile));
            PlayerPrefs.Save();
        }

        public void Delete(string profileId)
        {
            if (string.IsNullOrWhiteSpace(profileId))
            {
                throw new ArgumentException("A HUD layout profile ID is required.", nameof(profileId));
            }

            PlayerPrefs.DeleteKey(BuildKey(profileId));
            PlayerPrefs.Save();
        }

        private static string BuildKey(string profileId)
        {
            return KeyPrefix + profileId;
        }
    }
}
