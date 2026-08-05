using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Hollowmere.Interaction
{
    /// <summary>
    /// #22 Deliverable 2: the Add Action picker. Lists ActionCatalog entries
    /// (category + search filter) and adds the tapped one to whichever
    /// ActionBar it was opened for. Owns no persistence or bar-management UI
    /// of its own - Deliverable 5 wires a trigger (e.g. a "+" button on the
    /// bar) to call Open(); this class only needs a target ActionBar handed
    /// to it.
    ///
    /// Requires Inspector wiring once a prefab/scene hierarchy exists:
    /// - panelRoot: the picker's *visual* root GameObject (a HollowmerePanel),
    ///   starts inactive. This presenter's own GameObject must NOT be
    ///   panelRoot or a descendant of it - Unity never calls Awake on a
    ///   component whose GameObject is inactive at scene load, so an
    ///   Instance-registering presenter has to live on an always-active
    ///   sibling/parent and merely toggle panelRoot's active state.
    /// - searchInput: a legacy UI InputField (this project doesn't use TMP -
    ///   see HollowmereTypography.ApplyTo(Text)).
    /// - listParent: the Content transform of a ScrollRect the item rows get
    ///   instantiated into.
    /// - itemViewPrefab: an ActionPickerItemView prefab instance.
    /// - closeButton: optional, a Button that calls Close().
    /// - categoryTabButtons/categoryTabLabels: 4 parallel-indexed Buttons/Texts for
    ///   All/Spells/Skills/Pets (see CategoryTabs) - a real UO build almost always
    ///   spans multiple of these at once (a Bard Tamer needs Taming, Magery, and
    ///   Music/Peace/Discord skills together - see uooutlands.com/wiki Templates),
    ///   so this is a *filter* on top of the always-available search, not a
    ///   mutually-exclusive replacement for browsing everything.
    /// </summary>
    internal class ActionPickerPresenter : MonoBehaviour
    {
        // #22 D5: the "+" trigger lives on the Gump/ClassicUO side (ActionButtonGump),
        // not Unity UI, so it has no scene reference to this presenter to call Open()
        // on - it goes through this singleton instead. See panelRoot's doc comment
        // above for why this component's own GameObject must stay active.
        public static ActionPickerPresenter Instance { get; private set; }

        // Large catalog (400+ entries) + VerticalLayoutGroup/ContentSizeFitter rebuild
        // on every keystroke would be a visible multi-hundred-ms hitch on device -
        // nobody scrolls hundreds of rows anyway, so search narrows before anything renders.
        private const int MaxVisibleResults = 50;

        private static readonly Color ActiveTabColor = new Color(0.3f, 0.5f, 0.3f, 1f);
        private static readonly Color InactiveTabColor = new Color(0.18f, 0.18f, 0.18f, 1f);

        // Parallel to categoryTabButtons/categoryTabLabels: index 0 = "All" (null
        // filter), 1..3 = Spells/Skills/PetCommands. Index-matched rather than a
        // dictionary since there are exactly 4, fixed at design time.
        private static readonly ActionCategory?[] TabCategories =
        {
            null, ActionCategory.Spells, ActionCategory.Skills, ActionCategory.PetCommands
        };

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private InputField searchInput;
        [SerializeField] private Transform listParent;
        [SerializeField] private ActionPickerItemView itemViewPrefab;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button[] categoryTabButtons;
        [SerializeField] private Image[] categoryTabBackgrounds;
        [SerializeField] private Text titleLabel;

        private ActionBar targetBar;
        private List<CatalogEntry> allEntries = new List<CatalogEntry>();
        private readonly List<ActionPickerItemView> spawnedItems = new List<ActionPickerItemView>();
        private ActionCategory? selectedCategory;

        // Recomputed once per Open() (not live) - sufficient because OnEntrySelected
        // always Closes the picker right after adding, so the bar's contents can't
        // change again while this same list is on screen.
        private readonly HashSet<string> alreadyOnBarRefIds = new HashSet<string>();

        private void Awake()
        {
            Instance = this;

            if (searchInput != null)
            {
                searchInput.onValueChanged.AddListener(_ => ApplyFilters());
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
            }

            if (categoryTabButtons != null)
            {
                for (int i = 0; i < categoryTabButtons.Length; i++)
                {
                    int tabIndex = i;

                    if (categoryTabButtons[i] != null)
                    {
                        categoryTabButtons[i].onClick.AddListener(() => SetCategoryFilter(tabIndex));
                    }
                }
            }

            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        public void Open(ActionBar bar)
        {
            // See ChatPopupPresenter.Open for why - the two popups overlap on screen,
            // so only one can be open at a time.
            ChatPopupPresenter.Instance?.Close();

            targetBar = bar;
            allEntries = ActionCatalog.GetAll().ToList();
            selectedCategory = null;

            alreadyOnBarRefIds.Clear();

            if (bar != null)
            {
                foreach (string refId in bar.GetCatalogRefIds())
                {
                    alreadyOnBarRefIds.Add(refId);
                }
            }

            if (searchInput != null)
            {
                searchInput.SetTextWithoutNotify(string.Empty);
            }

            UpdateTabVisuals();
            ApplyFilters();

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }
        }

        public void Close()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }

            targetBar = null;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void SetCategoryFilter(int tabIndex)
        {
            if (tabIndex < 0 || tabIndex >= TabCategories.Length)
            {
                return;
            }

            selectedCategory = TabCategories[tabIndex];
            UpdateTabVisuals();
            ApplyFilters();
        }

        private void UpdateTabVisuals()
        {
            if (categoryTabBackgrounds == null)
            {
                return;
            }

            for (int i = 0; i < categoryTabBackgrounds.Length; i++)
            {
                if (categoryTabBackgrounds[i] != null)
                {
                    categoryTabBackgrounds[i].color = TabCategories[i] == selectedCategory ? ActiveTabColor : InactiveTabColor;
                }
            }
        }

        private void ApplyFilters()
        {
            IEnumerable<CatalogEntry> filtered = allEntries;

            if (selectedCategory.HasValue)
            {
                filtered = filtered.Where(e => e.Category == selectedCategory.Value);
            }

            string query = searchInput != null ? searchInput.text : string.Empty;

            if (!string.IsNullOrWhiteSpace(query))
            {
                filtered = filtered.Where(e => e.Definition.Label.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            var filteredList = filtered.ToList();
            UpdateTitleLabel(filteredList.Count);
            Populate(filteredList);
        }

        // Reuses the existing title bar label instead of adding a dedicated UI element
        // for what's a rare state (only matters once a filtered list exceeds the cap) -
        // without this, a truncated list silently looks like "that's everything",
        // which could read as a missing spell/skill rather than an unrefined search.
        private void UpdateTitleLabel(int matchCount)
        {
            if (titleLabel == null)
            {
                return;
            }

            titleLabel.text = matchCount > MaxVisibleResults
                ? $"Add Action - {matchCount} matches, refine search"
                : "Add Action";
        }

        private void Populate(IReadOnlyList<CatalogEntry> entries)
        {
            foreach (ActionPickerItemView item in spawnedItems)
            {
                if (item != null)
                {
                    Destroy(item.gameObject);
                }
            }

            spawnedItems.Clear();

            if (itemViewPrefab == null || listParent == null)
            {
                return;
            }

            foreach (CatalogEntry entry in entries.Take(MaxVisibleResults))
            {
                ActionPickerItemView item = Instantiate(itemViewPrefab, listParent);
                bool alreadyOnBar = entry.Definition.CatalogRefId != null && alreadyOnBarRefIds.Contains(entry.Definition.CatalogRefId);
                item.Initialize(entry, OnEntrySelected, alreadyOnBar);
                spawnedItems.Add(item);
            }
        }

        private void OnEntrySelected(CatalogEntry entry)
        {
            targetBar?.AddButton(entry.Definition, removable: true);
            Close();
        }
    }
}
