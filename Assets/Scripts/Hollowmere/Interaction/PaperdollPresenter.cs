using System.Collections.Generic;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Scenes;
using UnityEngine;
using UnityEngine.UI;
using GameObject = UnityEngine.GameObject;

namespace Hollowmere.Interaction
{
    /// <summary>
    /// Lightweight, immutable snapshot of one equipment slot - a Layer plus whatever
    /// is (or isn't) equipped there. Serial == 0 means the slot is empty. Never holds
    /// a reference to the engine Item itself, same reasoning as BackpackItemRow: a
    /// stale row can't outlive the real object in a way that matters (Use re-resolves
    /// by serial).
    /// </summary>
    internal readonly struct PaperdollSlotRow
    {
        public readonly Layer Layer;
        public readonly uint Serial;
        public readonly string DisplayName;
        public readonly ushort Graphic;

        public PaperdollSlotRow(Layer layer, uint serial, string displayName, ushort graphic)
        {
            Layer = layer;
            Serial = serial;
            DisplayName = displayName;
            Graphic = graphic;
        }

        public bool IsEmpty => Serial == 0;
    }

    /// <summary>
    /// Mobile Paperdoll v1: a compact equipment grid, analogous to Backpack, built
    /// entirely from existing UO equipment state - never a second equipment model.
    ///
    /// Equipment state: Entity.FindItemByLayer(Layer) - the exact API the real desktop
    /// PaperDollGump/PaperDollInteractable already use for every slot they draw (see
    /// external/ClassicUO/src/Game/UI/Controls/PaperDollInteractable.cs). A simple
    /// linear scan of the mobile's own Items linked list, the same structure
    /// BackpackPresenter already walks for backpack contents - not a new data source.
    ///
    /// Slot list: SlotLayers below mirrors PaperDollInteractable's own _layerOrder
    /// (the existing, production-proven "which layers does a dressed character have"
    /// list) plus Layer.Backpack, which the desktop draws as its own icon separately.
    /// Two deliberate omissions from the desktop's list: Hair and Beard. Those are
    /// real FindItemByLayer results, but the desktop composites them into the body
    /// sprite, never renders them as tappable equipment icons, and explicitly disables
    /// lift/drag for them (GumpPicEquipment.CanLift excludes both) - treating them as
    /// equipment slots here would be an invented interaction, not a reused one.
    ///
    /// Icon art: ClassicUO.IO.Resources.ArtLoader.Instance.GetTexture(graphic) -
    /// the exact bridge BackpackItemView.ApplyIcon already uses (see
    /// PaperdollSlotView.ApplyIcon). Uses item.DisplayedGraphic, matching the desktop
    /// Paperdoll's own icon-slot code (PaperDollGump.EquipmentSlot.ItemGumpFixed), not
    /// item.Graphic - DisplayedGraphic is the one that accounts for charge-based/
    /// variant graphics.
    ///
    /// Interaction: tapping a filled slot shows its name/layer and a Use button that
    /// calls GameActions.DoubleClick(serial) - the exact same passthrough
    /// BackpackPresenter.UseSelected already fires, "server decides what double-click
    /// means for this item type" (open/wear/read/etc.), not new action semantics.
    /// Unequip is deliberately NOT implemented: the only existing path is
    /// GameActions.PickUp(serial, 0, 0), which starts the ItemHold drag-and-drop flow
    /// (see PaperDollInteractable.GumpPicEquipment.Update()) - that's the drag system
    /// this slice's scope explicitly excludes, not a clean single-action unequip call.
    ///
    /// Delivery mechanism: unlike BackpackPresenter/BackpackItemView (a
    /// MonoBehaviour with [SerializeField] references into a hand-wired prefab and
    /// scene hierarchy), this panel builds its own Unity UI hierarchy entirely in
    /// code at runtime - the same shape WorldEntityScreenAnchor/ActionBar/
    /// ActionButtonGump already use for every other Hollowmere HUD/interaction
    /// surface built this project. Reused from Backpack: the row-snapshot struct
    /// shape, the ArtLoader icon bridge, BackpackPresenter.ResolveDisplayName, and
    /// the detail-panel-with-Use-button shape. Not reused: the [SerializeField]
    /// wiring itself - that's Backpack's delivery mechanism, not its architecture,
    /// and CLAUDE.md's own Project Standards list "Manual Inspector setup when
    /// automation is practical" under Avoid.
    /// </summary>
    internal class PaperdollPresenter : MonoBehaviour
    {
        public static PaperdollPresenter Instance { get; private set; }

