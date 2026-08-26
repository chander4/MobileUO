namespace Hollowmere.Hud
{
    public static class HudLayoutProfileIds
    {
        public const string BuiltInDefault = "hollowmere.builtin.default";
        public const string PlayerActive = "hollowmere.player.active";
    }

    /// <summary>
    /// Code-registered default layout templates. Copied into player profiles once;
    /// never written back over customized player data.
    /// </summary>
    public static class HudBuiltInProfiles
    {
        private const int LegacyButtonWidth = 88;
        private const int LegacyRightMargin = 100;
        private const int LegacyTopMargin = 100;

        private const int ActionBarButtonWidth = 88;
        private const int ActionBarButtonHeight = 44;
        private const int ActionBarRightMargin = 20;
        private const int ActionBarBottomMargin = 24;

        private const int VitalsLeftMargin = 16;
        private const int VitalsTopMargin = 16;

        public static HudLayoutProfile CreateDefault()
        {
            var profile = new HudLayoutProfile
            {
                SchemaVersion = HudLayoutProfileSerializer.CurrentSchemaVersion,
                Id = HudLayoutProfileIds.BuiltInDefault,
                DisplayName = "Hollowmere Default"
            };

            profile.Variants.Add(CreateVariant(HudOrientation.Landscape, null));
            profile.Variants.Add(CreateVariant(HudOrientation.Portrait, null));
            profile.Variants.Add(CreateVariant(HudOrientation.Landscape, HudDeviceClass.Tablet));

            return profile;
        }

        private static HudLayoutVariant CreateVariant(HudOrientation orientation, HudDeviceClass? deviceClass)
        {
            return new HudLayoutVariant
            {
                Key = new HudLayoutVariantKey { Orientation = orientation, DeviceClass = deviceClass },
                WidgetStates =
                {
                    CreateVitalsDefaultState(),
                    CreatePrimaryActionDefaultState(),
                    CreateActionBarDefaultState()
                }
            };
        }

        /// <summary>
        /// Top-left vitals strip; keeps clear of Primary Action (top-right) and Action Bar (bottom-right).
        /// </summary>
        internal static HudWidgetState CreateVitalsDefaultState()
        {
            return new HudWidgetState
            {
                WidgetId = HudWidgetIds.Vitals,
                IsVisible = true,
                Placement = new HudPlacement
                {
                    Anchor = new HudPoint(0, 0),
                    Pivot = new HudPoint(0, 0),
                    LogicalOffset = new HudPoint(
                        VitalsLeftMargin / 10d,
                        VitalsTopMargin / 10d),
                    Scale = 1
                }
            };
        }

        /// <summary>
        /// Matches the former PrimaryActionButtonBootstrap spawn (top-right inset),
        /// expressed as normalized safe-rectangle placement.
        /// </summary>
        internal static HudWidgetState CreatePrimaryActionDefaultState()
        {
            return new HudWidgetState
            {
                WidgetId = HudWidgetIds.PrimaryAction,
                IsVisible = true,
                Placement = new HudPlacement
                {
                    Anchor = new HudPoint(1, 0),
                    Pivot = new HudPoint(1, 0),
                    LogicalOffset = new HudPoint(
                        -(LegacyRightMargin + LegacyButtonWidth) / 10d,
                        LegacyTopMargin / 10d),
                    Scale = 1
                }
            };
        }

        /// <summary>
        /// Matches the former ActionBarBootstrap default spawn (bottom-right),
        /// expressed as normalized safe-rectangle placement for the bar anchor cell.
        /// </summary>
        internal static HudWidgetState CreateActionBarDefaultState()
        {
            return new HudWidgetState
            {
                WidgetId = HudWidgetIds.ActionBar,
                IsVisible = true,
                Placement = new HudPlacement
                {
                    Anchor = new HudPoint(1, 1),
                    Pivot = new HudPoint(1, 1),
                    LogicalOffset = new HudPoint(
                        -(ActionBarRightMargin + ActionBarButtonWidth) / 10d,
                        -(ActionBarBottomMargin + ActionBarButtonHeight) / 10d),
                    Scale = 1
                }
            };
        }
    }
}
