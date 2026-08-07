using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Hollowmere.Hud.Tests
{
    public class HudWidgetRegistryTests
    {
        [Test]
        public void Lookup_ReturnsActiveDefinitionForRegisteredWidget()
        {
            var registry = new HudWidgetRegistry();
            registry.Register(Definition("hollowmere.joystick"));

            HudWidgetLookup lookup = registry.Lookup("hollowmere.joystick");

            Assert.That(lookup.Status, Is.EqualTo(HudWidgetLookupStatus.Active));
            Assert.That(lookup.CanonicalId, Is.EqualTo("hollowmere.joystick"));
            Assert.That(lookup.Definition.Id, Is.EqualTo("hollowmere.joystick"));
        }

        [Test]
        public void Definitions_RemainUniqueAcrossMultipleRegistrations()
        {
            var registry = new HudWidgetRegistry();
            registry.Register(Definition("hollowmere.action-bar"));
            registry.Register(Definition("hollowmere.primary-action"));

            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (HudWidgetDefinition definition in registry.Definitions)
            {
                Assert.That(ids.Add(definition.Id), Is.True, $"Duplicate definition ID '{definition.Id}'.");
            }
        }

        [Test]
        public void Register_RejectsDuplicateWidgetIds()
        {
            var registry = new HudWidgetRegistry();
            registry.Register(Definition("hollowmere.primary-action"));

            Assert.Throws<InvalidOperationException>(() => registry.Register(Definition("hollowmere.primary-action")));
        }

        [Test]
        public void RegisterAlias_MigratesToCanonicalDefinition()
        {
            var registry = new HudWidgetRegistry();
            registry.Register(Definition("hollowmere.primary-action"));
            registry.RegisterAlias("hollowmere.primary", "hollowmere.primary-action");
            registry.Validate();

            HudWidgetLookup lookup = registry.Lookup("hollowmere.primary");

            Assert.That(lookup.Status, Is.EqualTo(HudWidgetLookupStatus.Alias));
            Assert.That(lookup.CanonicalId, Is.EqualTo("hollowmere.primary-action"));
            Assert.That(lookup.Definition.Id, Is.EqualTo("hollowmere.primary-action"));
        }

        [Test]
        public void RegisterDeprecated_PreservesTombstoneWithoutResolution()
        {
            var registry = new HudWidgetRegistry();
            registry.RegisterDeprecated("hollowmere.removed-widget");

            HudWidgetLookup lookup = registry.Lookup("hollowmere.removed-widget");

            Assert.That(lookup.Status, Is.EqualTo(HudWidgetLookupStatus.Deprecated));
            Assert.That(registry.TryResolve("hollowmere.removed-widget", out _), Is.False);
        }

        [Test]
        public void AliasRegistrationOrder_DoesNotMatterAfterValidation()
        {
            var registry = new HudWidgetRegistry();
            registry.RegisterAlias("hollowmere.old", "hollowmere.new");
            registry.Register(Definition("hollowmere.new"));

            Assert.DoesNotThrow(registry.Validate);
            Assert.That(registry.TryResolve("hollowmere.old", out HudWidgetDefinition definition), Is.True);
            Assert.That(definition.Id, Is.EqualTo("hollowmere.new"));
        }

        [Test]
        public void Validate_RejectsAliasWithMissingTarget()
        {
            var registry = new HudWidgetRegistry();
            registry.RegisterAlias("hollowmere.old", "hollowmere.missing");

            Assert.Throws<InvalidOperationException>(registry.Validate);
        }

        [Test]
        public void Registry_RejectsIdReuseAcrossCanonicalAliasAndDeprecatedEntries()
        {
            var registry = new HudWidgetRegistry();
            registry.Register(Definition("hollowmere.active"));
            registry.RegisterAlias("hollowmere.legacy", "hollowmere.active");
            registry.RegisterDeprecated("hollowmere.removed");

            Assert.Throws<InvalidOperationException>(() => registry.RegisterAlias("hollowmere.active", "hollowmere.other"));
            Assert.Throws<InvalidOperationException>(() => registry.Register(Definition("hollowmere.legacy")));
            Assert.Throws<InvalidOperationException>(() => registry.Register(Definition("hollowmere.removed")));
        }

        private static HudWidgetDefinition Definition(string id)
        {
            return new HudWidgetDefinition(id, HudWidgetRendererKind.Gump, HudWidgetCapabilities.Movable);
        }
    }
}
