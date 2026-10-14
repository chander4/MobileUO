using System;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.Scenes;
using UnityEngine;
using UnityEngine.UI;

namespace Hollowmere.Interaction
{
    /// <summary>
    /// Mobile Spell/Skill UI v1: a compact, read-only browser over the player's
    /// existing spells/skills, analogous to Backpack/Paperdoll - never a second
    /// spell/skill data model.
    ///
    /// Data source: ActionCatalog.GetAll() (specifically GetSpells/GetSkills) -
    /// the same renderer-agnostic aggregator ActionBar's "+" picker already
    /// consumes. GetSpells() walks all 8 existing school dictionaries
    /// (SpellsMagery/Necromancy/Chivalry/Bushido/Ninjitsu/Spellweaving/
    /// Mysticism/Mastery); GetSkills() walks World.Player.Skills filtered to
    /// IsClickable. No new data source, no new packet, no new model.
    ///
    /// Not the same thing as ActionPickerPresenter: that one lists the same
    /// catalog to let a player ADD an entry as a permanent Action Bar button
    /// (tap closes the picker and mutates the bar). This one lets a player
    /// BROWSE what they have and see what it is (tap opens a read-only detail
    /// panel) - the Backpack/Paperdoll "select an item, see its info" shape,
    /// not the bar-customization shape. Reuses ActionPickerPresenter's proven
    /// navigation instead of inventing new navigation: the same 4-tab
    /// All/Spells/Skills/Pets filter, the same capped-list-plus-search
    /// approach (no ScrollRect - MaxVisibleResults keeps a single fixed-height
    /// column cheap to rebuild, and the title bar says so when truncated).
    ///
    /// Icons: Gump art (ActionCatalog.GetSpells() sets
    /// GameActionDefinition.IconGraphic to the spell's small hotbar icon -
    /// the exact same one ActionButtonGump's native GumpPic already renders
    /// for these entries once added to a real Action Bar). GumpsLoader is the
    /// Gump-art equivalent of the ArtLoader bridge BackpackItemView/
    /// PaperdollSlotView already use for Item art - same UOTexture32/
    /// .UnityTexture shape, see SpellSkillItemView.ApplyIcon. Skills/Pet
    /// Commands/Utility entries have no IconGraphic (ActionCatalog never sets
    /// one for them) - their rows/detail simply show no icon, not a
    /// placeholder.
    ///
    /// Detail enrichment: Label/Category/IconGraphic is all CatalogEntry
    /// exposes. For a richer detail view this looks up the underlying
    /// SpellDefinition (ManaCost, MinSkill) or live Skill (Value) by matching
    /// Definition.Label against spell.Name/skill.Name - the same fields
    /// ActionCatalog itself already iterates, just read a second time here
    /// rather than widening CatalogEntry's shape for one consumer. Matches by
    /// name, not by parsing CatalogRefId's string format - that format
    /// belongs to ActionCatalog and could change independently of this class.
    /// If a name doesn't match anything, the panel falls back to name+category
    /// only rather than fabricating zeros.
    ///
    /// Execution: Spells get a "Cast" action, Skills get a "Use" action - both
    /// are the same shared executeButton (mutually exclusive by category, so
    /// one button suffices), both firing the existing GameActionDefinition.
    /// TryExecute() (CastSpell(index)/UseSkill(index) via ActionCatalog - no
    /// new execution path). The targeting-cursor conflict that originally kept
    /// this read-only was solved generically in WorldTapTargeting
    /// (SendToTargetManager, gated on the universal TargetManager.IsTargeting,
    /// not "is this a spell") - Skills reuse that exact bridge with zero
    /// changes to WorldTapTargeting/SpellTargetingIndicator, since neither one
    /// ever cared what triggered targeting. Pets/Utility remain browsing-only.
    ///
    /// Delivery mechanism: self-constructing Unity UI in code (no Inspector
    /// wiring), the same shape PaperdollPresenter/WorldEntityScreenAnchor/
    /// ActionBar already use for every other Hollowmere surface built this
    /// project - see PaperdollPresenter's own class doc for why.
    /// </summary>
    internal class SpellSkillPresenter : MonoBehaviour
    {
        public static SpellSkillPresenter Instance { get; private set; }

