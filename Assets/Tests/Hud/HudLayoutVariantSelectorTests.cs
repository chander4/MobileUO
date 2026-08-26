using System.Collections.Generic;
using NUnit.Framework;

namespace Hollowmere.Hud.Tests
{
    public class HudLayoutVariantSelectorTests
    {
        [Test]
        public void Select_UsesExactDeviceClassAndOrientationFirst()
        {
            HudLayoutProfile profile = CreateProfile();
            AddVariant(profile, HudOrientation.Landscape, null, "base");
            AddVariant(profile, HudOrientation.Landscape, HudDeviceClass.Tablet, "tablet");
            AddVariant(profile, HudOrientation.Portrait, null, "portrait");

            HudLayoutVariantSelection selection = new HudLayoutService(
                new HudWidgetRegistry(),
                new InMemoryHudLayoutStorage()).SelectVariant(
                profile,
                new HudLayoutViewport(HudOrientation.Landscape, HudDeviceClass.Tablet));

            Assert.That(selection.FallbackStep, Is.EqualTo(HudLayoutVariantFallbackStep.ExactDeviceClassAndOrientation));
            Assert.That(selection.Variant.WidgetStates[0].WidgetId, Is.EqualTo("tablet"));
        }

        [Test]
        public void Select_FallsBackToOrientationOnly()
        {
            HudLayoutProfile profile = CreateProfile();
            AddVariant(profile, HudOrientation.Landscape, null, "landscape");
            AddVariant(profile, HudOrientation.Portrait, null, "portrait");

            HudLayoutVariantSelection selection = new HudLayoutService(
                new HudWidgetRegistry(),
                new InMemoryHudLayoutStorage()).SelectVariant(
                profile,
                new HudLayoutViewport(HudOrientation.Portrait, HudDeviceClass.Tablet));

            Assert.That(selection.FallbackStep, Is.EqualTo(HudLayoutVariantFallbackStep.OrientationOnly));
            Assert.That(selection.Variant.WidgetStates[0].WidgetId, Is.EqualTo("portrait"));
        }

        [Test]
        public void Select_FallsBackToDeviceClassBase()
        {
            HudLayoutProfile profile = CreateProfile();
            AddVariant(profile, HudOrientation.Landscape, null, "landscape-base");
            AddVariant(profile, HudOrientation.Landscape, HudDeviceClass.Tablet, "tablet-landscape");

            HudLayoutVariantSelection selection = new HudLayoutService(
                new HudWidgetRegistry(),
                new InMemoryHudLayoutStorage()).SelectVariant(
                profile,
                new HudLayoutViewport(HudOrientation.Portrait, HudDeviceClass.Tablet));

            Assert.That(selection.FallbackStep, Is.EqualTo(HudLayoutVariantFallbackStep.DeviceClassBase));
            Assert.That(selection.Variant.WidgetStates[0].WidgetId, Is.EqualTo("tablet-landscape"));
        }

        [Test]
        public void Select_FallsBackToProfileBase()
        {
            HudLayoutProfile profile = CreateProfile();
            AddVariant(profile, HudOrientation.Landscape, null, "base");

            HudLayoutVariantSelection selection = new HudLayoutService(
                new HudWidgetRegistry(),
                new InMemoryHudLayoutStorage()).SelectVariant(
                profile,
                new HudLayoutViewport(HudOrientation.Portrait, HudDeviceClass.Phone));

            Assert.That(selection.FallbackStep, Is.EqualTo(HudLayoutVariantFallbackStep.ProfileBase));
            Assert.That(selection.Variant.WidgetStates[0].WidgetId, Is.EqualTo("base"));
        }

        private static HudLayoutProfile CreateProfile()
        {
            return new HudLayoutProfile
            {
                Id = "test",
                DisplayName = "test",
                SchemaVersion = HudLayoutProfileSerializer.CurrentSchemaVersion
            };
        }

        private static void AddVariant(
            HudLayoutProfile profile,
            HudOrientation orientation,
            HudDeviceClass? deviceClass,
            string marker)
        {
            profile.Variants.Add(new HudLayoutVariant
            {
                Key = new HudLayoutVariantKey { Orientation = orientation, DeviceClass = deviceClass },
                WidgetStates = new List<HudWidgetState>
                {
                    new HudWidgetState { WidgetId = marker }
                }
            });
        }
    }
}