        // Mirrors PaperDollInteractable._layerOrder, minus Hair/Beard (see class doc),
        // plus Backpack (drawn as its own icon on the desktop too).
        private static readonly Layer[] SlotLayers =
        {
            Layer.Helmet, Layer.Earrings, Layer.Necklace, Layer.Ring, Layer.Bracelet,
            Layer.Cloak, Layer.Robe, Layer.Tunic, Layer.Torso, Layer.Shirt,
            Layer.Arms, Layer.Gloves, Layer.Waist, Layer.Skirt, Layer.Pants,
            Layer.Legs, Layer.Shoes, Layer.Face, Layer.OneHanded, Layer.TwoHanded,
            Layer.Talisman, Layer.Backpack
        };

        private const float SlotSize = 56f;
        private const float SlotSpacing = 6f;
        private const int Columns = 4;
        private const float PanelWidth = Columns * SlotSize + (Columns - 1) * SlotSpacing + 24f;

        private static readonly Color PanelBackground = new Color(0.08f, 0.08f, 0.09f, 0.96f);
        private static readonly Color DetailBackground = new Color(0.05f, 0.05f, 0.06f, 0.98f);

        private GameObject panelRoot;
        private Transform gridContent;
        private readonly List<PaperdollSlotView> slotViews = new List<PaperdollSlotView>();

        private GameObject detailRoot;
        private Image detailIconImage;
        private Sprite detailIconSprite;
        private Text detailNameText;
        private Text detailLayerText;
        private Button useButton;
        private Button unequipButton;
        private Button detailCloseButton;
        private Button closeButton;

        private PaperdollSlotRow selectedRow;
        private PaperdollSlotView selectedView;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var go = new GameObject("HollowmerePaperdollPresenter");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<PaperdollPresenter>();
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
            Populate();
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

        // World.Player.FindItemByLayer(layer) per slot - no second equipment store,
        // same "read what the engine already tracks" approach EnumerateBackpackRows
        // already uses for the backpack's own contents.
        private void Populate()
        {
            Mobile player = World.Player;

            for (int i = 0; i < SlotLayers.Length; i++)
            {
                Layer layer = SlotLayers[i];
                Item equipped = player?.FindItemByLayer(layer);

                PaperdollSlotRow row = equipped != null && !equipped.IsDestroyed
                    ? new PaperdollSlotRow(layer, equipped.Serial, BackpackPresenter.ResolveDisplayName(equipped), equipped.DisplayedGraphic)
                    : new PaperdollSlotRow(layer, 0, null, 0);

                slotViews[i].SetRow(row, OnSlotSelected);
            }
        }

        private void OnSlotSelected(PaperdollSlotRow row)
        {
            if (row.IsEmpty)
            {
                return;
            }

            selectedRow = row;

            selectedView?.SetSelected(false);
            selectedView = FindView(row.Layer);
            selectedView?.SetSelected(true);

            if (detailNameText != null)
            {
                detailNameText.text = row.DisplayName;
            }

            if (detailLayerText != null)
            {
                detailLayerText.text = row.Layer.ToString();
            }

            if (detailIconImage != null)
            {
                if (detailIconSprite != null)
                {
                    Destroy(detailIconSprite);
                }

                detailIconSprite = PaperdollSlotView.TryBuildIconSprite(row.Graphic);
                detailIconImage.sprite = detailIconSprite;
                detailIconImage.enabled = detailIconSprite != null;
            }

            // Backpack itself is excluded: the Unequip destination IS the
            // backpack, so unequipping the backpack layer is nonsensical.
            if (unequipButton != null)
            {
                unequipButton.gameObject.SetActive(row.Layer != Layer.Backpack);
            }

            if (detailRoot != null)
            {
                detailRoot.SetActive(true);
            }
        }

        private PaperdollSlotView FindView(Layer layer)
        {
            for (int i = 0; i < SlotLayers.Length; i++)
            {
                if (SlotLayers[i] == layer)
                {
                    return slotViews[i];
                }
            }

            return null;
        }

        // Deliberately just DoubleClick - see class doc. Not equip/unequip semantics,
        // the same server-decides passthrough BackpackPresenter.UseSelected fires.
        private void UseSelected()
        {
            GameActions.DoubleClick(selectedRow.Serial);
            CloseDetail();
        }

