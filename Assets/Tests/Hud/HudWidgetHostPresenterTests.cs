using NUnit.Framework;

namespace Hollowmere.Hud.Tests
{
    public class HudWidgetHostPresenterTests
    {
        [Test]
        public void RefreshLayout_MaterializesRegisteredPresenterAndAppliesBounds()
        {
            var registry = new HudWidgetRegistry();
            registry.Register(new HudWidgetDefinition("hollowmere.test", HudWidgetRendererKind.Gump));

            var presenterRegistry = new HudWidgetPresenterRegistry();
            var recording = new RecordingPresenter("hollowmere.test");
            presenterRegistry.Register("hollowmere.test", _ => recording);

            var service = new HudLayoutService(registry, new InMemoryHudLayoutStorage());
            var host = new HudWidgetHost(registry, service, presenterRegistry);

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
                    new HudWidgetState
                    {
                        WidgetId = "hollowmere.test",
                        Placement = new HudPlacement
                        {
                            Anchor = new HudPoint(0, 0),
                            Pivot = new HudPoint(0, 0)
                        }
                    }
                }
            });

            var geometry = new HudLayoutGeometry(new HudSize(1000, 500), new HudInsets(0, 0, 0, 0));

            host.SetViewport(new HudLayoutViewport(HudOrientation.Landscape, HudDeviceClass.Phone));
            host.SetActiveProfile(profile);
            host.RefreshLayout(geometry);

            Assert.That(recording.ApplyCount, Is.EqualTo(1));
            Assert.That(recording.LastBounds.Width, Is.EqualTo(10));
            Assert.That(recording.LastBounds.X, Is.EqualTo(0));
        }

        private sealed class RecordingPresenter : IHudWidgetPresenter
        {
            public RecordingPresenter(string id) => WidgetId = id;

            public string WidgetId { get; }
            public int ApplyCount { get; private set; }
            public HudRect LastBounds { get; private set; }

            public void Apply(HudResolvedWidgetLayout layout)
            {
                ApplyCount++;
                LastBounds = layout.Bounds;
            }

            public void Dispose()
            {
            }

            public HudSize GetIntrinsicSize() => new HudSize(10, 10);
        }
    }
}
