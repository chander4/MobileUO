using System.Collections.Generic;

namespace Hollowmere.Hud.Tests
{
    internal sealed class InMemoryHudLayoutStorage : IHudLayoutStorage
    {
        private readonly Dictionary<string, string> profiles = new Dictionary<string, string>();

        public bool TryLoad(string profileId, out HudLayoutProfile profile, out string error)
        {
            profile = null;
            error = null;

            if (!profiles.TryGetValue(profileId, out string json))
            {
                error = "HUD layout profile was not found.";
                return false;
            }

            return HudLayoutProfileSerializer.TryDeserialize(json, out profile, out error);
        }

        public void Save(HudLayoutProfile profile)
        {
            profiles[profile.Id] = HudLayoutProfileSerializer.Serialize(profile);
        }

        public void Delete(string profileId)
        {
            profiles.Remove(profileId);
        }
    }
}
