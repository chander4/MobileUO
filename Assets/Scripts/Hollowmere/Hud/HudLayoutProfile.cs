using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Hollowmere.Hud
{
    [Serializable]
    public sealed class HudLayoutVariantKey : IEquatable<HudLayoutVariantKey>
    {
        public HudOrientation Orientation = HudOrientation.Landscape;
        public HudDeviceClass? DeviceClass;
        public string InputMode;

        public bool Equals(HudLayoutVariantKey other)
        {
            return other != null
                && Orientation == other.Orientation
                && DeviceClass == other.DeviceClass
                && string.Equals(InputMode, other.InputMode, StringComparison.Ordinal);
        }

        public override bool Equals(object obj) => Equals(obj as HudLayoutVariantKey);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Orientation;
                hash = (hash * 397) ^ (DeviceClass.HasValue ? (int)DeviceClass.Value : -1);
                hash = (hash * 397) ^ (InputMode == null ? 0 : StringComparer.Ordinal.GetHashCode(InputMode));
                return hash;
            }
        }
    }

    [Serializable]
    public sealed class HudWidgetState
    {
        public string WidgetId;
        public bool IsVisible = true;
        public bool IsLocked;
        public HudSizePolicy SizePolicy = HudSizePolicy.Intrinsic;
        public HudPlacement Placement = new HudPlacement();
    }

    /// <summary>
    /// A versioned payload owned by one widget. The HUD host persists it but does
    /// not deserialize its feature semantics or use it to make layout decisions.
    /// </summary>
    [Serializable]
    public sealed class HudWidgetSettings
    {
        public string WidgetId;
        public int SchemaVersion = 1;
        public string PayloadJson = "{}";
    }

    [Serializable]
    public sealed class HudLayoutVariant
    {
        public HudLayoutVariantKey Key = new HudLayoutVariantKey();
        public List<HudWidgetState> WidgetStates = new List<HudWidgetState>();
        public List<HudWidgetSettings> WidgetSettings = new List<HudWidgetSettings>();
    }

    [Serializable]
    public sealed class HudLayoutProfile
    {
        public int SchemaVersion;
        public string Id;
        public string DisplayName;
        public List<HudLayoutVariant> Variants = new List<HudLayoutVariant>();

        public HudLayoutProfile CopyAs(string id, string displayName)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("A profile ID is required.", nameof(id));
            }

            HudLayoutProfile copy = HudLayoutProfileSerializer.Clone(this);
            copy.Id = id;
            copy.DisplayName = displayName;
            return copy;
        }
    }

    public sealed class HudLayoutProfileValidationResult
    {
        public IReadOnlyList<string> Errors { get; }
        public bool IsValid => Errors.Count == 0;

        internal HudLayoutProfileValidationResult(List<string> errors)
        {
            Errors = errors;
        }
    }

    /// <summary>
    /// Pure C# profile serialization and one-way schema migration. Unknown widget
    /// IDs are intentionally preserved; registry-aware callers may report them as
    /// warnings but must not erase them during a round trip.
    /// </summary>
    /// <remarks>
    /// TODO(#25B+): <see cref="Migrate"/> and <see cref="Serialize"/> may mutate the
    /// input profile (schema bump, list normalization). Revisit copy-on-write or
    /// immutable snapshots once <c>HudLayoutService</c> and <c>HudWidgetHost</c> own
    /// in-memory profile lifecycles.
    /// </remarks>
    public static class HudLayoutProfileSerializer
    {
        public const int CurrentSchemaVersion = 1;

        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Ignore,
            Formatting = Formatting.None
        };

        public static string Serialize(HudLayoutProfile profile)
        {
            HudLayoutProfile migrated = Migrate(profile);
            HudLayoutProfileValidationResult validation = Validate(migrated);

            if (!validation.IsValid)
            {
                throw new InvalidOperationException("Invalid HUD layout profile: " + string.Join("; ", validation.Errors));
            }

            return JsonConvert.SerializeObject(migrated, SerializerSettings);
        }

        public static bool TryDeserialize(string json, out HudLayoutProfile profile, out string error)
        {
            profile = null;
            error = null;

            if (string.IsNullOrWhiteSpace(json))
            {
                error = "HUD layout profile JSON is empty.";
                return false;
            }

            try
            {
                HudLayoutProfile parsed = JsonConvert.DeserializeObject<HudLayoutProfile>(json, SerializerSettings);

                if (parsed == null)
                {
                    error = "HUD layout profile JSON did not contain a profile.";
                    return false;
                }

                profile = Migrate(parsed);
                HudLayoutProfileValidationResult validation = Validate(profile);

                if (!validation.IsValid)
                {
                    error = string.Join("; ", validation.Errors);
                    profile = null;
                    return false;
                }

                return true;
            }
            catch (JsonException exception)
            {
                error = exception.Message;
                return false;
            }
            catch (InvalidOperationException exception)
            {
                error = exception.Message;
                return false;
            }
        }

        public static HudLayoutProfile Clone(HudLayoutProfile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            string serialized = Serialize(profile);

            if (!TryDeserialize(serialized, out HudLayoutProfile copy, out string error))
            {
                throw new InvalidOperationException("HUD layout profile clone failed: " + error);
            }

            return copy;
        }

        public static HudLayoutProfile Migrate(HudLayoutProfile profile)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            if (profile.SchemaVersion > CurrentSchemaVersion)
            {
                throw new InvalidOperationException($"HUD layout schema {profile.SchemaVersion} is newer than supported schema {CurrentSchemaVersion}.");
            }

            if (profile.SchemaVersion == 0)
            {
                // Schema 0 was the pre-versioned profile shape. It has the same
                // fields as schema 1, so normalization plus an explicit version is
                // the complete one-way migration.
                profile.SchemaVersion = 1;
            }

            Normalize(profile);
            return profile;
        }

        public static HudLayoutProfileValidationResult Validate(HudLayoutProfile profile)
        {
            var errors = new List<string>();

            if (profile == null)
            {
                errors.Add("HUD layout profile is required.");
                return new HudLayoutProfileValidationResult(errors);
            }

            if (string.IsNullOrWhiteSpace(profile.Id))
            {
                errors.Add("HUD layout profile ID is required.");
            }

            var variantKeys = new HashSet<HudLayoutVariantKey>();

            foreach (HudLayoutVariant variant in profile.Variants ?? new List<HudLayoutVariant>())
            {
                if (variant == null || variant.Key == null)
                {
                    errors.Add("HUD layout variants require a key.");
                    continue;
                }

                if (!variantKeys.Add(variant.Key))
                {
                    errors.Add("HUD layout profile contains duplicate variant keys.");
                }

                ValidateWidgetIds(variant.WidgetStates, state => state == null ? null : state.WidgetId, "widget state", errors);
                ValidateWidgetIds(variant.WidgetSettings, settings => settings == null ? null : settings.WidgetId, "widget settings", errors);
            }

            return new HudLayoutProfileValidationResult(errors);
        }

        private static void Normalize(HudLayoutProfile profile)
        {
            if (profile.Variants == null)
            {
                profile.Variants = new List<HudLayoutVariant>();
            }

            foreach (HudLayoutVariant variant in profile.Variants)
            {
                if (variant == null)
                {
                    continue;
                }

                if (variant.Key == null)
                {
                    variant.Key = new HudLayoutVariantKey();
                }

                if (variant.WidgetStates == null)
                {
                    variant.WidgetStates = new List<HudWidgetState>();
                }

                if (variant.WidgetSettings == null)
                {
                    variant.WidgetSettings = new List<HudWidgetSettings>();
                }
            }
        }

        private static void ValidateWidgetIds<T>(IEnumerable<T> values, Func<T, string> getId, string label, List<string> errors)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);

            foreach (T value in values ?? Array.Empty<T>())
            {
                string id = getId(value);

                if (string.IsNullOrWhiteSpace(id))
                {
                    errors.Add($"HUD {label} requires a widget ID.");
                }
                else if (!ids.Add(id))
                {
                    errors.Add($"HUD variant contains duplicate {label} ID '{id}'.");
                }
            }
        }
    }
}
