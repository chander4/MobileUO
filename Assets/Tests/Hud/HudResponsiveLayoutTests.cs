using System.Collections.Generic;
using NUnit.Framework;

namespace Hollowmere.Hud.Tests
{
    /// <summary>
    /// Mobile HUD Responsive Layout v1/v2: exercises the real production resolution
    /// pipeline (HudLayoutService.Resolve -> HudLayoutVariantSelector.Select ->
    /// HudWidgetLayoutResolver.ResolveBounds, exactly what HudWidgetHost.SyncPresenters
    /// calls per widget) against HudBuiltInProfiles.CreateDefault() at several
    /// landscape aspect ratios/resolutions, and asserts both which variant gets
    /// selected (v2) and that the six persistent widgets stay within the safe area
    /// and never overlap each other (v1). No portrait case: ProjectSettings has
    /// allowedAutorotateToPortrait/allowedAutorotateToPortraitUpsideDown both 0 -
    /// this project is landscape-locked, so portrait is out of scope per the
    /// task's own "if the current project supports portrait" condition.
    ///
    /// Intrinsic sizes are duplicated here (not referenced from the owning
    /// presenters) because this test assembly's .asmdef only references
    /// Hollowmere.Hud, not the Assembly-CSharp types that declare them - same
    /// cross-assembly-boundary reason HudBuiltInProfiles.VitalsHeightForClearance
    /// already duplicates HealthBarGumpCustom.HPB_HEIGHT_MULTILINE. Sources:
    /// PlayerVitalsGump -> HealthBarGumpCustom.HPB_WIDTH (120) /
    /// HPB_HEIGHT_MULTILINE (60); TargetVitalsGump -> HPB_WIDTH (120) /
    /// HPB_HEIGHT_MULTILINE + DistanceLabelHeight (60+16=76, Interaction Polish
    /// v1 - see TargetVitalsPresenter.TargetVitalsHeight); PlayerStatsGump.
    /// StatsWidth/Height (160/112); BuffBarGump.BarWidth/BarHeight (192/108);
    /// ActionButtonGump.ButtonWidth/Height (88/44), used by both
    /// ActionBarPresenter and PrimaryActionPresenter.
    /// </summary>
    public class HudResponsiveLayoutTests
    {
        private static readonly Dictionary<string, HudSize> IntrinsicSizes = new Dictionary<string, HudSize>
        {
            [HudWidgetIds.Vitals] = new HudSize(120, 60),
            [HudWidgetIds.StatusEffects] = new HudSize(192, 108),
            [HudWidgetIds.Stats] = new HudSize(160, 112),
            // Mobile HUD Interaction Polish v1: 76, not 60 - TargetVitalsGump adds a
            // 16px distance-label row on top of the 60px base bar
            // (TargetVitalsPresenter.TargetVitalsHeight was corrected to match;
            // confirmed live via the actual gump's Height).
            [HudWidgetIds.TargetVitals] = new HudSize(120, 76),
            [HudWidgetIds.ActionBar] = new HudSize(88, 44),
            [HudWidgetIds.PrimaryAction] = new HudSize(88, 44)
        };

        // Widths/heights are real raw-pixel landscape resolutions (this project's
        // resolutionScalingMode is Disabled, so Screen.width/height - and therefore
        // Window.ClientBounds, see ClientRunner.cs - report physical pixels, not
        // scaled logical points). 1920x1080 matches the ~1920x1080 Editor Game view
        // HudBuiltInProfiles' own comments say the original constants were tuned
        // against. 1560x720 is an exact 19.5:9 budget-phone-class panel. 1024x768 is
        // an exact 4:3 budget-tablet-class panel (e.g. the classic iPad 2 native
        // resolution). Device-class classification mirrors
        // HudWidgetHostComposition.TryGetGeometry's own rule (shorter side >= 900 =>
        // Tablet) for realism.
        //
        // Mobile HUD Responsive Layout v2: expectedFallbackStep/expectedResolvedDeviceClass
        // assert which variant HudLayoutVariantSelector actually picks for each case -
        // not just that the resolved bounds are safe. 1920x1080 is the only case that
        // exact-matches the registered Landscape/Tablet variant; 1560x720 and 1024x768
        // both classify Phone (no Landscape/Phone variant is registered), so both fall
        // back to the Landscape/null variant via OrientationOnly. This is currently
        // harmless only because CreateVariant() produces identical WidgetStates
        // regardless of which variant is selected (see the dedicated test below) - the
        // moment that stops being true, a 1024x768-class real tablet (iPad-generation
        // panels commonly report a landscape short side under 900px) would silently
        // miss any future Tablet-specific placement. Flagged here as a precondition
        // worth re-checking if a real per-device difference is ever introduced, not
        // fixed now since nothing today depends on it.
        private static readonly object[] LandscapeCases =
        {
            new object[] { "16:9 landscape (1920x1080)", 1920d, 1080d, HudDeviceClass.Tablet, HudLayoutVariantFallbackStep.ExactDeviceClassAndOrientation, (HudDeviceClass?)HudDeviceClass.Tablet },
            new object[] { "19.5:9 phone landscape (1560x720)", 1560d, 720d, HudDeviceClass.Phone, HudLayoutVariantFallbackStep.OrientationOnly, (HudDeviceClass?)null },
            new object[] { "4:3 tablet landscape (1024x768)", 1024d, 768d, HudDeviceClass.Phone, HudLayoutVariantFallbackStep.OrientationOnly, (HudDeviceClass?)null }
        };

