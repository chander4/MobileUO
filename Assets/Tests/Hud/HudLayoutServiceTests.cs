using System;
using NUnit.Framework;

namespace Hollowmere.Hud.Tests
{
    public class HudLayoutServiceTests
    {
        [Test]
        public void RegisterBuiltInProfile_AndCreatePlayerProfile_CopiesSnapshot()
        {
            var service = CreateService();
            HudLayoutProfile builtIn = HudBuiltInProfiles.CreateDefault();
            service.RegisterBuiltInProfile(builtIn);

            HudLayoutProfile player = service.CreatePlayerProfileFromBuiltIn(
                HudLayoutProfileIds.BuiltInDefault,
                "player-1",
                "Player 1");

            player.Variants[0].WidgetStates[0].IsVisible = false;

            Assert.That(player.Id, Is.EqualTo("player-1"));
            Assert.That(service.TryGetBuiltInProfile(HudLayoutProfileIds.BuiltInDefault, out HudLayoutProfile template), Is.True);
            Assert.That(template.Variants[0].WidgetStates[0].IsVisible, Is.True);
        }

        [Test]
        public void Resolve_MergesMissingWidgetsFromDefaultTemplate()
        {
            var service = CreateService();
            HudLayoutProfile builtIn = HudBuiltInProfiles.CreateDefault();
            service.RegisterBuiltInProfile(builtIn);

            var player = new HudLayoutProfile
            {
                Id = "player",
                DisplayName = "player",
                SchemaVersion = HudLayoutProfileSerializer.CurrentSchemaVersion
            };
            player.Variants.Add(new HudLayoutVariant
            {
                Key = new HudLayoutVariantKey { Orientation = HudOrientation.Landscape }
            });

            HudLayoutResolution resolution = service.Resolve(
                player,
                new HudLayoutViewport(HudOrientation.Landscape, HudDeviceClass.Phone),
                builtIn);

            Assert.That(resolution.WidgetStates.Count, Is.EqualTo(3));
            Assert.That(resolution.WidgetStates[0].WidgetId, Is.EqualTo(HudWidgetIds.Vitals));
            Assert.That(resolution.WidgetStates[1].WidgetId, Is.EqualTo(HudWidgetIds.PrimaryAction));
            Assert.That(resolution.WidgetStates[2].WidgetId, Is.EqualTo(HudWidgetIds.ActionBar));
        }

        [Test]
        public void Resolve_EmitsRegistryWarningsWithoutStrippingUnknownWidgets()
        {
            var registry = new HudWidgetRegistry();
            var service = new HudLayoutService(registry, new InMemoryHudLayoutStorage());

            var profile = new HudLayoutProfile
            {
                Id = "player",
                DisplayName = "player",
                SchemaVersion = HudLayoutProfileSerializer.CurrentSchemaVersion
            };
            profile.Variants.Add(new HudLayoutVariant
            {
                Key = new HudLayoutVariantKey { Orientation = HudOrientation.Landscape },
                WidgetStates =
                {
                    new HudWidgetState { WidgetId = "future.widget" }
                }
            });

            HudLayoutResolution resolution = service.Resolve(
                profile,
                new HudLayoutViewport(HudOrientation.Landscape, HudDeviceClass.Phone));

            Assert.That(resolution.WidgetStates.Count, Is.EqualTo(1));
            Assert.That(resolution.Warnings.Count, Is.EqualTo(1));
            Assert.That(resolution.Warnings[0], Does.Contain("future.widget"));
        }

        [Test]
        public void ExportAndImport_RoundTripProfile()
        {
            var service = CreateService();
            HudLayoutProfile builtIn = HudBuiltInProfiles.CreateDefault();
            service.RegisterBuiltInProfile(builtIn);
            HudLayoutProfile player = service.CreatePlayerProfileFromBuiltIn(
                HudLayoutProfileIds.BuiltInDefault,
                "player-export",
                "Export");

            string json = service.ExportToJson(player);

            Assert.That(service.TryImportFromJson(json, out HudLayoutProfile imported, out string error), Is.True, error);
            Assert.That(imported.Id, Is.EqualTo("player-export"));
        }

        [Test]
        public void SaveAndLoad_UsesStorageBoundary()
        {
            var storage = new InMemoryHudLayoutStorage();
            var service = new HudLayoutService(new HudWidgetRegistry(), storage);
            HudLayoutProfile profile = HudBuiltInProfiles.CreateDefault();
            profile.Id = "saved";
            profile.DisplayName = "Saved";

            service.Save(profile);

            Assert.That(service.TryLoad("saved", out HudLayoutProfile loaded, out string error), Is.True, error);
            Assert.That(loaded.DisplayName, Is.EqualTo("Saved"));
        }

        private static HudLayoutService CreateService()
        {
            return new HudLayoutService(new HudWidgetRegistry(), new InMemoryHudLayoutStorage());
        }
    }
}
