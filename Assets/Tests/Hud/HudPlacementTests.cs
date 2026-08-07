using NUnit.Framework;

namespace Hollowmere.Hud.Tests
{
    public class HudPlacementTests
    {
        [Test]
        public void CalculateSafeArea_ExcludesInsetsAndLogicalUnitsUseShortestAxis()
        {
            HudRect safe = HudPlacementMath.CalculateSafeArea(new HudSize(1000, 600), new HudInsets(10, 20, 30, 40));

            Assert.That(safe.X, Is.EqualTo(10));
            Assert.That(safe.Y, Is.EqualTo(20));
            Assert.That(safe.Width, Is.EqualTo(960));
            Assert.That(safe.Height, Is.EqualTo(540));
            Assert.That(HudPlacementMath.LogicalUnitSize(safe), Is.EqualTo(5.4));
        }

        [Test]
        public void Resolve_UsesNormalizedAnchorPivotAndLogicalOffset()
        {
            var placement = new HudPlacement
            {
                Anchor = new HudPoint(1, 1),
                Pivot = new HudPoint(1, 1),
                LogicalOffset = new HudPoint(-2, -1)
            };

            HudRect rect = HudPlacementMath.Resolve(placement, new HudRect(0, 0, 1000, 500), new HudSize(100, 50));

            Assert.That(rect.X, Is.EqualTo(890));
            Assert.That(rect.Y, Is.EqualTo(445));
        }

        [Test]
        public void TransformOrientation_RotatesAnchorPivotOffsetAndDock()
        {
            var portrait = new HudPlacement
            {
                Anchor = new HudPoint(0.2, 0.8),
                Pivot = new HudPoint(0, 1),
                LogicalOffset = new HudPoint(3, 7),
                DockEdge = HudDockEdge.Left
            };

            HudPlacement landscape = HudPlacementMath.TransformOrientation(portrait, HudOrientation.Portrait, HudOrientation.Landscape);

            Assert.That(landscape.Anchor.X, Is.EqualTo(0.2).Within(1e-9));
            Assert.That(landscape.Anchor.Y, Is.EqualTo(0.2).Within(1e-9));
            Assert.That(landscape.Pivot.X, Is.EqualTo(0).Within(1e-9));
            Assert.That(landscape.Pivot.Y, Is.EqualTo(0).Within(1e-9));
            Assert.That(landscape.LogicalOffset.X, Is.EqualTo(-7).Within(1e-9));
            Assert.That(landscape.LogicalOffset.Y, Is.EqualTo(3).Within(1e-9));
            Assert.That(landscape.DockEdge, Is.EqualTo(HudDockEdge.Top));
        }

        [Test]
        public void SnapToSafeAreaEdges_SnapsOnlyWithinTolerance()
        {
            HudRect snapped = HudPlacementMath.SnapToSafeAreaEdges(new HudRect(3, 90, 20, 10), new HudRect(0, 0, 100, 100), 5);
            HudRect unchanged = HudPlacementMath.SnapToSafeAreaEdges(new HudRect(8, 90, 20, 10), new HudRect(0, 0, 100, 100), 5);

            Assert.That(snapped.X, Is.EqualTo(0));
            Assert.That(snapped.Y, Is.EqualTo(90));
            Assert.That(unchanged.X, Is.EqualTo(8));
        }

        [Test]
        public void ResolveDocked_ComputesOrderedEdgeSlotsWithoutRendering()
        {
            HudRect bottom = HudPlacementMath.ResolveDocked(new HudRect(0, 0, 200, 100), new HudSize(40, 20), HudDockEdge.Bottom, 2, 5);
            HudRect right = HudPlacementMath.ResolveDocked(new HudRect(0, 0, 200, 100), new HudSize(40, 20), HudDockEdge.Right, 1, 5);

            Assert.That(bottom.X, Is.EqualTo(90));
            Assert.That(bottom.Y, Is.EqualTo(80));
            Assert.That(right.X, Is.EqualTo(160));
            Assert.That(right.Y, Is.EqualTo(25));
        }
    }
}
