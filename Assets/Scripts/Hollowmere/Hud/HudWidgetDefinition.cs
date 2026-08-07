using System;
using System.Collections.Generic;

namespace Hollowmere.Hud
{
    public enum HudWidgetRendererKind
    {
        Gump,
        Unity
    }

    [Flags]
    public enum HudWidgetCapabilities
    {
        None = 0,
        Movable = 1 << 0,
        Hideable = 1 << 1,
        Resizable = 1 << 2,
        Dockable = 1 << 3
    }

    /// <summary>
    /// Immutable registration metadata for one permanent HUD widget identity.
    /// Renderer factories deliberately do not belong here yet: Issue #25A has no
    /// presenters or host, and this model must remain renderer-neutral.
    /// </summary>
    public sealed class HudWidgetDefinition
    {
        public string Id { get; }
        public HudWidgetRendererKind RendererKind { get; }
        public HudWidgetCapabilities Capabilities { get; }
        public IReadOnlyList<string> Categories { get; }

        public HudWidgetDefinition(
            string id,
            HudWidgetRendererKind rendererKind,
            HudWidgetCapabilities capabilities = HudWidgetCapabilities.None,
            IReadOnlyList<string> categories = null)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("A widget ID is required.", nameof(id));
            }

            Id = id;
            RendererKind = rendererKind;
            Capabilities = capabilities;
            Categories = categories ?? Array.Empty<string>();
        }
    }
}
