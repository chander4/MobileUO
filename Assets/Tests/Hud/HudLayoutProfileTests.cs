using NUnit.Framework;

namespace Hollowmere.Hud.Tests
{
    public class HudLayoutProfileTests
    {
        [Test]
        public void SerializeDeserialize_RoundTripsProfileAndUnknownWidgets()
        {
            HudLayoutProfile source = CreateProfile("default");
            source.Variants[0].WidgetStates.Add(new HudWidgetState { WidgetId = "future.unknown-widget" });
            source.Variants[0].WidgetSettings.Add(new HudWidgetSettings { WidgetId = "future.unknown-widget", SchemaVersion = 7, PayloadJson = "{\"future\":true}" });

            string json = HudLayoutProfileSerializer.Serialize(source);
            bool parsed = HudLayoutProfileSerializer.TryDeserialize(json, out HudLayoutProfile restored, out string error);

            Assert.That(parsed, Is.True, error);
            Assert.That(restored.Id, Is.EqualTo("default"));
            Assert.That(restored.Variants[0].WidgetStates[0].WidgetId, Is.EqualTo("future.unknown-widget"));
            Assert.That(restored.Variants[0].WidgetSettings[0].PayloadJson, Is.EqualTo("{\"future\":true}"));
        }

        [Test]
        public void Deserialize_MigratesSchemaZeroToCurrentSchema()
        {
            const string legacyJson = "{\"SchemaVersion\":0,\"Id\":\"legacy\",\"DisplayName\":\"Legacy\",\"Variants\":[]}";

            bool parsed = HudLayoutProfileSerializer.TryDeserialize(legacyJson, out HudLayoutProfile profile, out string error);

            Assert.That(parsed, Is.True, error);
            Assert.That(profile.SchemaVersion, Is.EqualTo(HudLayoutProfileSerializer.CurrentSchemaVersion));
        }

        [Test]
        public void Deserialize_RejectsMalformedOrFutureSchemaDataGracefully()
        {
            Assert.That(HudLayoutProfileSerializer.TryDeserialize("not json", out _, out string malformedError), Is.False);
            Assert.That(malformedError, Is.Not.Empty);

            const string futureJson = "{\"SchemaVersion\":999,\"Id\":\"future\",\"Variants\":[]}";
            Assert.That(HudLayoutProfileSerializer.TryDeserialize(futureJson, out _, out string futureError), Is.False);
            Assert.That(futureError, Does.Contain("newer than supported"));
        }

        [Test]
        public void Validate_RejectsDuplicateWidgetStateIds()
        {
            HudLayoutProfile profile = CreateProfile("duplicate");
            profile.Variants[0].WidgetStates.Add(new HudWidgetState { WidgetId = "hollowmere.action-bar" });
            profile.Variants[0].WidgetStates.Add(new HudWidgetState { WidgetId = "hollowmere.action-bar" });

            HudLayoutProfileValidationResult result = HudLayoutProfileSerializer.Validate(profile);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors[0], Does.Contain("duplicate"));
        }

        [Test]
        public void CopyAs_ProducesIndependentFullSnapshot()
        {
            HudLayoutProfile source = CreateProfile("default");
            source.Variants[0].WidgetStates.Add(new HudWidgetState { WidgetId = "hollowmere.primary-action", IsVisible = true });

            HudLayoutProfile copy = source.CopyAs("pve", "PvE");
            copy.Variants[0].WidgetStates[0].IsVisible = false;

            Assert.That(copy.Id, Is.EqualTo("pve"));
            Assert.That(copy.DisplayName, Is.EqualTo("PvE"));
            Assert.That(source.Variants[0].WidgetStates[0].IsVisible, Is.True);
        }

        private static HudLayoutProfile CreateProfile(string id)
        {
            var profile = new HudLayoutProfile { Id = id, DisplayName = id };
            profile.Variants.Add(new HudLayoutVariant
            {
                Key = new HudLayoutVariantKey { Orientation = HudOrientation.Landscape }
            });
            return profile;
        }
    }
}
