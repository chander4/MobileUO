using System.Collections.Generic;
using ClassicUO.Game;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.IO.Resources;
using Hollowmere.Diagnostics;
using UnityEngine;
using UnityEngine.UI;

namespace Hollowmere.Interaction
{
    /// <summary>
    /// Lightweight, immutable snapshot of one backpack item - enough for display and
    /// to re-issue GameActions.DoubleClick against the real Item later via Serial.
    /// Never holds a reference to the engine Item itself, so a stale row can't outlive
    /// the real object in a way that matters (the "Use" button re-resolves by serial).
    /// </summary>
    public readonly struct BackpackItemRow
    {
        public readonly uint Serial;
        public readonly string DisplayName;
        public readonly ushort Amount;
        public readonly ushort Graphic;

        public BackpackItemRow(uint serial, string displayName, ushort amount, ushort graphic)
        {
            Serial = serial;
            DisplayName = displayName;
            Amount = amount;
            Graphic = graphic;
        }
    }

    /// <summary>
    /// #32: mobile backpack panel. Follows the same shape as ActionPickerPresenter
    /// (#22) - a Unity UI modal, not a HUD widget (see #32 audit: a temporary,
    /// explicitly-opened panel doesn't fit HudLayoutService's persistently-positioned
    /// widget model). Observes World.Player's existing backpack contents; never
    /// stores a second copy of inventory state beyond the display-only row snapshot.
    ///
    /// Requires Inspector wiring once a prefab/scene hierarchy exists, same as
    /// ActionPickerPresenter:
    /// - panelRoot: the picker's visual root GameObject, starts inactive. This
    ///   presenter's own GameObject must NOT be panelRoot or a descendant of it -
    ///   Unity never calls Awake on a component whose GameObject is inactive at
    ///   scene load.
    /// - listParent: the Content transform of a ScrollRect the item rows get
    ///   instantiated into.
    /// - itemViewPrefab: a BackpackItemView prefab instance.
    /// - closeButton: closes the panel.
    /// - detailRoot/detailNameText/detailInfoText/useButton/detailCloseButton: the
    ///   single-item detail state.
    /// - emptyStateText: shown when the backpack has no items.
    /// </summary>
    internal class BackpackPresenter : MonoBehaviour
    {
        public static BackpackPresenter Instance { get; private set; }

        // Smallest read-only seam PrimaryActionResolver needs to know "a backpack
        // item is selected" - same internal-static-property shape
        // TargetVitalsPresenter.CurrentTarget already uses for the same purpose.
        // Deliberately just the serial, not the row/view, so callers can't reach
        // into selection internals beyond what they need.
        internal static uint? SelectedSerial { get; private set; }

        [SerializeField] private UnityEngine.GameObject panelRoot;
        [SerializeField] private Transform listParent;
        [SerializeField] private BackpackItemView itemViewPrefab;
        [SerializeField] private Button closeButton;
        [SerializeField] private UnityEngine.GameObject emptyStateText;

        [SerializeField] private UnityEngine.GameObject detailRoot;
        [SerializeField] private Text detailNameText;
        [SerializeField] private Text detailInfoText;
        [SerializeField] private Button useButton;
        [SerializeField] private Button detailCloseButton;

        // Mobile Equipment Interaction v1: not [SerializeField] - the existing
        // Backpack prefab/scene hierarchy was never wired with this button, and
        // editing that hand-wired hierarchy risks the already-working detail panel.
        // Built once in Awake() as a child of the existing (wired) detailRoot,
        // positioned by mirroring useButton's own RectTransform - same self-
        // constructing approach PaperdollPresenter already uses, applied here only
        // for this one additive element rather than the whole panel.
        private Button equipButton;

        private readonly List<BackpackItemView> spawnedItems = new List<BackpackItemView>();
        private BackpackItemRow selectedRow;
        private BackpackItemView selectedView;

        private void Awake()
        {
            Instance = this;

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
            }

            if (detailCloseButton != null)
            {
                detailCloseButton.onClick.AddListener(CloseDetail);
            }

            if (useButton != null)
            {
                useButton.onClick.AddListener(UseSelected);
                BuildEquipButton();
            }

            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }

            if (detailRoot != null)
            {
                detailRoot.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // Mirrors useButton's own RectTransform (anchors/pivot/size), stacked
        // directly above it - reads the existing wired button's layout rather than
        // guessing detail panel dimensions.
        private void BuildEquipButton()
        {
            RectTransform useRect = useButton.GetComponent<RectTransform>();
            var equipGo = new UnityEngine.GameObject("EquipButton", typeof(RectTransform), typeof(Image), typeof(Button));
            equipGo.transform.SetParent(useButton.transform.parent, false);

            var equipRect = (RectTransform)equipGo.transform;
            equipRect.anchorMin = useRect.anchorMin;
            equipRect.anchorMax = useRect.anchorMax;
            equipRect.pivot = useRect.pivot;
            equipRect.sizeDelta = useRect.sizeDelta;
            equipRect.anchoredPosition = useRect.anchoredPosition + new Vector2(0f, useRect.sizeDelta.y + 8f);

            equipGo.GetComponent<Image>().color = new Color(0.15f, 0.25f, 0.35f, 1f);
            equipButton = equipGo.GetComponent<Button>();
            equipButton.onClick.AddListener(EquipSelected);

            var labelGo = new UnityEngine.GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(equipRect, false);
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
            label.text = "Equip";
            label.raycastTarget = false;

            equipGo.SetActive(false);
        }

        public void Open()
        {
            CloseDetail();
            Populate(EnumerateBackpackRows());

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }
        }