        // Mobile Equipment Interaction v1: PickUp() + DropItem(into the backpack) -
        // the exact same two top-level GameActions a real drag-from-paperdoll-to-
        // backpack produces at its start and end, called back-to-back instead of
        // across a drag gesture. Mirrors the existing desktop precedent
        // (PaperDollGump.OnMouseUp's own DropItem(ItemHold.Serial, 0xFFFF, 0xFFFF,
        // 0, container) call) exactly, including passing ItemHold.Serial (what the
        // engine now thinks it's holding) rather than the row snapshot's serial.
        // No client-side Layer/Container writes here - the existing
        // UpdateContainedItem packet handler does that when the server confirms.
        private void UnequipSelected()
        {
            Item backpack = World.Player?.FindItemByLayer(Layer.Backpack);
            if (backpack == null)
            {
                CloseDetail();
                return;
            }

            bool pickedUp = GameActions.PickUp(selectedRow.Serial, 0, 0);

            if (pickedUp)
            {
                GameActions.DropItem(ItemHold.Serial, 0xFFFF, 0xFFFF, 0, backpack.Serial);

                // Recovery, not a new mechanism - see BackpackPresenter.EquipSelected's
                // identical comment. ItemHold.Clear() is an existing method.
                if (ItemHold.Enabled)
                {
                    ItemHold.Clear();
                }
            }

            Close();
        }

        private bool EnsurePanel()
        {
            if (panelRoot != null)
            {
                return true;
            }

            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                return false;
            }

            panelRoot = new GameObject("HollowmerePaperdollPanel", typeof(RectTransform), typeof(Image));
            panelRoot.transform.SetParent(canvas.transform, false);

            var panelRect = (RectTransform)panelRoot.transform;
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;

            Image panelImage = panelRoot.GetComponent<Image>();
            panelImage.color = PanelBackground;

            var titleGo = new GameObject("Title", typeof(RectTransform), typeof(Text));
            titleGo.transform.SetParent(panelRect, false);
            var titleRect = (RectTransform)titleGo.transform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(0f, 28f);
            titleRect.anchoredPosition = new Vector2(0f, -8f);
            Text titleText = titleGo.GetComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 18;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = Color.white;
            titleText.text = "Equipment";
            titleText.raycastTarget = false;

            var gridGo = new GameObject("Grid", typeof(RectTransform), typeof(GridLayoutGroup));
            gridGo.transform.SetParent(panelRect, false);
            var gridRect = (RectTransform)gridGo.transform;
            gridRect.anchorMin = new Vector2(0.5f, 1f);
            gridRect.anchorMax = new Vector2(0.5f, 1f);
            gridRect.pivot = new Vector2(0.5f, 1f);
            gridRect.anchoredPosition = new Vector2(0f, -44f);

            int rows = Mathf.CeilToInt(SlotLayers.Length / (float)Columns);
            float gridWidth = Columns * SlotSize + (Columns - 1) * SlotSpacing;
            float gridHeight = rows * SlotSize + (rows - 1) * SlotSpacing;
            gridRect.sizeDelta = new Vector2(gridWidth, gridHeight);

            GridLayoutGroup gridLayout = gridGo.GetComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(SlotSize, SlotSize);
            gridLayout.spacing = new Vector2(SlotSpacing, SlotSpacing);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = Columns;
            gridContent = gridGo.transform;

            for (int i = 0; i < SlotLayers.Length; i++)
            {
                var slotGo = new GameObject("Slot_" + SlotLayers[i]);
                PaperdollSlotView view = slotGo.AddComponent<PaperdollSlotView>();
                view.Build(gridContent);
                slotViews.Add(view);
            }

            panelRect.sizeDelta = new Vector2(PanelWidth, gridHeight + 44f + 44f);

            var closeGo = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGo.transform.SetParent(panelRect, false);
            var closeRect = (RectTransform)closeGo.transform;
            closeRect.anchorMin = new Vector2(0.5f, 0f);
            closeRect.anchorMax = new Vector2(0.5f, 0f);
            closeRect.pivot = new Vector2(0.5f, 0f);
            closeRect.sizeDelta = new Vector2(PanelWidth - 24f, 32f);
            closeRect.anchoredPosition = new Vector2(0f, 8f);
            closeGo.GetComponent<Image>().color = new Color(0.3f, 0.15f, 0.15f, 1f);
            closeButton = closeGo.GetComponent<Button>();
            closeButton.onClick.AddListener(Close);
            AddLabel(closeRect, "Close");

            BuildDetailPanel(canvas.transform);

            panelRoot.SetActive(false);

            return true;
        }

