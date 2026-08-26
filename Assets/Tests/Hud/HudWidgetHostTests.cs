using NUnit.Framework;

namespace Hollowmere.Hud.Tests
{
    public class HudWidgetHostTests
    {
        [Test]
        public void RefreshLayout_UsesLayoutServiceWithoutMutatingProfileWidgets()
        {
            var registry = new HudWidgetRegistry();
            var presenterRegistry = new HudWidgetPresenterRegistry();
            var service = new HudLayoutService(registry, new InMemoryHudLayoutStorage());
            HudLayoutProfile builtIn = HudBuiltInProfiles.CreateDefault();
            builtIn.Variants[0].WidgetStates.Clear();
            builtIn.Variants[0].WidgetStates.Add(new HudWidgetState { WidgetId = "hollowmere.joystick", IsVisible = true });
            service.RegisterBuiltInProfile(builtIn);

            var player = new HudLayoutProfile
            {
                Id = "player-host",
                DisplayName = "Player",
                SchemaVersion = HudLayoutProfileSerializer.CurrentSchemaVersion
            };
            player.Variants.Add(new HudLayoutVariant
            {
                Key = new HudLayoutVariantKey { Orientation = HudOrientation.Landscape }
            });

            var host = new HudWidgetHost(registry, service, presenterRegistry);
            var geometry = new HudLayoutGeometry(new HudSize(1000, 500), new HudInsets(0, 0, 0, 0));
            host.SetDefaultTemplate(builtIn);
            host.SetViewport(new HudLayoutViewport(HudOrientation.Landscape, HudDeviceClass.Phone));
            host.SetActiveProfile(player);
            host.RefreshLayout(geometry);

            Assert.That(host.CurrentResolution, Is.Not.Null);
            Assert.That(host.CurrentResolution.WidgetStates.Count, Is.EqualTo(1));
            Assert.That(host.CurrentResolution.WidgetStates[0].WidgetId, Is.EqualTo("hollowmere.joystick"));
            Assert.That(player.Variants[0].WidgetStates.Count, Is.EqualTo(0));
        }

        [Test]
        public void SetViewport_ChangeTriggersNewResolution()
        {
            var registry = new HudWidgetRegistry();
            var presenterRegistry = new HudWidgetPresenterRegistry();
            var service = new HudLayoutService(registry, new InMemoryHudLayoutStorage());
            HudLayoutProfile builtIn = HudBuiltInProfiles.CreateDefault();
            builtIn.Variants[0].WidgetStates[0].WidgetId = "landscape";
            builtIn.Variants[1].WidgetStates[0].WidgetId = "portrait";
            service.RegisterBuiltInProfile(builtIn);

            var player = service.CreatePlayerProfileFromBuiltIn(
                HudLayoutProfileIds.BuiltInDefault,
                "player-rotate",
                "Player");

            var host = new HudWidgetHost(registry, service, presenterRegistry);
            var geometry = new HudLayoutGeometry(new HudSize(1000, 800), new HudInsets(0, 0, 0, 0));
            host.SetDefaultTemplate(builtIn);
            host.SetActiveProfile(player);
            host.SetViewport(new HudLayoutViewport(HudOrientation.Landscape, HudDeviceClass.Phone));
            host.RefreshLayout(geometry);

            Assert.That(host.CurrentResolution.WidgetStates[0].WidgetId, Is.EqualTo("landscape"));

            host.SetViewport(new HudLayoutViewport(HudOrientation.Portrait, HudDeviceClass.Phone));
            host.RefreshLayout(geometry);

            Assert.That(host.CurrentResolution.WidgetStates[0].WidgetId, Is.EqualTo("portrait"));
        }
    }
}
