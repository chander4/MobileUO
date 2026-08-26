using System;
using System.Collections.Generic;

namespace Hollowmere.Hud
{
    public delegate IHudWidgetPresenter HudWidgetPresenterFactory(HudWidgetDefinition definition);

    /// <summary>
    /// Maps registered widget IDs to presenter factories. The host uses this instead of
    /// widget-specific branching.
    /// </summary>
    public sealed class HudWidgetPresenterRegistry
    {
        private readonly Dictionary<string, HudWidgetPresenterFactory> factories =
            new Dictionary<string, HudWidgetPresenterFactory>(StringComparer.Ordinal);

        public void Register(string widgetId, HudWidgetPresenterFactory factory)
        {
            if (string.IsNullOrWhiteSpace(widgetId))
            {
                throw new ArgumentException("A widget ID is required.", nameof(widgetId));
            }

            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            if (factories.ContainsKey(widgetId))
            {
                throw new InvalidOperationException($"HUD presenter factory for '{widgetId}' is already registered.");
            }

            factories.Add(widgetId, factory);
        }

        public bool TryCreate(HudWidgetDefinition definition, out IHudWidgetPresenter presenter)
        {
            presenter = null;

            if (definition == null || string.IsNullOrWhiteSpace(definition.Id))
            {
                return false;
            }

            if (!factories.TryGetValue(definition.Id, out HudWidgetPresenterFactory factory))
            {
                return false;
            }

            presenter = factory(definition);
            return presenter != null;
        }
    }
}
