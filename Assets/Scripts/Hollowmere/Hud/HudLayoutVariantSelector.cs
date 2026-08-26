using System;
using System.Collections.Generic;

namespace Hollowmere.Hud
{
    internal static class HudLayoutVariantSelector
    {
        public static HudLayoutVariantSelection Select(HudLayoutProfile profile, HudLayoutViewport viewport)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            IReadOnlyList<HudLayoutVariant> variants = profile.Variants;
            if (variants == null || variants.Count == 0)
            {
                throw new InvalidOperationException("HUD layout profile does not contain any variants.");
            }

            HudLayoutVariant exact = FindExact(variants, viewport);
            if (exact != null)
            {
                return CreateSelection(exact, HudLayoutVariantFallbackStep.ExactDeviceClassAndOrientation);
            }

            HudLayoutVariant orientationOnly = FindOrientationOnly(variants, viewport);
            if (orientationOnly != null)
            {
                return CreateSelection(orientationOnly, HudLayoutVariantFallbackStep.OrientationOnly);
            }

            HudLayoutVariant deviceBase = FindDeviceClassBase(variants, viewport);
            if (deviceBase != null)
            {
                return CreateSelection(deviceBase, HudLayoutVariantFallbackStep.DeviceClassBase);
            }

            HudLayoutVariant profileBase = FindProfileBase(variants);
            if (profileBase != null)
            {
                return CreateSelection(profileBase, HudLayoutVariantFallbackStep.ProfileBase);
            }

            throw new InvalidOperationException("HUD layout profile does not contain a resolvable variant.");
        }

        private static HudLayoutVariant FindExact(IReadOnlyList<HudLayoutVariant> variants, HudLayoutViewport viewport)
        {
            foreach (HudLayoutVariant variant in variants)
            {
                if (variant?.Key == null)
                {
                    continue;
                }

                if (variant.Key.Orientation == viewport.Orientation
                    && variant.Key.DeviceClass == viewport.DeviceClass
                    && InputModeMatches(variant.Key.InputMode, viewport.InputMode))
                {
                    return variant;
                }
            }

            return null;
        }

        private static HudLayoutVariant FindOrientationOnly(IReadOnlyList<HudLayoutVariant> variants, HudLayoutViewport viewport)
        {
            foreach (HudLayoutVariant variant in variants)
            {
                if (variant?.Key == null)
                {
                    continue;
                }

                if (variant.Key.Orientation == viewport.Orientation
                    && !variant.Key.DeviceClass.HasValue
                    && string.IsNullOrEmpty(variant.Key.InputMode))
                {
                    return variant;
                }
            }

            return null;
        }

        private static HudLayoutVariant FindDeviceClassBase(IReadOnlyList<HudLayoutVariant> variants, HudLayoutViewport viewport)
        {
            HudLayoutVariant fallback = null;

            foreach (HudLayoutVariant variant in variants)
            {
                if (variant?.Key == null || variant.Key.DeviceClass != viewport.DeviceClass)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(variant.Key.InputMode))
                {
                    fallback = variant;
                    if (variant.Key.Orientation == HudOrientation.Landscape)
                    {
                        return variant;
                    }
                }
            }

            return fallback;
        }

        private static HudLayoutVariant FindProfileBase(IReadOnlyList<HudLayoutVariant> variants)
        {
            HudLayoutVariant fallback = null;

            foreach (HudLayoutVariant variant in variants)
            {
                if (variant?.Key == null)
                {
                    continue;
                }

                if (!variant.Key.DeviceClass.HasValue && string.IsNullOrEmpty(variant.Key.InputMode))
                {
                    fallback = variant;
                    if (variant.Key.Orientation == HudOrientation.Landscape)
                    {
                        return variant;
                    }
                }
            }

            return fallback ?? (variants.Count > 0 ? variants[0] : null);
        }

        private static bool InputModeMatches(string keyInputMode, string viewportInputMode)
        {
            if (string.IsNullOrEmpty(keyInputMode) && string.IsNullOrEmpty(viewportInputMode))
            {
                return true;
            }

            return string.Equals(keyInputMode, viewportInputMode, StringComparison.Ordinal);
        }

        private static HudLayoutVariantSelection CreateSelection(HudLayoutVariant variant, HudLayoutVariantFallbackStep step)
        {
            return new HudLayoutVariantSelection(variant, variant.Key, step);
        }
    }
}
