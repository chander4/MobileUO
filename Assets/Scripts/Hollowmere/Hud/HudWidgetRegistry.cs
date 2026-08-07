using System;
using System.Collections.Generic;

namespace Hollowmere.Hud
{
    public enum HudWidgetLookupStatus
    {
        Unknown,
        Active,
        Alias,
        Deprecated
    }

    public readonly struct HudWidgetLookup
    {
        public HudWidgetLookupStatus Status { get; }
        public string RequestedId { get; }
        public string CanonicalId { get; }
        public HudWidgetDefinition Definition { get; }

        internal HudWidgetLookup(HudWidgetLookupStatus status, string requestedId, string canonicalId, HudWidgetDefinition definition)
        {
            Status = status;
            RequestedId = requestedId;
            CanonicalId = canonicalId;
            Definition = definition;
        }
    }

    /// <summary>
    /// The sole discovery and identity boundary for HUD widgets. Aliases are
    /// migration records, not alternate identities; deprecated IDs are retained
    /// tombstones so old profiles can be handled deliberately.
    /// </summary>
    public sealed class HudWidgetRegistry
    {
        private readonly Dictionary<string, HudWidgetDefinition> definitions = new Dictionary<string, HudWidgetDefinition>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> aliases = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly HashSet<string> deprecatedIds = new HashSet<string>(StringComparer.Ordinal);

        public IEnumerable<HudWidgetDefinition> Definitions => definitions.Values;

        public void Register(HudWidgetDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            EnsureAvailable(definition.Id);
            definitions.Add(definition.Id, definition);
        }

        public void RegisterAlias(string aliasId, string canonicalId)
        {
            EnsureId(aliasId, nameof(aliasId));
            EnsureId(canonicalId, nameof(canonicalId));
            EnsureAvailable(aliasId);

            if (string.Equals(aliasId, canonicalId, StringComparison.Ordinal))
            {
                throw new ArgumentException("An alias cannot point to itself.", nameof(aliasId));
            }

            aliases.Add(aliasId, canonicalId);
        }

        public void RegisterDeprecated(string id)
        {
            EnsureId(id, nameof(id));
            EnsureAvailable(id);
            deprecatedIds.Add(id);
        }

        public HudWidgetLookup Lookup(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return new HudWidgetLookup(HudWidgetLookupStatus.Unknown, id, null, null);
            }

            if (definitions.TryGetValue(id, out HudWidgetDefinition definition))
            {
                return new HudWidgetLookup(HudWidgetLookupStatus.Active, id, id, definition);
            }

            if (aliases.TryGetValue(id, out string canonicalId))
            {
                definitions.TryGetValue(canonicalId, out definition);
                return new HudWidgetLookup(definition == null ? HudWidgetLookupStatus.Unknown : HudWidgetLookupStatus.Alias, id, canonicalId, definition);
            }

            return deprecatedIds.Contains(id)
                ? new HudWidgetLookup(HudWidgetLookupStatus.Deprecated, id, null, null)
                : new HudWidgetLookup(HudWidgetLookupStatus.Unknown, id, null, null);
        }

        public bool TryResolve(string id, out HudWidgetDefinition definition)
        {
            HudWidgetLookup lookup = Lookup(id);
            definition = lookup.Definition;
            return lookup.Status == HudWidgetLookupStatus.Active || lookup.Status == HudWidgetLookupStatus.Alias;
        }

        /// <summary>
        /// Call after all built-in registrations. Aliases may be registered before
        /// their target so registration order does not matter; unresolved targets
        /// are rejected only at this explicit validation boundary.
        /// </summary>
        public void Validate()
        {
            foreach (KeyValuePair<string, string> alias in aliases)
            {
                if (!definitions.ContainsKey(alias.Value))
                {
                    throw new InvalidOperationException($"HUD widget alias '{alias.Key}' points to unknown widget '{alias.Value}'.");
                }
            }
        }

        private void EnsureAvailable(string id)
        {
            EnsureId(id, nameof(id));

            if (definitions.ContainsKey(id) || aliases.ContainsKey(id) || deprecatedIds.Contains(id))
            {
                throw new InvalidOperationException($"HUD widget ID '{id}' is already registered, aliased, or deprecated.");
            }
        }

        private static void EnsureId(string id, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("A non-empty widget ID is required.", parameterName);
            }
        }
    }
}