        // Mirrors ActionPickerPresenter's own cap/reasoning exactly: a few
        // hundred entries rebuilding a layout on every keystroke is a visible
        // hitch on device, and nobody scrolls hundreds of rows - search narrows
        // before anything renders.
        private const int MaxVisibleResults = 50;
        private const float RowHeight = 48f;
        private const float PanelWidth = 300f;
        private const float PanelHeight = 420f;

        private static readonly Color PanelBackground = new Color(0.08f, 0.08f, 0.09f, 0.96f);
        private static readonly Color DetailBackground = new Color(0.05f, 0.05f, 0.06f, 0.98f);
        private static readonly Color ActiveTabColor = new Color(0.3f, 0.5f, 0.3f, 1f);
        private static readonly Color InactiveTabColor = new Color(0.18f, 0.18f, 0.18f, 1f);

        // Index-matched to categoryTabBackgrounds/categoryTabButtons, mirroring
        // ActionPickerPresenter.TabCategories exactly (null = "All").
        private static readonly ActionCategory?[] TabCategories =
        {
            null, ActionCategory.Spells, ActionCategory.Skills, ActionCategory.PetCommands
        };

        private static readonly string[] TabLabels = { "All", "Spells", "Skills", "Pets" };

        private GameObject panelRoot;
        private Text titleText;
        private InputField searchInput;
        private Transform listContent;
        private readonly Button[] categoryTabButtons = new Button[TabCategories.Length];
        private readonly Image[] categoryTabBackgrounds = new Image[TabCategories.Length];

        private GameObject detailRoot;
        private Image detailIconImage;
        private Sprite detailIconSprite;
        private Text detailNameText;
        private Text detailCategoryText;
        private Text detailInfoText;
        private Button executeButton;
        private Text executeButtonLabel;

        private List<CatalogEntry> allEntries = new List<CatalogEntry>();
        private ActionCategory? selectedCategory;
        private CatalogEntry selectedEntry;
        private readonly List<SpellSkillItemView> spawnedRows = new List<SpellSkillItemView>();
        private SpellSkillItemView selectedView;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var go = new GameObject("HollowmereSpellSkillPresenter");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<SpellSkillPresenter>();
        }

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Open()
        {
            if (ClassicUO.Client.Game == null || !(ClassicUO.Client.Game.Scene is GameScene))
            {
                return;
            }

            if (!EnsurePanel())
            {
                return;
            }

            CloseDetail();
            allEntries = ActionCatalog.GetAll().ToList();
            selectedCategory = null;

            if (searchInput != null)
            {
                searchInput.SetTextWithoutNotify(string.Empty);
            }

            UpdateTabVisuals();
            ApplyFilters();
            panelRoot.SetActive(true);
        }

        public void Close()
        {
            CloseDetail();

            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        private void CloseDetail()
        {
            if (detailRoot != null)
            {
                detailRoot.SetActive(false);
            }

            selectedView?.SetSelected(false);
            selectedView = null;
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
            for (int i = 0; i < categoryTabBackgrounds.Length; i++)
            {
                if (categoryTabBackgrounds[i] != null)
                {
                    categoryTabBackgrounds[i].color = TabCategories[i] == selectedCategory ? ActiveTabColor : InactiveTabColor;
                }
            }
        }

        // Same shape as ActionPickerPresenter.ApplyFilters: category filter, then
        // a case-insensitive substring match on Label, then a hard cap with a
        // title-bar note when truncated rather than an invented "page 2" control.
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

            if (titleText != null)
            {
                titleText.text = filteredList.Count > MaxVisibleResults
                    ? $"Spells & Skills - {filteredList.Count} matches, refine search"
                    : "Spells & Skills";
            }

            Populate(filteredList);
        }

        private void Populate(IReadOnlyList<CatalogEntry> entries)
        {
            foreach (SpellSkillItemView row in spawnedRows)
            {
                if (row != null)
                {
                    Destroy(row.gameObject);
                }
            }

            spawnedRows.Clear();

            if (listContent == null)
            {
                return;
            }

            foreach (CatalogEntry entry in entries.Take(MaxVisibleResults))
            {
                var rowGo = new GameObject("Row_" + entry.Definition.Label);
                SpellSkillItemView view = rowGo.AddComponent<SpellSkillItemView>();
                view.Build(listContent, RowHeight);
                view.Initialize(entry, OnEntrySelected);
                spawnedRows.Add(view);
            }
        }

