using System.Collections.Generic;
using System.Linq;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;

namespace Hollowmere.Interaction
{
    public enum ActionCategory
    {
        Spells,
        Skills,
        PetCommands,
        Macros,

        // Genuine gameplay actions that don't fit the above (Loot Corpse) - not the
        // same thing as the dev/settings-only "Reset Action Bars" entry that was
        // deliberately removed from the picker and moved to Options instead. This
        // category is for things a player actually does mid-combat/mid-loop.
        Utility
    }

    public struct CatalogEntry
    {
        public ActionCategory Category;
        public GameActionDefinition Definition;
    }

    /// <summary>
    /// Read-only aggregator over action sources that already exist in the engine
    /// so the picker UI can list "anything a player can do" without owning any of
    /// that data itself. No UI here - see Issue #22.
    ///
    /// Deliberately native ClassicUO calls only (GameActions.CastSpell/UseSkill/Say)
    /// - an earlier version of Skills/PetCommands went through the Assistant plugin's
    /// HotKeys catalog instead, which meant the picker's Skills/Pets tabs silently
    /// stayed empty unless the player had turned on "Enable Assistant" (a large,
    /// unrelated legacy subsystem: auto-loot, scavenger, vendor-buy scripting).
    /// Using a skill or telling your pet to follow needs none of that - it's the
    /// same packet ClassicUO's own SkillGumpAdvanced/QuickChatBarBootstrap already
    /// send natively - so this catalog no longer depends on Assistant at all.
    /// </summary>
    internal static class ActionCatalog
    {
        // Same six commands Assistant's HotKeys used to send via
        // Engine.Instance.SendToServer - these are just UOSObjects.Player.Say(...)
        // under the hood there too, so sending them as plain speech natively
        // (matching QuickChatBarBootstrap's phrase buttons) is a straight
        // equivalent, not a behavior change.
        private static readonly (string Label, string Phrase)[] PetCommands =
        {
            ("All Come", "All Come"),
            ("All Follow", "All Follow"),
            ("All Guard", "All Guard"),
            ("All Kill", "All Kill"),
            ("All Stay", "All Stay"),
            ("All Stop", "All Stop"),
        };
        public static IEnumerable<CatalogEntry> GetSpells()
        {
            var schools = new IReadOnlyDictionary<int, SpellDefinition>[]
            {
                SpellsMagery.GetAllSpells,
                SpellsNecromancy.GetAllSpells,
                SpellsChivalry.GetAllSpells,
                SpellsBushido.GetAllSpells,
                SpellsNinjitsu.GetAllSpells,
                SpellsSpellweaving.GetAllSpells,
                SpellsMysticism.GetAllSpells,
                SpellsMastery.GetAllSpells,
            };

            foreach (var school in schools)
            {
                foreach (SpellDefinition spell in school.Values)
                {
                    if (string.IsNullOrEmpty(spell.Name))
                    {
                        continue;
                    }

                    int id = spell.ID;

                    // GumpIconID is the large spellbook-page icon; GumpIconSmallID is the
                    // one the engine's own hotbar-style button (UseSpellButtonGump) uses -
                    // that's the one that actually fits a 44px button.
                    ushort iconGraphic = (ushort) spell.GumpIconSmallID;

                    yield return new CatalogEntry
                    {
                        Category = ActionCategory.Spells,
                        Definition = new GameActionDefinition(spell.Name, () => GameActions.CastSpell(id),
                            iconGraphic: iconGraphic, catalogRefId: $"spell:{id}")
                    };
                }
            }
        }

        // Native GameActions.UseSkill(index) - the same call SkillGumpAdvanced's own
        // Active Use button makes. Only skills with IsClickable have a meaningful "use"
        // action (matches the same filter that button applies) - a passive skill like
        // Resisting Spells has nothing to execute. Requires World.Player, so this
        // naturally yields nothing before login rather than needing a separate guard.
        public static IEnumerable<CatalogEntry> GetSkills()
        {
            if (World.Player == null)
            {
                yield break;
            }

            foreach (Skill skill in World.Player.Skills)
            {
                if (!skill.IsClickable)
                {
                    continue;
                }

                int index = skill.Index;
                string name = skill.Name;

                yield return new CatalogEntry
                {
                    Category = ActionCategory.Skills,
                    Definition = new GameActionDefinition(name, () => GameActions.UseSkill(index), catalogRefId: $"skill:{name}")
                };
            }
        }

        public static IEnumerable<CatalogEntry> GetPetCommands()
        {
            foreach (var (label, phrase) in PetCommands)
            {
                yield return new CatalogEntry
                {
                    Category = ActionCategory.PetCommands,
                    Definition = new GameActionDefinition(label, () => GameActions.Say(phrase), catalogRefId: $"pet:{phrase}")
                };
            }
        }

        // Loots via GameActions.GrabItem (native, same call the "grab bag" preference
        // already drives) against whatever corpse the player last selected -
        // SelectedObject.LastObject is already set to the corpse by the engine's own
        // GridLootGump/ContainerGump when its loot window opens (GridLootGump.cs:308),
        // so this needs no new tracking or hooks into those Gumps. Matches the same
        // "act on whatever you last touched" pattern Target Last already uses.
        public static IEnumerable<CatalogEntry> GetUtilityActions()
        {
            yield return new CatalogEntry
            {
                Category = ActionCategory.Utility,
                Definition = new GameActionDefinition(
                    "Loot Corpse",
                    LootLastCorpse,
                    canExecute: () => TryGetLootableCorpse(out _),
                    tooltip: "Grabs everything from the corpse you last opened/selected",
                    catalogRefId: "utility:lootcorpse")
            };
        }

        private static bool TryGetLootableCorpse(out Item corpse)
        {
            corpse = SelectedObject.LastObject as Item;
            return corpse != null && corpse.IsCorpse && !corpse.IsDestroyed;
        }

        private static void LootLastCorpse()
        {
            if (TryGetLootableCorpse(out Item corpse))
            {
                LootCorpse(corpse);
            }
        }

        // Shared with #24's PrimaryActionDefinitionFactory, which already has a
        // specific corpse Item (from PrimaryAction.Target) rather than needing to
        // look one up via SelectedObject - same GrabItem loop either way.
        public static void LootCorpse(Item corpse)
        {
            for (LinkedObject i = corpse.Items; i != null; i = i.Next)
            {
                Item item = (Item) i;
                GameActions.GrabItem(item.Serial, item.Amount);
            }
        }

        public static IEnumerable<CatalogEntry> GetAll()
        {
            return GetSpells().Concat(GetSkills()).Concat(GetPetCommands()).Concat(GetUtilityActions());
        }

        // #22 D4: re-resolves a persisted CatalogRefId back to a live CatalogEntry on
        // next login. A linear scan over ~400 entries at login time is cheap enough not
        // to warrant a dictionary - this only runs once per bar per login, not per frame.
        public static bool TryResolve(string catalogRefId, out CatalogEntry entry)
        {
            foreach (CatalogEntry candidate in GetAll())
            {
                if (candidate.Definition.CatalogRefId == catalogRefId)
                {
                    entry = candidate;
                    return true;
                }
            }

            entry = default;
            return false;
        }
    }
}
