using System;
using NUnit.Framework;

namespace Hollowmere.Hud.Tests
{
    public class HudWidgetPresenterRegistryTests
    {
        [Test]
        public void Register_RejectsDuplicateFactories()
        {
            var registry = new HudWidgetPresenterRegistry();
            registry.Register("hollowmere.test", _ => new StubPresenter("hollowmere.test"));

            Assert.Throws<InvalidOperationException>(() =>
                registry.Register("hollowmere.test", _ => new StubPresenter("hollowmere.test")));
        }

        [Test]
        public void TryCreate_ReturnsRegisteredPresenter()
        {
            var registry = new HudWidgetPresenterRegistry();
            registry.Register("hollowmere.test", _ => new StubPresenter("hollowmere.test"));

            var definition = new HudWidgetDefinition("hollowmere.test", HudWidgetRendererKind.Gump);

            Assert.That(registry.TryCreate(definition, out IHudWidgetPresenter presenter), Is.True);
            Assert.That(presenter.WidgetId, Is.EqualTo("hollowmere.test"));
        }

        private sealed class StubPresenter : IHudWidgetPresenter
        {
            public StubPresenter(string id) => WidgetId = id;

            public string WidgetId { get; }

            public void Apply(HudResolvedWidgetLayout layout)
            {
            }

            public void Dispose()
            {
            }

            public HudSize GetIntrinsicSize() => new HudSize(10, 10);
        }
    }
}
