using NUnit.Framework;

namespace Hollowmere.Hud.Tests
{
    public class HudBuiltInProfilesTests
    {
        [Test]
        public void CreateDefault_IncludesPrimaryActionActionBarVitalsStatsAndTargetVitalsWidgets()
        {
            HudLayoutProfile profile = HudBuiltInProfiles.CreateDefault();

            Assert.That(profile.Variants[0].WidgetStates.Count, Is.EqualTo(6));
            Assert.That(profile.Variants[0].WidgetStates[0].WidgetId, Is.EqualTo(HudWidgetIds.Vitals));
            Assert.That(profile.Variants[0].WidgetStates[1].WidgetId, Is.EqualTo(HudWidgetIds.StatusEffects));
            Assert.That(profile.Variants[0].WidgetStates[2].WidgetId, Is.EqualTo(HudWidgetIds.PrimaryAction));
            Assert.That(profile.Variants[0].WidgetStates[3].WidgetId, Is.EqualTo(HudWidgetIds.ActionBar));
            Assert.That(profile.Variants[0].WidgetStates[4].WidgetId, Is.EqualTo(HudWidgetIds.Stats));
            Assert.That(profile.Variants[0].WidgetStates[5].WidgetId, Is.EqualTo(HudWidgetIds.TargetVitals));
        }
    }
}
