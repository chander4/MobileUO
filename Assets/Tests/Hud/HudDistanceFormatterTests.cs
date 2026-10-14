using NUnit.Framework;

namespace Hollowmere.Hud.Tests
{
    public class HudDistanceFormatterTests
    {
        [Test]
        public void FormatTiles_ZeroTiles_ReadsAsPlural()
        {
            Assert.That(HudDistanceFormatter.FormatTiles(0), Is.EqualTo("0 tiles"));
        }

        [Test]
        public void FormatTiles_OneTile_UsesSingular()
        {
            Assert.That(HudDistanceFormatter.FormatTiles(1), Is.EqualTo("1 tile"));
        }

        [Test]
        public void FormatTiles_MultipleTiles_UsesPlural()
        {
            Assert.That(HudDistanceFormatter.FormatTiles(12), Is.EqualTo("12 tiles"));
        }

        [Test]
        public void FormatTiles_UnknownSentinel_ReturnsEmpty()
        {
            Assert.That(HudDistanceFormatter.FormatTiles(HudDistanceFormatter.UnknownDistance), Is.EqualTo(string.Empty));
        }

        [Test]
        public void FormatTiles_Negative_ReturnsEmpty()
        {
            Assert.That(HudDistanceFormatter.FormatTiles(-1), Is.EqualTo(string.Empty));
        }
    }
}
