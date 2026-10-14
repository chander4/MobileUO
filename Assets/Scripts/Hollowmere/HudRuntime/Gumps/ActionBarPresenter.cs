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

        public HudSize GetIntrinsicSize() => new HudSize(ActionButtonGump.ButtonWidth, ActionButtonGump.ButtonHeight);

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
            int maxPerRow = Mathf.Max(1, Mathf.FloorToInt(viewportWidth * MaxRowWidthFraction / ActionButtonGump.ButtonWidth));

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

            // #34: re-engage whoever was last attacked, distinct from "Target Last" above
            // (which only answers a currently-pending server target-cursor - see
            // TargetManager.TargetLast). GameActions.Attack already maintains
            // TargetManager.LastAttack itself; this only observes it. Validity re-checked
            // every frame via canExecute rather than trusted forever - a last-attacked
            // serial can go stale (target died/left range) with no engine notification.
            bar.AddButton(
                new GameActionDefinition(
                    "Attack Last",
                    () => ClassicUO.Game.GameActions.Attack(TargetManager.LastAttack),
                    () => TargetManager.LastAttack != 0
                        && ClassicUO.Game.World.Get(TargetManager.LastAttack) is ClassicUO.Game.GameObjects.Mobile lastAttackMobile
                        && !lastAttackMobile.IsDestroyed));

            // #34: precisely tapping a small, moving enemy on a touchscreen is hard,
            // especially mid-fight - this fires on whatever NearestThreatFinder already
            // identifies as the closest hostile threat, no selection tap required.
            bar.AddButton(
                new GameActionDefinition(
                    "Attack Nearest",
                    () =>
                    {
                        var nearest = NearestThreatFinder.FindNearest();
                        if (nearest != null)
                        {
                            ClassicUO.Game.GameActions.Attack(nearest.Serial);
                        }
                    },
                    () => NearestThreatFinder.FindNearest() != null));

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
