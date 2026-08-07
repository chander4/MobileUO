using NUnit.Framework;

namespace Hollowmere.Hud.Tests
{
    /// <summary>
    /// Permanent migration-chain tests. When <see cref="HudLayoutProfileSerializer.CurrentSchemaVersion"/>
    /// increases, add a frozen JSON fixture for the previous schema and assert
    /// <see cref="HudLayoutProfileSerializer.Migrate"/> plus deserialize still yield a valid profile.
    /// </summary>
    public class HudLayoutMigrationTests
    {
        private const string SchemaZeroFixtureJson =
            "{\"SchemaVersion\":0,\"Id\":\"fixture-schema-0\",\"DisplayName\":\"Schema 0\",\"Variants\":[{\"Key\":{\"Orientation\":1},\"WidgetStates\":[{\"WidgetId\":\"hollowmere.action-bar\",\"IsVisible\":false,\"Placement\":{\"Anchor\":{\"X\":0.25,\"Y\":0.75}}}],\"WidgetSettings\":[{\"WidgetId\":\"hollowmere.action-bar\",\"SchemaVersion\":1,\"PayloadJson\":\"{\\\"slots\\\":4}\"}]}]}";

        private const string SchemaOneFixtureJson =
            "{\"SchemaVersion\":1,\"Id\":\"fixture-schema-1\",\"DisplayName\":\"Schema 1\",\"Variants\":[{\"Key\":{\"Orientation\":0},\"WidgetStates\":[{\"WidgetId\":\"hollowmere.primary-action\"}],\"WidgetSettings\":[]}]}";

        [Test]
        public void SchemaZeroFixture_MigratesToCurrent_AndPreservesWidgetData()
        {
            Assert.That(
                HudLayoutProfileSerializer.TryDeserialize(SchemaZeroFixtureJson, out HudLayoutProfile profile, out string error),
                Is.True,
                error);

            Assert.That(profile.SchemaVersion, Is.EqualTo(HudLayoutProfileSerializer.CurrentSchemaVersion));
            Assert.That(profile.Id, Is.EqualTo("fixture-schema-0"));
            Assert.That(profile.Variants[0].WidgetStates[0].WidgetId, Is.EqualTo("hollowmere.action-bar"));
            Assert.That(profile.Variants[0].WidgetStates[0].IsVisible, Is.False);
            Assert.That(profile.Variants[0].WidgetSettings[0].PayloadJson, Is.EqualTo("{\"slots\":4}"));
        }

        [Test]
        public void CurrentSchemaFixture_MigrateIsIdempotent_AndSurvivesSerializeDeserialize()
        {
            Assert.That(
                HudLayoutProfileSerializer.TryDeserialize(SchemaOneFixtureJson, out HudLayoutProfile profile, out string parseError),
                Is.True,
                parseError);

            HudLayoutProfileSerializer.Migrate(profile);
            int schemaAfterFirstMigrate = profile.SchemaVersion;

            HudLayoutProfileSerializer.Migrate(profile);

            Assert.That(profile.SchemaVersion, Is.EqualTo(schemaAfterFirstMigrate));
            Assert.That(profile.SchemaVersion, Is.EqualTo(HudLayoutProfileSerializer.CurrentSchemaVersion));

            string json = HudLayoutProfileSerializer.Serialize(profile);

            Assert.That(
                HudLayoutProfileSerializer.TryDeserialize(json, out HudLayoutProfile restored, out string roundTripError),
                Is.True,
                roundTripError);
            Assert.That(restored.Id, Is.EqualTo("fixture-schema-1"));
            Assert.That(restored.Variants[0].WidgetStates[0].WidgetId, Is.EqualTo("hollowmere.primary-action"));
        }
    }
}
