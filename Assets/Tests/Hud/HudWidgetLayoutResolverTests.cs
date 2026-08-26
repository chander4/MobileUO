using NUnit.Framework;

namespace Hollowmere.Hud.Tests
{
    public class HudWidgetLayoutResolverTests
    {
        [Test]
        public void ResolveBounds_UsesPlacementMathAgainstSafeArea()
        {
            var state = new HudWidgetState
            {
                Placement = new HudPlacement
                {
                    Anchor = new HudPoint(1, 1),
                    Pivot = new HudPoint(1, 1),
                    LogicalOffset = new HudPoint(-2, -1)
                }
            };

            var geometry = new HudLayoutGeometry(new HudSize(1000, 500), new HudInsets(0, 0, 0, 0));
            HudRect bounds = HudWidgetLayoutResolver.ResolveBounds(state, geometry, new HudSize(100, 50));

            Assert.That(bounds.X, Is.EqualTo(890));
            Assert.That(bounds.Y, Is.EqualTo(445));
        }
    }
}