        private void BuildDetailPanel(Transform canvasTransform)
        {
            detailRoot = new GameObject("HollowmerePaperdollDetail", typeof(RectTransform), typeof(Image));
            detailRoot.transform.SetParent(canvasTransform, false);

            var detailRect = (RectTransform)detailRoot.transform;
            detailRect.anchorMin = new Vector2(0.5f, 0.5f);
            detailRect.anchorMax = new Vector2(0.5f, 0.5f);
            detailRect.pivot = new Vector2(0.5f, 0.5f);
            detailRect.sizeDelta = new Vector2(220f, 224f);
            detailRect.anchoredPosition = Vector2.zero;
            detailRoot.GetComponent<Image>().color = DetailBackground;

            // Mobile Paperdoll/Backpack Read-Only Polish v1: a preview of the same
            // icon the slot itself shows, reusing PaperdollSlotView's sprite-building
            // helper - same ArtLoader bridge, no second art path. Panel grew by 56px
            // (168 -> 224) to fit it above the name/layer text; the bottom button
            // block (Use/Unequip/Close) is bottom-anchored and untouched.
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
            nameRect.sizeDelta = new Vector2(-16f, 28f);
            nameRect.anchoredPosition = new Vector2(0f, -56f);
            detailNameText = nameGo.GetComponent<Text>();
            detailNameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            detailNameText.fontSize = 15;
            detailNameText.alignment = TextAnchor.MiddleCenter;
            detailNameText.color = Color.white;
            detailNameText.horizontalOverflow = HorizontalWrapMode.Wrap;
            detailNameText.raycastTarget = false;

            var layerGo = new GameObject("Layer", typeof(RectTransform), typeof(Text));
            layerGo.transform.SetParent(detailRect, false);
            var layerRect = (RectTransform)layerGo.transform;
            layerRect.anchorMin = new Vector2(0f, 1f);
            layerRect.anchorMax = new Vector2(1f, 1f);
            layerRect.pivot = new Vector2(0.5f, 1f);
            layerRect.sizeDelta = new Vector2(-16f, 18f);
            layerRect.anchoredPosition = new Vector2(0f, -88f);
            detailLayerText = layerGo.GetComponent<Text>();
            detailLayerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            detailLayerText.fontSize = 12;
            detailLayerText.alignment = TextAnchor.MiddleCenter;
            detailLayerText.color = new Color(1f, 1f, 1f, 0.65f);
            detailLayerText.raycastTarget = false;

            var useGo = new GameObject("Use", typeof(RectTransform), typeof(Image), typeof(Button));
            useGo.transform.SetParent(detailRect, false);
            var useRect = (RectTransform)useGo.transform;
            useRect.anchorMin = new Vector2(0f, 0f);
            useRect.anchorMax = new Vector2(1f, 0f);
            useRect.pivot = new Vector2(0.5f, 0f);
            useRect.sizeDelta = new Vector2(-16f, 32f);
            useRect.anchoredPosition = new Vector2(0f, 80f);
            useGo.GetComponent<Image>().color = new Color(0.15f, 0.3f, 0.15f, 1f);
            useButton = useGo.GetComponent<Button>();
            useButton.onClick.AddListener(UseSelected);
            AddLabel(useRect, "Use");

            var unequipGo = new GameObject("Unequip", typeof(RectTransform), typeof(Image), typeof(Button));
            unequipGo.transform.SetParent(detailRect, false);
            var unequipRect = (RectTransform)unequipGo.transform;
            unequipRect.anchorMin = new Vector2(0f, 0f);
            unequipRect.anchorMax = new Vector2(1f, 0f);
            unequipRect.pivot = new Vector2(0.5f, 0f);
            unequipRect.sizeDelta = new Vector2(-16f, 32f);
            unequipRect.anchoredPosition = new Vector2(0f, 44f);
            unequipGo.GetComponent<Image>().color = new Color(0.15f, 0.25f, 0.35f, 1f);
            unequipButton = unequipGo.GetComponent<Button>();
            unequipButton.onClick.AddListener(UnequipSelected);
            AddLabel(unequipRect, "Unequip");

            var detailCloseGo = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button));
            detailCloseGo.transform.SetParent(detailRect, false);
            var detailCloseRect = (RectTransform)detailCloseGo.transform;
            detailCloseRect.anchorMin = new Vector2(0f, 0f);
            detailCloseRect.anchorMax = new Vector2(1f, 0f);
            detailCloseRect.pivot = new Vector2(0.5f, 0f);
            detailCloseRect.sizeDelta = new Vector2(-16f, 32f);
            detailCloseRect.anchoredPosition = new Vector2(0f, 8f);
            detailCloseGo.GetComponent<Image>().color = new Color(0.3f, 0.15f, 0.15f, 1f);
            detailCloseButton = detailCloseGo.GetComponent<Button>();
            detailCloseButton.onClick.AddListener(CloseDetail);
            AddLabel(detailCloseRect, "Close");

            detailRoot.SetActive(false);
        }

        private static void AddLabel(RectTransform parent, string text)
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
            label.fontSize = 13;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.text = text;
            label.raycastTarget = false;
        }
    }
}
