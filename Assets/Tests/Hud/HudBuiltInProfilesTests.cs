using NUnit.Framework;

namespace Hollowmere.Hud.Tests
{
    public class HudBuiltInProfilesTests
    {
        [Test]
        public void CreateDefault_IncludesPrimaryActionActionBarVitalsAndStatsWidgets()
        {
            HudLayoutProfile profile = HudBuiltInProfiles.CreateDefault();

            Assert.That(profile.Variants[0].WidgetStates.Count, Is.EqualTo(4));
            Assert.That(profile.Variants[0].WidgetStates[0].WidgetId, Is.EqualTo(HudWidgetIds.Vitals));
            Assert.That(profile.Variants[0].WidgetStates[1].WidgetId, Is.EqualTo(HudWidgetIds.PrimaryAction));
            Assert.That(profile.Variants[0].WidgetStates[2].WidgetId, Is.EqualTo(HudWidgetIds.ActionBar));
            Assert.That(profile.Variants[0].WidgetStates[3].WidgetId, Is.EqualTo(HudWidgetIds.Stats));
        }
    }
}