        public void Close()
        {
            CloseDetail();

            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        // internal, not private: PrimaryActionDefinitionFactory.BuildUse calls this
        // after firing DoubleClick via Primary Action, so a Use fired from either
        // entry point leaves the backpack panel in the same state.
        internal void CloseDetail()
        {
            if (detailRoot != null)
            {
                detailRoot.SetActive(false);
            }

            selectedView?.SetSelected(false);
            selectedView = null;
            SelectedSerial = null;
        }

        // World.Player's backpack is a plain Entity.Items linked list, same access
        // pattern as PrimaryActionResolver/PrimaryActionDefinitionFactory already use
        // elsewhere for World.Player/selected-item state - no packet parsing, no
        // second inventory store, this only reads what the engine already tracks.
        private static IReadOnlyList<BackpackItemRow> EnumerateBackpackRows()
        {
            var rows = new List<BackpackItemRow>();

            Item backpack = World.Player?.FindItemByLayer(Layer.Backpack);
            if (backpack == null)
            {
                return rows;
            }

            for (var entry = backpack.Items; entry != null; entry = entry.Next)
            {
                if (!(entry is Item item) || item.IsDestroyed)
                {
                    continue;
                }

                rows.Add(new BackpackItemRow(item.Serial, ResolveDisplayName(item), item.Amount, item.Graphic));
            }

            return rows;
        }

        // internal, not private: Mobile Target Info v1 reuses this exact fallback
        // (Name -> ItemData.Name -> "item") for the target-info name label instead
        // of duplicating it.
        internal static string ResolveDisplayName(Item item)
        {
            if (!string.IsNullOrEmpty(item.Name))
            {
                return item.Name;
            }

            string typeName = item.ItemData.Name;
            return !string.IsNullOrEmpty(typeName) ? typeName : "item";
        }

        private void Populate(IReadOnlyList<BackpackItemRow> rows)
        {
            foreach (BackpackItemView item in spawnedItems)
            {
                if (item != null)
                {
                    Destroy(item.gameObject);
                }
            }

            spawnedItems.Clear();

            if (emptyStateText != null)
            {
                emptyStateText.SetActive(rows.Count == 0);
            }

            if (itemViewPrefab == null || listParent == null)
            {
                return;
            }

            foreach (BackpackItemRow row in rows)
            {
                BackpackItemView view = Instantiate(itemViewPrefab, listParent);
                view.Initialize(row, OnItemSelected);
                spawnedItems.Add(view);
            }
        }

        private void OnItemSelected(BackpackItemRow row, BackpackItemView view)
        {
            selectedRow = row;
            SelectedSerial = row.Serial;

            selectedView?.SetSelected(false);
            selectedView = view;
            selectedView.SetSelected(true);

            if (detailNameText != null)
            {
                detailNameText.text = row.DisplayName;
            }

            if (detailInfoText != null)
            {
                detailInfoText.text = row.Amount > 1 ? $"Amount: {row.Amount}" : string.Empty;
            }

            // Same IsWearable lookup GameActions.Equip itself checks internally
            // (via ItemHold.ItemData.IsWearable) - the UI gate and the action's
            // own guard agree, not a second/invented eligibility rule.
            if (equipButton != null)
            {
                equipButton.gameObject.SetActive(TileDataLoader.Instance.StaticData[row.Graphic].IsWearable);
            }

            if (detailRoot != null)
            {
                detailRoot.SetActive(true);
            }
        }

        // Deliberately just DoubleClick - the server/ClassicUO action system already
        // decides what that means per item type (equip/eat/drink/open/...), same
        // "server decides" philosophy PrimaryActionDefinitionFactory.BuildTalk already
        // documents. No separate food/potion/weapon/armor logic here.
        private void UseSelected()
        {
            if (Diag.IsEnabled(DiagCategory.Actions))
            {
                Diag.Log(DiagCategory.Actions, $"Backpack Use firing, serial={selectedRow.Serial}");
            }

            GameActions.DoubleClick(selectedRow.Serial);
            CloseDetail();
        }

        // Mobile Equipment Interaction v1: PickUp() + Equip() - the exact same two
        // top-level GameActions a real drag-from-backpack-to-paperdoll produces at
        // its start and end, called back-to-back instead of across a drag gesture.
        // No new packets (PPickUpRequest/PEquipRequest both already exist), no
        // client-side layer assignment (Equip() sends ItemHold.ItemData.Layer - the
        // item's own data-driven layer, never something this code picks), server
        // remains authoritative (the existing EquipItem/UpdateContainedItem packet
        // handlers, untouched, update Item.Layer/Container when the server confirms).
        //
        // Close(), not just CloseDetail(): a successful Equip changes what belongs
        // in this list (the item leaves the backpack), and Populate() only runs in
        // Open() - closing forces a fresh read next time, the same reasoning
        // PaperdollPresenter's Unequip applies. The server round-trip hasn't landed
        // by the time this method returns, so an immediate re-Populate() here
        // wouldn't show the change anyway.
        private void EquipSelected()
        {
            if (Diag.IsEnabled(DiagCategory.Actions))
            {
                Diag.Log(DiagCategory.Actions, $"Backpack Equip firing, serial={selectedRow.Serial}");
            }

            bool pickedUp = GameActions.PickUp(selectedRow.Serial, 0, 0);

            if (pickedUp)
            {
                GameActions.Equip();

                // Recovery, not a new mechanism: PickUp can succeed while Equip's
                // own guard (IsWearable/IsFixedPosition) declines to send anything,
                // which would otherwise leave ItemHold stuck enabled and block every
                // future pickup. ItemHold.Clear() is an existing method.
                if (ItemHold.Enabled)
                {
                    ItemHold.Clear();
                }
            }

            Close();
        }
    }
}