        private void OnEntrySelected(CatalogEntry entry, SpellSkillItemView view)
        {
            selectedEntry = entry;
            selectedView?.SetSelected(false);
            selectedView = view;
            selectedView?.SetSelected(true);

            // Mobile Skill Execution & Targeting v1: the shared executeButton
            // shows "Cast" for Spells, "Use" for Skills (mutually exclusive by
            // category, so one button covers both), and hides for Pets/Utility,
            // which stay browsing-only as before.
            if (executeButton != null)
            {
                bool canExecuteHere = entry.Category == ActionCategory.Spells || entry.Category == ActionCategory.Skills;
                executeButton.gameObject.SetActive(canExecuteHere);

                if (canExecuteHere && executeButtonLabel != null)
                {
                    executeButtonLabel.text = entry.Category == ActionCategory.Spells ? "Cast" : "Use";
                }
            }

            detailNameText.text = entry.Definition.Label;
            detailCategoryText.text = entry.Category.ToString();
            detailInfoText.text = BuildEnrichmentText(entry);

            if (detailIconSprite != null)
            {
                Destroy(detailIconSprite);
                detailIconSprite = null;
            }

            if (entry.Definition.IconGraphic.HasValue)
            {
                var gumpTexture = ClassicUO.IO.Resources.GumpsLoader.Instance.GetTexture(entry.Definition.IconGraphic.Value);
                var unityTexture = gumpTexture?.UnityTexture as Texture2D;

                if (unityTexture != null)
                {
                    detailIconSprite = Sprite.Create(unityTexture, new Rect(0, 0, unityTexture.width, unityTexture.height), new Vector2(0.5f, 0.5f));
                }
            }

            detailIconImage.sprite = detailIconSprite;
            detailIconImage.enabled = detailIconSprite != null;

            detailRoot.SetActive(true);
        }

        // Re-resolves richer detail from the same existing data ActionCatalog
        // itself reads, matched by name (not CatalogRefId's string format - that
        // belongs to ActionCatalog). Falls back to no extra text rather than
        // fabricating anything if nothing matches.
        private static string BuildEnrichmentText(CatalogEntry entry)
        {
            if (entry.Category == ActionCategory.Spells)
            {
                var schools = new IReadOnlyDictionary<int, SpellDefinition>[]
                {
                    SpellsMagery.GetAllSpells, SpellsNecromancy.GetAllSpells, SpellsChivalry.GetAllSpells,
                    SpellsBushido.GetAllSpells, SpellsNinjitsu.GetAllSpells, SpellsSpellweaving.GetAllSpells,
                    SpellsMysticism.GetAllSpells, SpellsMastery.GetAllSpells
                };

                foreach (var school in schools)
                {
                    foreach (SpellDefinition spell in school.Values)
                    {
                        if (spell.Name != entry.Definition.Label)
                        {
                            continue;
                        }

                        // This project's SpellDefinition tables only populate
                        // Name/ID/GumpIconSmallID - ManaCost/MinSkill read back as 0
                        // for every spell (verified live, not just this one), not a
                        // real "free to cast" value. Showing "Mana: 0" would read as
                        // real data when it's actually unpopulated - omit rather than
                        // fabricate an answer this table doesn't actually have.
                        if (spell.ManaCost == 0 && spell.MinSkill == 0)
                        {
                            return string.Empty;
                        }

                        return $"Mana: {spell.ManaCost}   Min Skill: {spell.MinSkill}";
                    }
                }

                return string.Empty;
            }

            if (entry.Category == ActionCategory.Skills && World.Player != null)
            {
                foreach (Skill skill in World.Player.Skills)
                {
                    if (skill.Name == entry.Definition.Label)
                    {
                        return $"Value: {skill.Value:0.0}";
                    }
                }
            }

            return string.Empty;
        }

        private bool EnsurePanel()
        {
            if (panelRoot != null)
            {
                return true;
            }

            Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                return false;
            }

            panelRoot = new GameObject("HollowmereSpellSkillPanel", typeof(RectTransform), typeof(Image));
            panelRoot.transform.SetParent(canvas.transform, false);

            var panelRect = (RectTransform)panelRoot.transform;
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
            panelRect.anchoredPosition = Vector2.zero;
            panelRoot.GetComponent<Image>().color = PanelBackground;

