using System;
using System.Collections.Generic;

namespace Hollowmere.Hud
{
    /// <summary>
    /// Renderer-neutral HUD coordinator. Resolves layout through
    /// <see cref="HudLayoutService"/> and applies it through registered presenters.
    /// </summary>
    public sealed class HudWidgetHost
    {
        private readonly HudWidgetRegistry registry;
        private readonly HudLayoutService layoutService;
        private readonly HudWidgetPresenterRegistry presenterRegistry;
        private readonly Dictionary<string, IHudWidgetPresenter> activePresenters =
            new Dictionary<string, IHudWidgetPresenter>(StringComparer.Ordinal);

        private HudLayoutProfile activeProfile;
        private HudLayoutProfile defaultTemplate;
        private HudLayoutViewport viewport;
        private HudLayoutGeometry geometry;
        private bool hasViewport;
        private bool hasGeometry;

        public HudWidgetHost(
            HudWidgetRegistry registry,
            HudLayoutService layoutService,
            HudWidgetPresenterRegistry presenterRegistry)
        {
            this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
            this.layoutService = layoutService ?? throw new ArgumentNullException(nameof(layoutService));
            this.presenterRegistry = presenterRegistry ?? throw new ArgumentNullException(nameof(presenterRegistry));
        }

        public HudLayoutResolution CurrentResolution { get; private set; }

        public event Action<HudLayoutResolution> LayoutResolved;

        public IReadOnlyList<string> GetActivePresenterWidgetIds()
        {
            return new List<string>(activePresenters.Keys);
        }

        public void SetDefaultTemplate(HudLayoutProfile builtInProfile)
        {
            defaultTemplate = builtInProfile;
        }

        public void SetActiveProfile(HudLayoutProfile profile)
        {
            activeProfile = profile ?? throw new ArgumentNullException(nameof(profile));

            if (hasViewport && hasGeometry)
            {
                RefreshLayout(geometry);
            }
        }

        public bool SetViewport(HudLayoutViewport newViewport)
        {
            if (hasViewport && newViewport.Equals(viewport))
            {
                return false;
            }

            viewport = newViewport;
            hasViewport = true;
            return true;
        }

        public void RefreshLayout(HudLayoutGeometry layoutGeometry)
        {
            if (activeProfile == null)
            {
                throw new InvalidOperationException("HUD host requires an active profile before layout refresh.");
            }

            if (!hasViewport)
            {
                throw new InvalidOperationException("HUD host requires a viewport before layout refresh.");
            }

            geometry = layoutGeometry;
            hasGeometry = true;

            CurrentResolution = layoutService.Resolve(activeProfile, viewport, defaultTemplate);
            SyncPresenters();
            LayoutResolved?.Invoke(CurrentResolution);
        }

        public void TickPresenters()
        {
            foreach (IHudWidgetPresenter presenter in activePresenters.Values)
            {
                if (presenter is IHudWidgetPresenterTick tick)
                {
                    tick.Tick();
                }
            }
        }

        public void DisposePresenters()
        {
            foreach (KeyValuePair<string, IHudWidgetPresenter> entry in activePresenters)
            {
                entry.Value.Dispose();
            }

            activePresenters.Clear();
        }

        private void SyncPresenters()
        {
            var desired = new HashSet<string>(StringComparer.Ordinal);

            foreach (HudWidgetState state in CurrentResolution.WidgetStates)
            {
                if (state == null || string.IsNullOrWhiteSpace(state.WidgetId))
                {
                    continue;
                }

                if (!registry.TryResolve(state.WidgetId, out HudWidgetDefinition definition))
                {
                    continue;
                }

                desired.Add(state.WidgetId);

                if (!activePresenters.TryGetValue(state.WidgetId, out IHudWidgetPresenter presenter))
                {
                    if (!presenterRegistry.TryCreate(definition, out presenter))
                    {
                        desired.Remove(state.WidgetId);
                        continue;
                    }

                    activePresenters[state.WidgetId] = presenter;
                }

                HudRect bounds = HudWidgetLayoutResolver.ResolveBounds(state, geometry, presenter.GetIntrinsicSize());
                presenter.Apply(new HudResolvedWidgetLayout(state, bounds));
            }

            var toRemove = new List<string>();

            foreach (KeyValuePair<string, IHudWidgetPresenter> entry in activePresenters)
            {
                if (!desired.Contains(entry.Key))
                {
                    entry.Value.Dispose();
                    toRemove.Add(entry.Key);
                }
            }

            foreach (string id in toRemove)
            {
                activePresenters.Remove(id);
            }
        }
    }
}
