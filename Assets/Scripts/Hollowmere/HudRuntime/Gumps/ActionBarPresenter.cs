using System;
using System.Collections.Generic;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using Hollowmere.Hud;
using Hollowmere.Hud.Unity;
using Hollowmere.Interaction;
using UnityEngine;

namespace Hollowmere.HudRuntime.Gumps
{
    /// <summary>
    /// Gump presenter for the main Action Bar. Wraps <see cref="ActionBar"/> and its
    /// existing persistence, catalog, and AnchorManager grouping.
    /// </summary>
    public sealed class ActionBarPresenter : IHudWidgetPresenter, IHudWidgetPresenterTick
    {
        public const int ButtonWidth = 88;
        public const int ButtonHeight = 44;
        private const float MaxRowWidthFraction = 0.45f;

        private readonly string widgetId;
        private ActionBar bar;

        public static int LiveInstanceCount { get; private set; }

        public ActionBarPresenter(HudWidgetDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            widgetId = definition.Id;
        }

        public string WidgetId => widgetId;

        public HudSize GetIntrinsicSize() => new HudSize(ButtonWidth, ButtonHeight);

        public void Apply(HudResolvedWidgetLayout layout)
        {
            if (layout.State != null && !layout.State.IsVisible)
            {
                if (bar != null)
                {
                    foreach (ActionButtonGump button in bar.Buttons)
                    {
                        button.IsVisible = false;
                    }
                }

                return;
            }

            EnsureMaterialized(layout.Bounds);

            if (bar == null)
            {
                return;
            }

            foreach (ActionButtonGump button in bar.Buttons)
            {
                button.IsVisible = true;
            }

            bar.RelocateOrigin((int)Math.Round(layout.Bounds.X), (int)Math.Round(layout.Bounds.Y));
        }

        public void Tick()
        {
            if (bar == null)
            {
                return;
            }

            if (ClassicUO.Client.Game == null || !(ClassicUO.Client.Game.Scene is GameScene))
            {
                TeardownBar();
                return;
            }

            bar.Tick();
        }

        public void Dispose()
        {
            TeardownBar();
        }

        private void EnsureMaterialized(HudRect bounds)
        {
            if (bar != null)
            {
                return;
            }

            if (ClassicUO.Client.Game == null || !(ClassicUO.Client.Game.Scene is GameScene))
            {
                return;
            }

            var persistence = new PlayerPrefsActionBarPersistence();
            persistence.TryLoadContents(
                ActionBarBootstrap.PersistenceKey,
                out IReadOnlyList<string> savedContents);

            int viewportWidth = ClassicUO.Client.Game.Window.ClientBounds.Width;
            int defaultX = (int)Math.Round(bounds.X);
            int defaultY = (int)Math.Round(bounds.Y);
            int maxPerRow = Mathf.Max(1, Mathf.FloorToInt(viewportWidth * MaxRowWidthFraction / ButtonWidth));

            bar = new ActionBar(
                ActionBarBootstrap.PersistenceKey,
                ActionBarLayout.HorizontalRightToLeft,
                defaultX,
                defaultY,
                persistence,
                maxPerRow);

            ActionBarBootstrap.SetCurrent(bar);
            LiveInstanceCount++;
            HudSmokeTestBridge.RegisterActionBarLive(1);

            bar.AddButton(
                new GameActionDefinition(
                    "Target Last",
                    TargetManager.TargetLast,
                    () => TargetManager.IsTargeting));

            bar.AddButton(new GameActionDefinition("+", () => ActionPickerPresenter.Instance?.Open(bar)));

            foreach (string refId in savedContents)
            {
                if (ActionCatalog.TryResolve(refId, out CatalogEntry entry))
                {
                    bar.AddButton(entry.Definition, removable: true);
                }
            }
        }

        private void TeardownBar()
        {
            if (bar == null)
            {
                return;
            }

            bar.Clear();

            if (ActionBarBootstrap.Current == bar)
            {
                ActionBarBootstrap.SetCurrent(null);
            }

            bar = null;
            LiveInstanceCount = Math.Max(0, LiveInstanceCount - 1);
            HudSmokeTestBridge.RegisterActionBarLive(-1);
        }
    }
}