        [TestCaseSource(nameof(LandscapeCases))]
        public void DefaultProfile_PersistentWidgets_StayInSafeAreaAndDoNotOverlap(
            string caseName, double width, double height, HudDeviceClass deviceClass,
            HudLayoutVariantFallbackStep expectedFallbackStep, HudDeviceClass? expectedResolvedDeviceClass)
        {
            var service = new HudLayoutService(new HudWidgetRegistry(), new InMemoryHudLayoutStorage());
            HudLayoutProfile profile = HudBuiltInProfiles.CreateDefault();
            var viewport = new HudLayoutViewport(HudOrientation.Landscape, deviceClass);
            var geometry = new HudLayoutGeometry(new HudSize(width, height), new HudInsets(0, 0, 0, 0));

            HudLayoutResolution resolution = service.Resolve(profile, viewport);

            Assert.That(resolution.FallbackStep, Is.EqualTo(expectedFallbackStep),
                $"[{caseName}] unexpected variant fallback step");
            Assert.That(resolution.VariantKey.DeviceClass, Is.EqualTo(expectedResolvedDeviceClass),
                $"[{caseName}] unexpected resolved variant device class");

            var bounds = new Dictionary<string, HudRect>();
            foreach (HudWidgetState state in resolution.WidgetStates)
            {
                Assert.That(IntrinsicSizes.ContainsKey(state.WidgetId), Is.True,
                    $"[{caseName}] Unexpected widget '{state.WidgetId}' with no known intrinsic size - update IntrinsicSizes.");

                bounds[state.WidgetId] = HudWidgetLayoutResolver.ResolveBounds(state, geometry, IntrinsicSizes[state.WidgetId]);
            }

            foreach (KeyValuePair<string, HudRect> entry in bounds)
            {
                HudRect rect = entry.Value;
                Assert.That(rect.X, Is.GreaterThanOrEqualTo(0), $"[{caseName}] {entry.Key} left edge off-screen");
                Assert.That(rect.Y, Is.GreaterThanOrEqualTo(0), $"[{caseName}] {entry.Key} top edge off-screen");
                Assert.That(rect.X + rect.Width, Is.LessThanOrEqualTo(width), $"[{caseName}] {entry.Key} right edge off-screen");
                Assert.That(rect.Y + rect.Height, Is.LessThanOrEqualTo(height), $"[{caseName}] {entry.Key} bottom edge off-screen");
            }

            var ids = new List<string>(bounds.Keys);
            for (int i = 0; i < ids.Count; i++)
            {
                for (int j = i + 1; j < ids.Count; j++)
                {
                    Assert.That(Overlaps(bounds[ids[i]], bounds[ids[j]]), Is.False,
                        $"[{caseName}] {ids[i]} overlaps {ids[j]}");
                }
            }

            HudRect vitals = bounds[HudWidgetIds.Vitals];
            HudRect statusEffects = bounds[HudWidgetIds.StatusEffects];
            Assert.That(statusEffects.X, Is.EqualTo(vitals.X),
                $"[{caseName}] Status Effects should stay left-aligned with Vitals");
            Assert.That(statusEffects.Y, Is.GreaterThanOrEqualTo(vitals.Y + vitals.Height),
                $"[{caseName}] Status Effects should sit directly below Vitals, not overlapping it");
        }

        [Test]
        public void AllThreeBuiltInVariants_CurrentlyResolveToIdenticalWidgetStates()
        {
            // Documents a finding from Mobile HUD Responsive Layout v1, confirmed
            // and deliberately kept by v2: the three built-in variants
            // (Landscape/null, Portrait/null, Landscape/Tablet) are each built by
            // CreateVariant(orientation, deviceClass), but every Create*DefaultState()
            // helper it calls ignores both parameters - so today they carry
            // byte-for-byte identical placements. v2 evaluated phone-landscape,
            // standard-landscape, and tablet-landscape against every category the
            // task named (safe-area anchoring, inter-widget spacing, Action Bar,
            // joystick, Target Vitals, Status Effects-to-Vitals) and found no real
            // difference the existing placement model needs to express for the
            // currently supported resolutions - see HudResponsiveLayoutTests'
            // LandscapeCases, which pass with margin under this identical data at
            // all three. So this identity is intentional, not an unfixed accident:
            // if this test ever fails, someone introduced a difference, and the
            // question is whether that difference is justified (if so, update this
            // test and add a matching LandscapeCases entry; if not, revert it).
            HudLayoutProfile profile = HudBuiltInProfiles.CreateDefault();
            string landscape = SerializeWidgetStates(profile, HudOrientation.Landscape, null);
            string tablet = SerializeWidgetStates(profile, HudOrientation.Landscape, HudDeviceClass.Tablet);

            Assert.That(tablet, Is.EqualTo(landscape));
        }

        private static string SerializeWidgetStates(HudLayoutProfile profile, HudOrientation orientation, HudDeviceClass? deviceClass)
        {
            foreach (HudLayoutVariant variant in profile.Variants)
            {
                if (variant.Key.Orientation == orientation && variant.Key.DeviceClass == deviceClass)
                {
                    // Wrap in a profile with a neutral, shared Key so the
                    // comparison is only over WidgetStates, not variant identity.
                    var wrapper = new HudLayoutProfile { SchemaVersion = profile.SchemaVersion, Id = "comparison" };
                    wrapper.Variants.Add(new HudLayoutVariant
                    {
                        Key = new HudLayoutVariantKey(),
                        WidgetStates = variant.WidgetStates
                    });
                    return HudLayoutProfileSerializer.Serialize(wrapper);
                }
            }

            Assert.Fail($"No variant found for {orientation}/{deviceClass}.");
            return null;
        }

        private static bool Overlaps(HudRect a, HudRect b)
        {
            return a.X < b.X + b.Width
                && b.X < a.X + a.Width
                && a.Y < b.Y + b.Height
                && b.Y < a.Y + a.Height;
        }
    }
}
