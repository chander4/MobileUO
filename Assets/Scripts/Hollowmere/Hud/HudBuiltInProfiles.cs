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
        private const int ActionBarButtonWidth = 88;
        private const int ActionBarButtonHeight = 44;
        private const int ActionBarRightMargin = 20;
        private const int ActionBarBottomMargin = 24;

        // Mobile UX slice: Primary Action stacks above the Action Bar's rightmost
        // slot, both anchored to the bottom-right corner - the "thumb cluster" a
        // two-handed landscape/portrait grip can actually reach. The old top-right
        // placement (PrimaryActionButtonWidth/RightMargin/TopMargin, removed) was a
        // direct carry-over from the desktop-era PrimaryActionButtonBootstrap spawn
        // point, never revisited for touch.
        //
        // ActionBar wraps into multiple rows once a player has enough saved buttons
        // (ActionBarPresenter's own maxPerRow); HudBuiltInProfiles has no visibility
        // into that live, per-account row count. Live validation against a real
        // account (12 saved buttons, wrapping to 3 rows) showed the naive 1-row
        // clearance overlapping the topmost row - the LogicalOffset/10 convention
        // this whole file uses does not scale linearly with row count the way a raw
        // pixel value would, so "clear 3 rows" needed roughly 6 rows worth of
        // constant to actually clear 3 real rows with margin, confirmed by
        // re-measuring the presenter's actual live button bounds, not by
        // recalculating in the abstract. ClearedActionBarRows is a heuristic tuned
        // against that measurement (in a small Editor test viewport - real devices
        // have a larger logical-unit-to-pixel ratio, so this clearance is expected
        // to be more than sufficient there), not a structural guarantee - an
        // account with substantially more saved buttons could still see an overlap.
        // A fully robust fix would need Primary Action to read the Action Bar's
        // actual rendered row count, which is out of scope for this slice.
        private const int PrimaryActionButtonWidth = 88;
        private const int PrimaryActionButtonHeight = 44;
        private const int PrimaryActionRightMargin = ActionBarRightMargin;
        private const int PrimaryActionBarGap = 12;
        private const int ClearedActionBarRows = 6;

        private const int VitalsLeftMargin = 16;
        private const int VitalsTopMargin = 16;

        // Mirrors HealthBarGumpCustom.HPB_HEIGHT_MULTILINE (external/ClassicUO) -
        // the Vitals widget's own rendered height - so Status Effects clears it
        // with a small gap instead of overlapping. Same "measure, don't
        // re-derive" convention as ClearedActionBarRows above.
        //
        // Mobile HUD Responsive Layout v1: LogicalOffset is pixels-divided-by-10,
        // which only reproduces the intended raw-pixel gap when
        // HudPlacementMath.LogicalUnitSize(safeArea) ~= 10 (true at the ~1920x1080
        // editor viewport this was first tuned against - see the note above). Both
        // Vitals and Status Effects are FIXED pixel sizes, so on a real landscape
        // device with a smaller safe-area short side (project is landscape-locked -
        // allowedAutorotateToPortrait is 0 - so this is strictly a landscape
        // concern) the unit shrinks and the old TopGap=8 stopped clearing Vitals'
        // fixed 60px height at all: confirmed overlapping by ~11px at 1560x720
        // (19.5:9 budget phone landscape) and ~8px at 1024x768 (4:3 tablet
        // landscape) via HudResponsiveLayoutTests. 36 is the smallest round value
        // that still clears Vitals with a few px to spare at a 7.2 unit floor
        // (safe-area short side ~720px) while only widening the gap - never
        // shrinking it - at larger/higher-res safe areas.
        private const int VitalsHeightForClearance = 60;
        private const int StatusEffectsTopGap = 36;
        private const int StatusEffectsLeftMargin = VitalsLeftMargin;

        private const int StatsLeftMargin = 16;
        private const int StatsBottomMargin = 16;

        private const int TargetVitalsTopMargin = 16;

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
                    CreateStatusEffectsDefaultState(),
                    CreatePrimaryActionDefaultState(),
                    CreateActionBarDefaultState(),
                    CreateStatsDefaultState(),
                    CreateTargetVitalsDefaultState()
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
        /// Top-left, directly below Vitals - both are self-status widgets, so they
        /// share a corner rather than StatusEffects claiming its own.
        /// </summary>
        internal static HudWidgetState CreateStatusEffectsDefaultState()
        {
            return new HudWidgetState
            {
                WidgetId = HudWidgetIds.StatusEffects,
                IsVisible = true,
                Placement = new HudPlacement
                {
                    Anchor = new HudPoint(0, 0),
                    Pivot = new HudPoint(0, 0),
                    LogicalOffset = new HudPoint(
                        StatusEffectsLeftMargin / 10d,
                        (VitalsTopMargin + VitalsHeightForClearance + StatusEffectsTopGap) / 10d),
                    Scale = 1
                }
            };
        }

        /// <summary>
        /// Bottom-right, stacked directly above the Action Bar's rightmost slot -
        /// same corner, same thumb, one gap's worth of clearance above the bar.
        /// </summary>
        internal static HudWidgetState CreatePrimaryActionDefaultState()
        {
            return new HudWidgetState
            {
                WidgetId = HudWidgetIds.PrimaryAction,
                IsVisible = true,
                Placement = new HudPlacement
                {
                    Anchor = new HudPoint(1, 1),
                    Pivot = new HudPoint(1, 1),
                    LogicalOffset = new HudPoint(
                        -(PrimaryActionRightMargin + PrimaryActionButtonWidth) / 10d,
                        -(ActionBarBottomMargin + (ClearedActionBarRows * ActionBarButtonHeight) + PrimaryActionBarGap + PrimaryActionButtonHeight) / 10d),
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

        /// <summary>
        /// Bottom-left, the one corner not already used by Vitals (top-left),
        /// Primary Action (top-right), or Action Bar (bottom-right).
        /// </summary>
        internal static HudWidgetState CreateStatsDefaultState()
        {
            return new HudWidgetState
            {
                WidgetId = HudWidgetIds.Stats,
                IsVisible = true,
                Placement = new HudPlacement
                {
                    Anchor = new HudPoint(0, 1),
                    Pivot = new HudPoint(0, 1),
                    LogicalOffset = new HudPoint(
                        StatsLeftMargin / 10d,
                        -StatsBottomMargin / 10d),
                    Scale = 1
                }
            };
        }

        /// <summary>
        /// #34: top-center - the one edge not already claimed by Vitals/Primary Action
        /// (top corners) or Action Bar/Stats (bottom corners). Combat target status is
        /// the one piece of "who am I fighting" info that shouldn't have to be hunted
        /// for, so it gets the top-center slot rather than another corner.
        /// </summary>
        internal static HudWidgetState CreateTargetVitalsDefaultState()
        {
            return new HudWidgetState
            {
                WidgetId = HudWidgetIds.TargetVitals,
                IsVisible = true,
                Placement = new HudPlacement
                {
                    Anchor = new HudPoint(0.5, 0),
                    Pivot = new HudPoint(0.5, 0),
                    LogicalOffset = new HudPoint(0, TargetVitalsTopMargin / 10d),
                    Scale = 1
                }
            };
        }
    }
}
