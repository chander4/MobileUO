using NUnit.Framework;

namespace Hollowmere.Hud.Tests
{
    public class HudBuiltInProfilesTests
    {
        [Test]
        public void CreateDefault_IncludesPrimaryActionActionBarAndVitalsWidgets()
        {
            HudLayoutProfile profile = HudBuiltInProfiles.CreateDefault();

            Assert.That(profile.Variants[0].WidgetStates.Count, Is.EqualTo(3));
            Assert.That(profile.Variants[0].WidgetStates[0].WidgetId, Is.EqualTo(HudWidgetIds.Vitals));
            Assert.That(profile.Variants[0].WidgetStates[1].WidgetId, Is.EqualTo(HudWidgetIds.PrimaryAction));
            Assert.That(profile.Variants[0].WidgetStates[2].WidgetId, Is.EqualTo(HudWidgetIds.ActionBar));
        }
    }
}
