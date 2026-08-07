using System.Collections.Generic;
using NUnit.Framework;

namespace Hollowmere.Hud.Tests
{
    public class HudLayoutStorageTests
    {
        [Test]
        public void SaveAndLoad_RoundTripsThroughStorageAbstraction()
        {
            var storage = new InMemoryHudLayoutStorage();
            HudLayoutProfile profile = CreateProfile("mobile-default");
            profile.Variants[0].WidgetStates.Add(new HudWidgetState { WidgetId = "hollowmere.action-bar" });

            storage.Save(profile);

            Assert.That(storage.TryLoad("mobile-default", out HudLayoutProfile restored, out string error), Is.True, error);
            Assert.That(restored.Variants[0].WidgetStates[0].WidgetId, Is.EqualTo("hollowmere.action-bar"));
        }

        [Test]
        public void TryLoad_ReturnsGracefulErrorWhenProfileMissing()
        {
            var storage = new InMemoryHudLayoutStorage();

            Assert.That(storage.TryLoad("missing", out _, out string error), Is.False);
            Assert.That(error, Is.Not.Empty);
        }

        [Test]
        public void Delete_RemovesPersistedProfile()
        {
            var storage = new InMemoryHudLayoutStorage();
            storage.Save(CreateProfile("temp"));

            storage.Delete("temp");

            Assert.That(storage.TryLoad("temp", out _, out _), Is.False);
        }

        private static HudLayoutProfile CreateProfile(string id)
        {
            var profile = new HudLayoutProfile { Id = id, DisplayName = id, SchemaVersion = HudLayoutProfileSerializer.CurrentSchemaVersion };
            profile.Variants.Add(new HudLayoutVariant
            {
                Key = new HudLayoutVariantKey { Orientation = HudOrientation.Portrait }
            });
            return profile;
        }

        private sealed class InMemoryHudLayoutStorage : IHudLayoutStorage
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
}
