using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using Hollowmere.Diagnostics;
using Hollowmere.HudRuntime;

namespace Hollowmere.Interaction
{
    /// <summary>
    /// #24: the only place that turns a PrimaryAction (pure classification) into
    /// something renderable/executable - labels, tooltip text, and which
    /// GameActions call each verb actually makes. PrimaryActionResolver never
    /// references GameActionDefinition or GameActions; this is where that happens.
    /// </summary>
    internal static class PrimaryActionDefinitionFactory
    {
        // Shown when nothing's selected/nothing actionable - the button stays present
        // and visible but does nothing and reads as unavailable, rather than the Gump
        // being hidden/destroyed (a vanishing dock-anchored button would defeat the
        // point of "always in the same reachable spot").
        private static readonly GameActionDefinition Neutral =
            new GameActionDefinition("-", () => { }, canExecute: () => false);

        public static GameActionDefinition Create(PrimaryAction action)
        {
            switch (action.Type)
            {
                case PrimaryActionType.Attack:
                    return BuildAttack((Mobile) action.Target);
                case PrimaryActionType.Talk:
                    return BuildTalk((Mobile) action.Target);
                case PrimaryActionType.Loot:
                    return BuildLoot((Item) action.Target);
                case PrimaryActionType.PickUp:
                    return BuildPickUp((Item) action.Target);
                case PrimaryActionType.Use:
                    return BuildUse((Item) action.Target);
                default:
                    return Neutral;
            }
        }

        // Item/Mobile client state (OnGround, Container, ...) only updates once the
        // server confirms the action - PrimaryActionResolver would keep re-offering
        // the same verb against the same target for however long that round-trip
        // takes. Clearing the selection the instant a verb fires makes every action
        // one-shot: the button snaps back to neutral immediately and needs a fresh,
        // deliberate re-selection before it can fire again, rather than a second tap
        // landing in that window and re-firing on a target the server hasn't
        // finished processing yet.
        //
        // LastLeftDownObject also has to be cleared here, not just Object/LastObject -
        // confirmed via device logcat (#24) that GameSceneInputHandler.OnMouseDragging's
        // native click-and-drag-to-pick-up gesture keys off LastLeftDownObject (set at
        // the original OnLeftMouseDown, independent of our button), not Object/LastObject.
        // Leaving it set meant any touch-with-slight-movement after a button-driven Pick
        // Up (even a few px of normal finger wobble, well under a deliberate drag) re-fired
        // native PickUp on the same item a second time - the "reconnects to pointer" report.
        private static void ClearSelection()
        {
            SelectedObject.Object = null;
            SelectedObject.LastObject = null;
            SelectedObject.LastLeftDownObject = null;

            // WorldTapTargeting re-pins whatever it captured every frame (mobile
            // taps have no persistent hover to naturally clear on their own) -
            // without this, the pin would restore the just-cleared selection on
            // the very next frame, breaking the one-shot behavior above.
            WorldTapTargeting.ClearPin();
        }

        private static GameActionDefinition BuildAttack(Mobile mobile)
        {
            uint serial = mobile.Serial;
            string targetName = string.IsNullOrEmpty(mobile.Name) ? "target" : mobile.Name;

            return new GameActionDefinition(
                "Attack",
                () =>
                {
                    if (Diag.IsEnabled(DiagCategory.Actions))
                        Diag.Log(DiagCategory.Actions, $"Attack firing, serial={serial}");
                    GameActions.Attack(serial);

                    // Mobile Combat Feedback v1: ClearSelection() below removes the
                    // persistent target ring/info this same frame, so "who got hit"
                    // would otherwise vanish instantly. This is a short, independent
                    // pulse keyed to the mobile reference itself (still valid/alive -
                    // only the *selection* is clearing, not the target), not to
                    // WorldTapTargeting.CurrentSelection, so it survives the clear.
                    WorldEntityScreenAnchor.TriggerAttackFeedback(mobile);

                    ClearSelection();
                },
                tooltip: $"Attack {targetName}");
        }

        private static GameActionDefinition BuildTalk(Mobile mobile)
        {
            uint serial = mobile.Serial;
            string targetName = string.IsNullOrEmpty(mobile.Name) ? "them" : mobile.Name;

            // Same DoubleClick the player would send manually - the server decides
            // whether that opens a vendor gump, a quest dialog, or just a response,
            // this button doesn't need to know which in advance.
            return new GameActionDefinition(
                "Talk",
                () =>
                {
                    if (Diag.IsEnabled(DiagCategory.Actions))
                        Diag.Log(DiagCategory.Actions, $"Talk firing, serial={serial}");
                    GameActions.DoubleClick(serial);
                    ClearSelection();
                },
                tooltip: $"Talk to {targetName}");
        }

        private static GameActionDefinition BuildLoot(Item corpse)
        {
            return new GameActionDefinition(
                "Loot",
                () =>
                {
                    if (Diag.IsEnabled(DiagCategory.Actions))
                        Diag.Log(DiagCategory.Actions, $"Loot firing, corpse.Serial={corpse.Serial}");
                    ActionCatalog.LootCorpse(corpse);
                    ClearSelection();
                },
                canExecute: () => !corpse.IsDestroyed,
                tooltip: "Loot corpse");
        }

        private static GameActionDefinition BuildPickUp(Item item)
        {
            uint serial = item.Serial;
            ushort amount = item.Amount;
            string itemName = string.IsNullOrEmpty(item.Name) ? "item" : item.Name;

            // GameActions.PickUp (called inside GrabItem) silently no-ops - no error,
            // no feedback - if the item is locked or further than
            // Constants.DRAG_ITEMS_DISTANCE (3 tiles) from the player. Without this
            // check the button looks broken (tap does nothing) instead of just
            // correctly refusing to fire on something out of reach - same dimmed-
            // button convention Loot already uses for its own availability check.
            return new GameActionDefinition(
                "Pick Up",
                () =>
                {
                    if (Diag.IsEnabled(DiagCategory.Actions))
                        Diag.Log(DiagCategory.Actions, $"PickUp firing, serial={serial}, distance={item.Distance}");
                    GameActions.GrabItem(serial, amount);
                    ClearSelection();
                },
                canExecute: () => !item.IsDestroyed && !item.IsLocked && item.Distance <= Constants.DRAG_ITEMS_DISTANCE,
                tooltip: $"Pick up {itemName}");
        }

        // Same DoubleClick the Backpack panel's own Use button fires (see
        // BackpackPresenter.UseSelected) - reuses BackpackPresenter.CloseDetail()
        // directly instead of re-implementing that cleanup here, so the backpack's
        // selection highlight/detail panel end up in the same state either way.
        private static GameActionDefinition BuildUse(Item item)
        {
            uint serial = item.Serial;
            string itemName = string.IsNullOrEmpty(item.Name) ? "item" : item.Name;

            return new GameActionDefinition(
                "Use",
                () =>
                {
                    if (Diag.IsEnabled(DiagCategory.Actions))
                        Diag.Log(DiagCategory.Actions, $"Use firing (Primary Action), serial={serial}");
                    GameActions.DoubleClick(serial);
                    BackpackPresenter.Instance?.CloseDetail();
                },
                canExecute: () => !item.IsDestroyed,
                tooltip: $"Use {itemName}");
        }
    }
}
