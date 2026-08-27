using Hollowmere.Hud;
using Hollowmere.Hud.Unity;
using Hollowmere.HudRuntime.Gumps;
using UnityEngine;

namespace Hollowmere.HudRuntime
{
    /// <summary>
    /// Registers Gump presenters and wires the HUD host at runtime.
    /// </summary>
    public static class HudWidgetHostComposition
    {
        internal static HudWidgetHost Host { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var go = new GameObject("HollowmereHudWidgetHostComposition");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<Runner>();
        }

        private sealed class Runner : MonoBehaviour
        {
            private HudWidgetHost host;
            private HudLayoutViewport lastViewport;
            private HudLayoutGeometry lastGeometry;
            private bool hasSnapshot;

            private void Awake()
            {
                HudSmokeTestBridge.NotifyCompositionRunnerSpawned();
                var registry = new HudWidgetRegistry();
                registry.Register(new HudWidgetDefinition(
                    HudWidgetIds.PrimaryAction,
                    HudWidgetRendererKind.Gump,
                    HudWidgetCapabilities.Movable | HudWidgetCapabilities.Hideable));
                registry.Register(new HudWidgetDefinition(
                    HudWidgetIds.ActionBar,
                    HudWidgetRendererKind.Gump,
                    HudWidgetCapabilities.Movable | HudWidgetCapabilities.Hideable | HudWidgetCapabilities.Dockable));
                registry.Register(new HudWidgetDefinition(
                    HudWidgetIds.Vitals,
                    HudWidgetRendererKind.Gump,
                    HudWidgetCapabilities.Movable | HudWidgetCapabilities.Hideable));
                registry.Register(new HudWidgetDefinition(
                    HudWidgetIds.Stats,
                    HudWidgetRendererKind.Gump,
                    HudWidgetCapabilities.Movable | HudWidgetCapabilities.Hideable));

                var presenterRegistry = new HudWidgetPresenterRegistry();
                presenterRegistry.Register(
                    HudWidgetIds.PrimaryAction,
                    definition => new PrimaryActionPresenter(definition));
                presenterRegistry.Register(
                    HudWidgetIds.ActionBar,
                    definition => new ActionBarPresenter(definition));
                presenterRegistry.Register(
                    HudWidgetIds.Vitals,
                    definition => new VitalsPresenter(definition));
                presenterRegistry.Register(
                    HudWidgetIds.Stats,
                    definition => new StatsPresenter(definition));

                registry.Validate();

                HudLayoutProfile builtInDefault = HudBuiltInProfiles.CreateDefault();
                var layoutService = new HudLayoutService(registry, new PlayerPrefsHudLayoutStorage());
                layoutService.RegisterBuiltInProfile(builtInDefault);

                if (!layoutService.TryLoad(HudLayoutProfileIds.PlayerActive, out HudLayoutProfile activeProfile, out _))
                {
                    activeProfile = layoutService.CreatePlayerProfileFromBuiltIn(
                        HudLayoutProfileIds.BuiltInDefault,
                        HudLayoutProfileIds.PlayerActive,
                        "Player");
                    layoutService.Save(activeProfile);
                }

                host = new HudWidgetHost(registry, layoutService, presenterRegistry);
                Host = host;
                HudSmokeTestBridge.NotifyHostCreated(host);
                host.SetDefaultTemplate(builtInDefault);
                host.SetActiveProfile(activeProfile);
            }

            private void Update()
            {
                if (host == null)
                {
                    return;
                }

                if (!TryGetGeometry(out HudLayoutGeometry geometry, out HudLayoutViewport viewport))
                {
                    host.DisposePresenters();
                    hasSnapshot = false;
                    return;
                }

                host.TickPresenters();

                if (hasSnapshot
                    && viewport.Equals(lastViewport)
                    && GeometryEquals(geometry, lastGeometry))
                {
                    return;
                }

                lastGeometry = geometry;
                lastViewport = viewport;
                hasSnapshot = true;
                host.SetViewport(viewport);
                host.RefreshLayout(geometry);
                HudSmokeTestBridge.NotifyLayoutResolved(host.CurrentResolution);
            }

            private void OnDestroy()
            {
                host?.DisposePresenters();
                HudSmokeTestBridge.NotifyCompositionRunnerDestroyed();

                if (Host == host)
                {
                    Host = null;
                }
            }

            private static bool GeometryEquals(HudLayoutGeometry left, HudLayoutGeometry right)
            {
                return left.ViewportSize.Width == right.ViewportSize.Width
                    && left.ViewportSize.Height == right.ViewportSize.Height
                    && left.SafeAreaInsets.Left == right.SafeAreaInsets.Left
                    && left.SafeAreaInsets.Top == right.SafeAreaInsets.Top
                    && left.SafeAreaInsets.Right == right.SafeAreaInsets.Right
                    && left.SafeAreaInsets.Bottom == right.SafeAreaInsets.Bottom;
            }

            private static bool TryGetGeometry(out HudLayoutGeometry geometry, out HudLayoutViewport viewport)
            {
                geometry = default;
                viewport = default;

                if (ClassicUO.Client.Game == null
                    || !(ClassicUO.Client.Game.Scene is ClassicUO.Game.Scenes.GameScene))
                {
                    return false;
                }

                var bounds = ClassicUO.Client.Game.Window.ClientBounds;
                var viewportSize = new HudSize(bounds.Width, bounds.Height);
                geometry = new HudLayoutGeometry(viewportSize, new HudInsets(0, 0, 0, 0));
                viewport = new HudLayoutViewport(
                    bounds.Height >= bounds.Width ? HudOrientation.Portrait : HudOrientation.Landscape,
                    Mathf.Min(bounds.Width, bounds.Height) >= 900 ? HudDeviceClass.Tablet : HudDeviceClass.Phone);
                return true;
            }
        }
    }
}
