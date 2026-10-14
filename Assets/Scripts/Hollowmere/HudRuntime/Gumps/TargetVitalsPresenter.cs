using System;
using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps;
using Hollowmere.Hud;
using Hollowmere.Hud.Unity;
using Hollowmere.Interaction;

namespace Hollowmere.HudRuntime.Gumps
{
    /// <summary>
    /// Gump presenter for the current combat target's vitals (health/mana/stamina).
    ///
    /// Target source (#34 audit): SelectedObject.LastObject and TargetManager.LastAttack
    /// are NOT interchangeable. SelectedObject.LastObject is "what am I currently
    /// pointing at" (already what PrimaryActionResolver/PrimaryActionPresenter react to)
    /// - freshest info, but cleared the instant a Primary Action fires (see
    /// PrimaryActionDefinitionFactory.ClearSelection), which is exactly the moment
    /// mid-fight status is most wanted. TargetManager.LastAttack is "who did I last
    /// attack" - persists through that clear.
    ///
    /// Priority: (1) live manual selection via WorldTapTargeting.CurrentSelection -
    /// always respects a deliberate tap; (2) the nearest hostile threat via
    /// NearestThreatFinder - surfaces an attacker's vitals with zero taps, the
    /// PK-ambush scenario this widget exists for; (3) who was last attacked - keeps
    /// the display up if the threat fled out of NearestThreatFinder's radius; (4)
    /// hidden.
    ///
    /// Mobile Combat Targeting v2: tier (1) reads WorldTapTargeting.CurrentSelection
    /// (SelectedObject.LastObject ?? WorldTapTargeting.PinnedEntity), not raw
    /// SelectedObject.LastObject. Confirmed live that raw LastObject reverts to null
    /// the instant the finger lifts (the engine only keeps it populated while a touch
    /// is actively down/held - see WorldTapTargeting's own class doc), so tapping any
    /// Mobile that isn't already hostile-and-in-war-mode (tier 2) or already attacked
    /// (tier 3) made Target Vitals appear for one frame and vanish - confirmed via
    /// TargetVitalsLiveInstances going 1 to 0 across a real elapsed delay with no other
    /// state change. CurrentSelection is the same canonical "what's selected on
    /// mobile" accessor PrimaryActionPresenter and WorldEntityScreenAnchor already use,
    /// and ClearSelection() already nulls both halves of it (SelectedObject.LastObject
    /// AND WorldTapTargeting.ClearPin()), so tier 3 still correctly takes over the
    /// instant a Primary Action fires - this only fixes tier 1 surviving finger-lift,
    /// it does not change what survives a fired action.
    /// </summary>
    public sealed class TargetVitalsPresenter : IHudWidgetPresenter, IHudWidgetPresenterTick
    {
        public const int TargetVitalsWidth = HealthBarGumpCustom.HPB_WIDTH;

        // Mobile HUD Interaction Polish v1: was HealthBarGumpCustom.HPB_HEIGHT_MULTILINE
        // alone (60) - under-reported the gump's real rendered height by the
        // #34 distance-label row TargetVitalsGump adds on top of the base bar
        // (confirmed live: the actual gump's Height is 76, not 60). Harmless in
        // practice today (pivot is top-left and nothing is anchored directly
        // below Target Vitals at any tested resolution - see
        // HudResponsiveLayoutTests), but GetIntrinsicSize() reporting the wrong
        // footprint is still the kind of bug that becomes a real overlap the
        // moment anything is ever placed below this widget.
        public const int TargetVitalsHeight = HealthBarGumpCustom.HPB_HEIGHT_MULTILINE + TargetVitalsGump.DistanceLabelHeight;

        private readonly string widgetId;
        private TargetVitalsGump gump;
        private uint currentTargetSerial;
        private bool layoutVisible = true;
        private HudRect lastBounds;

        public static int LiveInstanceCount { get; private set; }

