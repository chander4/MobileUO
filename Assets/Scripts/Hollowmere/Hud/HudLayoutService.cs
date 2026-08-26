using System;
using System.Collections.Generic;

namespace Hollowmere.Hud
{
    /// <summary>
    /// Loads, validates, migrates, selects variants, resolves widget layout, and
    /// persists profiles. Layout policy lives here; <see cref="HudWidgetHost"/> applies
    /// resolved geometry through presenters.
    /// </summary>
    public sealed class HudLayoutService
    {
        private readonly HudWidgetRegistry registry;
        private readonly IHudLayoutStorage storage;
        private readonly Dictionary<string, HudLayoutProfile> builtInProfiles =
            new Dictionary<string, HudLayoutProfile>(StringComparer.Ordinal);

        public HudLayoutService(HudWidgetRegistry registry, IHudLayoutStorage storage)
        {
            this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public void RegisterBuiltInProfile(HudLayoutProfile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            if (string.IsNullOrWhiteSpace(profile.Id))
            {
                throw new ArgumentException("Built-in profile ID is required.", nameof(profile));
            }

            HudLayoutProfile migrated = HudLayoutProfileSerializer.Migrate(profile);
            HudLayoutProfileValidationResult validation = HudLayoutProfileSerializer.Validate(migrated);

            if (!validation.IsValid)
            {
                throw new InvalidOperationException("Invalid built-in HUD profile: " + string.Join("; ", validation.Errors));
            }

            builtInProfiles[migrated.Id] = HudLayoutProfileSerializer.Clone(migrated);
        }

        public bool TryGetBuiltInProfile(string profileId, out HudLayoutProfile profile)
        {
            return builtInProfiles.TryGetValue(profileId, out profile);
        }

        public HudLayoutProfile CreatePlayerProfileFromBuiltIn(string builtInId, string playerProfileId, string displayName)
        {
            if (!builtInProfiles.TryGetValue(builtInId, out HudLayoutProfile builtIn))
            {
                throw new InvalidOperationException($"Unknown built-in HUD profile '{builtInId}'.");
            }

            return builtIn.CopyAs(playerProfileId, displayName);
        }

        public bool TryLoad(string profileId, out HudLayoutProfile profile, out string error)
        {
            if (storage.TryLoad(profileId, out profile, out error))
            {
                profile = HudLayoutProfileSerializer.Migrate(profile);
                return true;
            }

            profile = null;
            return false;
        }

        public void Save(HudLayoutProfile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            storage.Save(profile);
        }

        public HudLayoutVariantSelection SelectVariant(HudLayoutProfile profile, HudLayoutViewport viewport)
        {
            HudLayoutProfile migrated = HudLayoutProfileSerializer.Migrate(profile);
            return HudLayoutVariantSelector.Select(migrated, viewport);
        }

        public HudLayoutResolution Resolve(
            HudLayoutProfile profile,
            HudLayoutViewport viewport,
            HudLayoutProfile defaultTemplate = null)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            HudLayoutProfile migrated = HudLayoutProfileSerializer.Migrate(profile);
            HudLayoutVariantSelection selection = HudLayoutVariantSelector.Select(migrated, viewport);
            List<HudWidgetState> merged = HudLayoutModelUtility.CloneStates(selection.Variant.WidgetStates);
            var warnings = new List<string>();

            if (defaultTemplate != null)
            {
                HudLayoutVariantSelection defaultSelection =
                    HudLayoutVariantSelector.Select(HudLayoutProfileSerializer.Migrate(defaultTemplate), viewport);
                MergeMissingWidgetStates(merged, defaultSelection.Variant.WidgetStates);
            }

            AppendRegistryWarnings(merged, warnings);

            return new HudLayoutResolution(
                selection.FallbackStep,
                selection.ResolvedKey,
                merged,
                warnings);
        }

        public string ExportToJson(HudLayoutProfile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            return HudLayoutProfileSerializer.Serialize(profile);
        }

        public bool TryImportFromJson(string json, out HudLayoutProfile profile, out string error)
        {
            return HudLayoutProfileSerializer.TryDeserialize(json, out profile, out error);
        }

        private void MergeMissingWidgetStates(List<HudWidgetState> target, IEnumerable<HudWidgetState> defaults)
        {
            var existing = new HashSet<string>(StringComparer.Ordinal);

            foreach (HudWidgetState state in target)
            {
                if (!string.IsNullOrWhiteSpace(state?.WidgetId))
                {
                    existing.Add(state.WidgetId);
                }
            }

            foreach (HudWidgetState state in defaults ?? Array.Empty<HudWidgetState>())
            {
                if (state == null || string.IsNullOrWhiteSpace(state.WidgetId) || existing.Contains(state.WidgetId))
                {
                    continue;
                }

                target.Add(HudLayoutModelUtility.CloneState(state));
                existing.Add(state.WidgetId);
            }
        }

        private void AppendRegistryWarnings(IEnumerable<HudWidgetState> states, List<string> warnings)
        {
            foreach (HudWidgetState state in states ?? Array.Empty<HudWidgetState>())
            {
                if (state == null || string.IsNullOrWhiteSpace(state.WidgetId))
                {
                    continue;
                }

                HudWidgetLookup lookup = registry.Lookup(state.WidgetId);

                if (lookup.Status == HudWidgetLookupStatus.Unknown)
                {
                    warnings.Add($"Unknown HUD widget ID '{state.WidgetId}' retained in layout.");
                }
                else if (lookup.Status == HudWidgetLookupStatus.Deprecated)
                {
                    warnings.Add($"Deprecated HUD widget ID '{state.WidgetId}' retained in layout.");
                }
            }
        }
    }
}