            var titleGo = new GameObject("Title", typeof(RectTransform), typeof(Text));
            titleGo.transform.SetParent(panelRect, false);
            var titleRect = (RectTransform)titleGo.transform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(0f, 24f);
            titleRect.anchoredPosition = new Vector2(0f, -6f);
            titleText = titleGo.GetComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 15;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = Color.white;
            titleText.text = "Spells & Skills";
            titleText.raycastTarget = false;

            BuildTabs(panelRect);
            BuildSearchField(panelRect);

            var listGo = new GameObject("List", typeof(RectTransform), typeof(VerticalLayoutGroup));
            listGo.transform.SetParent(panelRect, false);
            var listRect = (RectTransform)listGo.transform;
            listRect.anchorMin = new Vector2(0f, 0f);
            listRect.anchorMax = new Vector2(1f, 1f);
            listRect.offsetMin = new Vector2(8f, 8f);
            listRect.offsetMax = new Vector2(-8f, -74f);
            VerticalLayoutGroup listLayout = listGo.GetComponent<VerticalLayoutGroup>();
            listLayout.spacing = 4f;
            listLayout.childForceExpandHeight = false;
            listLayout.childForceExpandWidth = true;
            listLayout.childControlHeight = true;
            listLayout.childControlWidth = true;
            listContent = listGo.transform;

            var closeGo = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGo.transform.SetParent(panelRect, false);
            var closeRect = (RectTransform)closeGo.transform;
            closeRect.anchorMin = new Vector2(0.5f, 0f);
            closeRect.anchorMax = new Vector2(0.5f, 0f);
            closeRect.pivot = new Vector2(0.5f, 0f);
            closeRect.sizeDelta = new Vector2(PanelWidth - 24f, 28f);
            closeRect.anchoredPosition = new Vector2(0f, 6f);
            closeGo.GetComponent<Image>().color = new Color(0.3f, 0.15f, 0.15f, 1f);
            Button closeButton = closeGo.GetComponent<Button>();
            closeButton.onClick.AddListener(Close);
            AddLabel(closeRect, "Close");

            BuildDetailPanel(canvas.transform);

            panelRoot.SetActive(false);