        // POC seam (WorldEntityScreenAnchor): the already-resolved target entity, for
        // anything that wants to track it without re-deriving the resolution chain
        // above. Same static-accessor shape BackpackPresenter.Instance already uses.
        // Null whenever there is no valid target - callers don't need a second check.
        internal static Entity CurrentTarget { get; private set; }

        public TargetVitalsPresenter(HudWidgetDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            widgetId = definition.Id;
        }

        public string WidgetId => widgetId;

        public HudSize GetIntrinsicSize() => new HudSize(TargetVitalsWidth, TargetVitalsHeight);

        public void Apply(HudResolvedWidgetLayout layout)
        {
            layoutVisible = layout.State == null || layout.State.IsVisible;
            lastBounds = layout.Bounds;

            if (gump == null)
            {
                return;
            }

            gump.IsVisible = layoutVisible && currentTargetSerial != 0;
            gump.X = (int)Math.Round(layout.Bounds.X);
            gump.Y = (int)Math.Round(layout.Bounds.Y);
        }

        public void Tick()
        {
            if (ClassicUO.Client.Game == null || !(ClassicUO.Client.Game.Scene is GameScene))
            {
                TeardownGump();
                return;
            }

            uint desiredSerial = ResolveTargetSerial();

            if (desiredSerial != currentTargetSerial)
            {
                RebuildGump(desiredSerial);
            }

            if (gump != null)
            {
                gump.IsVisible = layoutVisible && currentTargetSerial != 0;

                if (gump.IsVisible)
                {
                    Entity targetEntity = World.Get(currentTargetSerial);
                    gump.SetDistanceTiles(targetEntity != null
                        ? targetEntity.Distance
                        : HudDistanceFormatter.UnknownDistance);

                    // ADR-009 rule 1: reuse the existing actionability classification
                    // for presentation - same resolver PrimaryActionPresenter already
                    // calls for the Primary Action button, not a second answer to
                    // "what is this."
                    PrimaryAction action = PrimaryActionResolver.Resolve(targetEntity);
                    gump.SetActionability(action.Type);

                    CurrentTarget = targetEntity;
                }
                else
                {
                    CurrentTarget = null;
                }
            }
            else
            {
                CurrentTarget = null;
            }
        }

        public void Dispose()
        {
            TeardownGump();
        }

        private static uint ResolveTargetSerial()
        {
            if (WorldTapTargeting.CurrentSelection is Mobile selectedMobile
                && !selectedMobile.IsDestroyed
                && selectedMobile != World.Player)
            {
                return selectedMobile.Serial;
            }

            Mobile nearestThreat = NearestThreatFinder.FindNearest();
            if (nearestThreat != null)
            {
                return nearestThreat.Serial;
            }

            uint lastAttack = TargetManager.LastAttack;
            if (lastAttack != 0
                && World.Get(lastAttack) is Mobile lastAttackMobile
                && !lastAttackMobile.IsDestroyed)
            {
                return lastAttackMobile.Serial;
            }

            return 0;
        }

        private void RebuildGump(uint desiredSerial)
        {
            TeardownGump();
            currentTargetSerial = desiredSerial;

            if (desiredSerial == 0 || !layoutVisible)
            {
                return;
            }

            Entity targetEntity = World.Get(desiredSerial);
            if (targetEntity == null)
            {
                currentTargetSerial = 0;
                return;
            }

            int x = (int)Math.Round(lastBounds.X);
            int y = (int)Math.Round(lastBounds.Y);

            gump = new TargetVitalsGump(targetEntity, x, y);
            UIManager.Add(gump);
            LiveInstanceCount++;
            HudSmokeTestBridge.RegisterTargetVitalsLive(1);
        }

        private void TeardownGump()
        {
            currentTargetSerial = 0;
            CurrentTarget = null;

            if (gump == null)
            {
                return;
            }

            gump.Dispose();
            gump = null;
            LiveInstanceCount = Math.Max(0, LiveInstanceCount - 1);
            HudSmokeTestBridge.RegisterTargetVitalsLive(-1);
        }
    }
}
