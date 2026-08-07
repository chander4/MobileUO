using ClassicUO.Game.GameObjects;

namespace Hollowmere.Interaction
{
    internal enum PrimaryActionType
    {
        None,
        Attack,
        Talk,
        Loot,
        PickUp
    }

    /// <summary>
    /// #24: pure domain value describing "what's the player's smart primary
    /// interaction with this object" - no presentation, no GameActionDefinition,
    /// no icons/labels/tooltips. Deliberately reusable by anything that wants this
    /// answer without depending on ActionButtonGump: a HUD button today, potentially
    /// action bars, a radial menu, keyboard shortcuts, controller prompts, or
    /// accessibility tooling later. See PrimaryActionDefinitionFactory for the only
    /// place that turns this into something renderable/executable.
    /// </summary>
    internal readonly struct PrimaryAction
    {
        public PrimaryActionType Type { get; }
        public BaseGameObject Target { get; }

        public PrimaryAction(PrimaryActionType type, BaseGameObject target)
        {
            Type = type;
            Target = target;
        }

        public static readonly PrimaryAction None = new PrimaryAction(PrimaryActionType.None, null);
    }
}