            return true;
        }

        private void BuildTabs(RectTransform panelRect)
        {
            var tabsGo = new GameObject("Tabs", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            tabsGo.transform.SetParent(panelRect, false);
            var tabsRect = (RectTransform)tabsGo.transform;
            tabsRect.anchorMin = new Vector2(0f, 1f);
            tabsRect.anchorMax = new Vector2(1f, 1f);
            tabsRect.pivot = new Vector2(0.5f, 1f);
            tabsRect.sizeDelta = new Vector2(-16f, 28f);
            tabsRect.anchoredPosition = new Vector2(0f, -32f);
            HorizontalLayoutGroup tabsLayout = tabsGo.GetComponent<HorizontalLayoutGroup>();
            tabsLayout.spacing = 4f;
            tabsLayout.childForceExpandWidth = true;
            tabsLayout.childForceExpandHeight = true;
            tabsLayout.childControlWidth = true;
            tabsLayout.childControlHeight = true;

            for (int i = 0; i < TabCategories.Length; i++)
            {
                int tabIndex = i;
                var tabGo = new GameObject("Tab_" + TabLabels[i], typeof(RectTransform), typeof(Image), typeof(Button));
                tabGo.transform.SetParent(tabsRect, false);
                Image tabImage = tabGo.GetComponent<Image>();
                tabImage.color = InactiveTabColor;
                categoryTabBackgrounds[i] = tabImage;

                Button tabButton = tabGo.GetComponent<Button>();
                tabButton.onClick.AddListener(() => SetCategoryFilter(tabIndex));
                categoryTabButtons[i] = tabButton;

                AddLabel((RectTransform)tabGo.transform, TabLabels[i], 11);
            }

            UpdateTabVisuals();
        }

        // Built for completeness (a real text field matching ActionPickerPresenter's
        // own searchInput), but keyboard entry can't be exercised in this session -
        // no touchscreen, no reliable way to drive Unity's EventSystem text input via
        // MCP. Filter logic itself (ApplyFilters) is validated directly instead - see
        // this slice's report.
        private void BuildSearchField(RectTransform panelRect)
        {
            var fieldGo = new GameObject("Search", typeof(RectTransform), typeof(Image), typeof(InputField));
            fieldGo.transform.SetParent(panelRect, false);
            var fieldRect = (RectTransform)fieldGo.transform;
            fieldRect.anchorMin = new Vector2(0f, 1f);
            fieldRect.anchorMax = new Vector2(1f, 1f);
            fieldRect.pivot = new Vector2(0.5f, 1f);
            fieldRect.sizeDelta = new Vector2(-16f, 26f);
            fieldRect.anchoredPosition = new Vector2(0f, -64f);
            fieldGo.GetComponent<Image>().color = new Color(0.16f, 0.16f, 0.16f, 1f);

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(fieldRect, false);
            var textRect = (RectTransform)textGo.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8f, 2f);
            textRect.offsetMax = new Vector2(-8f, -2f);
            Text inputText = textGo.GetComponent<Text>();
            inputText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            inputText.fontSize = 13;
            inputText.color = Color.white;
            inputText.alignment = TextAnchor.MiddleLeft;
            inputText.supportRichText = false;

            var placeholderGo = new GameObject("Placeholder", typeof(RectTransform), typeof(Text));
            placeholderGo.transform.SetParent(fieldRect, false);
            var placeholderRect = (RectTransform)placeholderGo.transform;
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = new Vector2(8f, 2f);
            placeholderRect.offsetMax = new Vector2(-8f, -2f);
            Text placeholderText = placeholderGo.GetComponent<Text>();
            placeholderText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            placeholderText.fontSize = 13;
            placeholderText.color = new Color(1f, 1f, 1f, 0.4f);
            placeholderText.alignment = TextAnchor.MiddleLeft;
            placeholderText.text = "Search...";

            searchInput = fieldGo.GetComponent<InputField>();
            searchInput.textComponent = inputText;
            searchInput.placeholder = placeholderText;
            searchInput.onValueChanged.AddListener(_ => ApplyFilters());
        }

        private void BuildDetailPanel(Transform canvasTransform)
        {
            detailRoot = new GameObject("HollowmereSpellSkillDetail", typeof(RectTransform), typeof(Image));
            detailRoot.transform.SetParent(canvasTransform, false);

            var detailRect = (RectTransform)detailRoot.transform;
            detailRect.anchorMin = new Vector2(0.5f, 0.5f);
            detailRect.anchorMax = new Vector2(0.5f, 0.5f);
            detailRect.pivot = new Vector2(0.5f, 0.5f);
            detailRect.sizeDelta = new Vector2(220f, 240f);
            detailRect.anchoredPosition = Vector2.zero;
            detailRoot.GetComponent<Image>().color = DetailBackground;

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(detailRect, false);
            var detailIconRect = (RectTransform)iconGo.transform;
            detailIconRect.anchorMin = new Vector2(0.5f, 1f);
            detailIconRect.anchorMax = new Vector2(0.5f, 1f);
            detailIconRect.pivot = new Vector2(0.5f, 1f);
            detailIconRect.sizeDelta = new Vector2(40f, 40f);
            detailIconRect.anchoredPosition = new Vector2(0f, -10f);
            detailIconImage = iconGo.GetComponent<Image>();
            detailIconImage.preserveAspect = true;
            detailIconImage.raycastTarget = false;
            detailIconImage.enabled = false;

            var nameGo = new GameObject("Name", typeof(RectTransform), typeof(Text));
            nameGo.transform.SetParent(detailRect, false);
            var nameRect = (RectTransform)nameGo.transform;
            nameRect.anchorMin = new Vector2(0f, 1f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.pivot = new Vector2(0.5f, 1f);
            nameRect.sizeDelta = new Vector2(-16f, 26f);
            nameRect.anchoredPosition = new Vector2(0f, -56f);
            detailNameText = nameGo.GetComponent<Text>();
            detailNameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            detailNameText.fontSize = 15;
            detailNameText.alignment = TextAnchor.MiddleCenter;
            detailNameText.color = Color.white;
            detailNameText.horizontalOverflow = HorizontalWrapMode.Wrap;
            detailNameText.raycastTarget = false;

            var categoryGo = new GameObject("Category", typeof(RectTransform), typeof(Text));
            categoryGo.transform.SetParent(detailRect, false);
            var categoryRect = (RectTransform)categoryGo.transform;
            categoryRect.anchorMin = new Vector2(0f, 1f);
            categoryRect.anchorMax = new Vector2(1f, 1f);
            categoryRect.pivot = new Vector2(0.5f, 1f);
            categoryRect.sizeDelta = new Vector2(-16f, 16f);
            categoryRect.anchoredPosition = new Vector2(0f, -84f);
            detailCategoryText = categoryGo.GetComponent<Text>();
            detailCategoryText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            detailCategoryText.fontSize = 11;
            detailCategoryText.alignment = TextAnchor.MiddleCenter;
            detailCategoryText.color = new Color(1f, 1f, 1f, 0.65f);
            detailCategoryText.raycastTarget = false;

            var infoGo = new GameObject("Info", typeof(RectTransform), typeof(Text));
            infoGo.transform.SetParent(detailRect, false);
            var infoRect = (RectTransform)infoGo.transform;
            infoRect.anchorMin = new Vector2(0f, 1f);
            infoRect.anchorMax = new Vector2(1f, 1f);
            infoRect.pivot = new Vector2(0.5f, 1f);
            infoRect.sizeDelta = new Vector2(-16f, 20f);
            infoRect.anchoredPosition = new Vector2(0f, -104f);
            detailInfoText = infoGo.GetComponent<Text>();
            detailInfoText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            detailInfoText.fontSize = 12;
            detailInfoText.alignment = TextAnchor.MiddleCenter;
            detailInfoText.color = new Color(0.85f, 0.85f, 1f, 0.9f);
            detailInfoText.raycastTarget = false;

            // Mobile Skill Execution & Targeting v1: existing GameActionDefinition.
            // TryExecute() - the same call PrimaryAction/Backpack Use/Paperdoll Use
            // already fire - not a new execution path. Resolves to
            // GameActions.CastSpell(id) for Spells or GameActions.UseSkill(index)
            // for Skills via ActionCatalog. Hidden for every other category, label
            // swapped per category (see OnEntrySelected).
            var executeGo = new GameObject("Execute", typeof(RectTransform), typeof(Image), typeof(Button));
            executeGo.transform.SetParent(detailRect, false);
            var executeRect = (RectTransform)executeGo.transform;
            executeRect.anchorMin = new Vector2(0f, 0f);
            executeRect.anchorMax = new Vector2(1f, 0f);
            executeRect.pivot = new Vector2(0.5f, 0f);
            executeRect.sizeDelta = new Vector2(-16f, 32f);
            executeRect.anchoredPosition = new Vector2(0f, 44f);
            executeGo.GetComponent<Image>().color = new Color(0.15f, 0.3f, 0.15f, 1f);
            executeButton = executeGo.GetComponent<Button>();
            executeButton.onClick.AddListener(ExecuteSelected);
            executeButtonLabel = AddLabel(executeRect, "Cast");

            var detailCloseGo = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button));
            detailCloseGo.transform.SetParent(detailRect, false);
            var detailCloseRect = (RectTransform)detailCloseGo.transform;
            detailCloseRect.anchorMin = new Vector2(0f, 0f);
            detailCloseRect.anchorMax = new Vector2(1f, 0f);
            detailCloseRect.pivot = new Vector2(0.5f, 0f);
            detailCloseRect.sizeDelta = new Vector2(-16f, 32f);
            detailCloseRect.anchoredPosition = new Vector2(0f, 8f);
            detailCloseGo.GetComponent<Image>().color = new Color(0.3f, 0.15f, 0.15f, 1f);
            Button detailCloseButton = detailCloseGo.GetComponent<Button>();
            detailCloseButton.onClick.AddListener(CloseDetail);
            AddLabel(detailCloseRect, "Close");

            detailRoot.SetActive(false);
        }

        // Fires the existing GameActionDefinition's Action (CastSpell for a Spells
        // entry, UseSkill for a Skills entry) and closes the Book so the player
        // can immediately tap their target in the world, if the server asks for
        // one - the resulting TargetCursor state is what
        // WorldTapTargeting.SendToTargetManager already consumes on the next tap,
        // unchanged from the spell-only version of this button.
        private void ExecuteSelected()
        {
            selectedEntry.Definition.TryExecute();
            Close();
        }

        private static Text AddLabel(RectTransform parent, string text, int fontSize = 13)
        {
            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(parent, false);
            var labelRect = (RectTransform)labelGo.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            Text label = labelGo.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.text = text;
            label.raycastTarget = false;

            return label;
        }
    }
}
