using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;

namespace Hollowmere.Interaction
{
    /// <summary>
    /// #24: pure classification - "what should the player's primary action button
    /// do right now, given what's selected." Knows about Item/Mobile/NotorietyFlag
    /// only, nothing about GameActionDefinition/icons/labels/GameActions calls (see
    /// PrimaryActionDefinitionFactory for that). Stateless and side-effect-free by
    /// design, so it's unit-testable without any Gump/Unity lifecycle involved -
    /// same discipline LongPressStateMachine got in #21A.
    /// </summary>
    internal static class PrimaryActionResolver
    {
        public static PrimaryAction Resolve(BaseGameObject selected)
        {
            if (selected is Item corpse && corpse.IsCorpse && !corpse.IsDestroyed)
            {
                return new PrimaryAction(PrimaryActionType.Loot, corpse);
            }

            if (selected is Item groundItem && groundItem.OnGround && !groundItem.IsDestroyed)
            {
                return new PrimaryAction(PrimaryActionType.PickUp, groundItem);
            }

            if (selected is Mobile mobile && mobile != World.Player && !mobile.IsDestroyed)
            {
                return IsHostile(mobile.NotorietyFlag)
                    ? new PrimaryAction(PrimaryActionType.Attack, mobile)
                    : new PrimaryAction(PrimaryActionType.Talk, mobile);
            }

            return PrimaryAction.None;
        }

        // Label/classification heuristic only - the actual Attack/DoubleClick calls
        // (built in PrimaryActionDefinitionFactory) already handle the real
        // legal/criminal-flagging consequences server-side regardless of what this
        // predicted, so a wrong guess here is a UX rough edge, not a new way to
        // accidentally flag criminal or attack the wrong thing.
        private static bool IsHostile(NotorietyFlag flag)
        {
            return flag == NotorietyFlag.Gray
                || flag == NotorietyFlag.Criminal
                || flag == NotorietyFlag.Enemy
                || flag == NotorietyFlag.Murderer;
        }
    }
}
