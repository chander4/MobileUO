using System.Collections.Generic;
using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Scenes;
using Hollowmere.HudRuntime;
using UnityEngine;
using UnityEngine.UI;
using GameObject = UnityEngine.GameObject;

namespace Hollowmere.Interaction
{
    /// <summary>
    /// Mobile UO Context Menu v1: a secondary action surface opened by
    /// WorldTapTargeting's existing long-press (the same pin a long press
    /// already produces - see WorldTapTargeting.OnLongPressDetected). Not a
    /// second gesture system and not a second LongPressStateMachine: this is
    /// only ever opened from the one call site that already detects a long
    /// press today.
    ///
    /// Action inventory audit (#36): for every target category long-press can
    /// pin (Item or Mobile - WorldTapTargeting never pins Land/Static), the
    /// only existing, safe, client-authoritative action NOT already covered by
    /// Primary Action is GameActions.SingleClick ("Examine" - the same request
    /// a native single-click sends; shows the entity's name/title, no state
    /// mutation, no server-side consequence). Attack/Talk/Loot/PickUp/Use are
    /// already Primary Action's job (PrimaryActionResolver/
    /// PrimaryActionDefinitionFactory) and are deliberately NOT duplicated
    /// here. GameActions.OpenPopupMenu (ContextMenuLongPressTrigger, currently
    /// unwired/inactive in this project) opens the native, SERVER-populated
    /// PopupMenuGump - an orthogonal, server-authoritative menu with
    /// label-text-sized hit boxes, not mobile touch sizing, and not
    /// enumerable ahead of time the way this task asks for; left untouched.
    /// Backpack Item is not applicable to this surface: WorldTapTargeting's
    /// long-press only ever pins a world-hit-test result (Item/Mobile under
    /// the finger in the game view), never a BackpackItemView tap - that
    /// surface already has its own Use/Equip/Unequip buttons in its own
    /// detail panel.
    ///
    /// Delivery mechanism: self-constructing Unity UI in code, the same shape
    /// Paperdoll/Backpack/SpellSkill already use (see SpellSkillPresenter's
    /// class doc). Not registered with HudWidgetHost/HudBuiltInProfiles - it
    /// has no persistent anchored slot, only ever exists while open, exactly
    /// like those three panels.
    /// </summary>
    internal class ContextActionMenuPresenter : MonoBehaviour
    {
        public static ContextActionMenuPresenter Instance { get; private set; }

        private const float RowWidth = 160f;
        private const float RowHeight = 44f;
        private const float RowSpacing = 4f;

        private static readonly Color PanelBackground = new Color(0.08f, 0.08f, 0.09f, 0.96f);
        private static readonly Color RowColor = new Color(0.18f, 0.18f, 0.18f, 1f);
        private static readonly Color RowHoverColor = new Color(0.3f, 0.3f, 0.3f, 1f);

        private GameObject catcherRoot;
        private GameObject panelRoot;
        private RectTransform panelRect;
        private VerticalLayoutGroup panelLayout;
        private readonly List<GameObject> spawnedRows = new List<GameObject>();

        private BaseGameObject openedFor;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var go = new GameObject("HollowmereContextActionMenuPresenter");
            Object.DontDestroyOnLoad(go);
            go.AddComponent<ContextActionMenuPresenter>();
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

        private void Update()
        {
            // No stale menu after the target changes: close the instant the
            // entity this menu was opened for stops being the current
            // selection (switched target, cleared selection, or the entity
            // itself became invalid) - the same per-frame self-consistency
            // check every other presenter in this HUD already does (compare
            // PrimaryActionPresenter.Tick()'s own reference check).
            if (openedFor == null)
            {
                return;
            }

            if (openedFor is ClassicUO.Game.GameObjects.GameObject go && go.IsDestroyed)
            {
                Close();
                return;
            }

            if (!ReferenceEquals(WorldTapTargeting.CurrentSelection, openedFor))
            {
                Close();
            }
        }

        public void Open(BaseGameObject target)
        {
            if (ClassicUO.Client.Game == null || !(ClassicUO.Client.Game.Scene is GameScene))
            {
                return;
            }

            if (!(target is Entity entity) || entity.IsDestroyed)
            {
                return;
            }

            List<GameActionDefinition> actions = BuildAdditionalActions(entity);
            if (actions.Count == 0)
            {
                return;
            }

            if (!EnsureBuilt())
            {
                return;
            }

            openedFor = target;
            PopulateRows(actions);
            Position(entity);

            catcherRoot.SetActive(true);
            panelRoot.SetActive(true);
        }

        public void Close()
        {
            openedFor = null;

            if (catcherRoot != null)
            {
                catcherRoot.SetActive(false);
            }

            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        // The only additional action available for any long-press-pinnable
        // target today - see class doc's action inventory audit. Returning an
        // empty list (rather than a placeholder row) is the answer to "what
        // happens when there's nothing more to offer": Open() simply does not
        // show a menu.
        private static List<GameActionDefinition> BuildAdditionalActions(Entity entity)
        {
            var actions = new List<GameActionDefinition>();

            if (entity == World.Player)
            {
                return actions;
            }

            uint serial = entity.Serial;
            string name = WorldEntityScreenAnchor.ResolveEntityName(entity as ClassicUO.Game.GameObjects.GameObject);

            actions.Add(new GameActionDefinition(
                "Examine",
                () => GameActions.SingleClick(serial),
                tooltip: $"Examine {name}"));

            return actions;
        }

        private void PopulateRows(List<GameActionDefinition> actions)
        {
            foreach (GameObject row in spawnedRows)
            {
                Destroy(row);
            }
            spawnedRows.Clear();

            foreach (GameActionDefinition action in actions)
            {
                GameActionDefinition capturedAction = action;

                var rowGo = new GameObject(capturedAction.Label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
                rowGo.transform.SetParent(panelRect, false);

                var layoutElement = rowGo.GetComponent<LayoutElement>();
                layoutElement.minHeight = RowHeight;
                layoutElement.preferredHeight = RowHeight;
                layoutElement.minWidth = RowWidth;

                Image rowImage = rowGo.GetComponent<Image>();
                rowImage.color = RowColor;

                Button rowButton = rowGo.GetComponent<Button>();
                rowButton.targetGraphic = rowImage;
                rowButton.onClick.AddListener(() =>
                {
                    capturedAction.TryExecute();
                    Close();
                });

                var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
                labelGo.transform.SetParent(rowGo.transform, false);
                var labelRect = (RectTransform)labelGo.transform;
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(10f, 0f);
                labelRect.offsetMax = new Vector2(-10f, 0f);
                Text label = labelGo.GetComponent<Text>();
                label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                label.fontSize = 16;
                label.alignment = TextAnchor.MiddleLeft;
                label.color = Color.white;
                label.text = capturedAction.Label;
                label.raycastTarget = false;

                spawnedRows.Add(rowGo);
            }
        }

        // Anchored offset up-and-right of the target's own screen point - near
        // it without sitting on top of it, the same "don't cover the thing
        // you're pointing at" intent WorldEntityScreenAnchor's own info panel
        // already follows (offset above the marker ring, not centered on it).
        private void Position(Entity entity)
        {
            if (!(entity is ClassicUO.Game.GameObjects.GameObject gameObject))
            {
                return;
            }

            Vector2 screenPoint = WorldEntityScreenAnchor.WorldToScreenPoint(gameObject);
            Canvas canvas = panelRect.GetComponentInParent<Canvas>();
            RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;

            Vector2 anchoredPosition = screenPoint;
            if (canvasRect != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out anchoredPosition);
            }

            panelRect.anchoredPosition = anchoredPosition + new Vector2(40f, 40f);
        }

        private bool EnsureBuilt()
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

            // Full-screen invisible catcher behind the panel - tapping
            // anywhere outside the menu's own rows closes it. Sibling order
            // (added before panelRoot) puts it behind the panel, so taps on
            // the panel/rows reach them first; standard Unity UI raycast
            // ordering, not a second input system.
            catcherRoot = new GameObject("HollowmereContextMenuCatcher", typeof(RectTransform), typeof(Image), typeof(Button));
            catcherRoot.transform.SetParent(canvas.transform, false);
            var catcherRect = (RectTransform)catcherRoot.transform;
            catcherRect.anchorMin = Vector2.zero;
            catcherRect.anchorMax = Vector2.one;
            catcherRect.offsetMin = Vector2.zero;
            catcherRect.offsetMax = Vector2.zero;
            Image catcherImage = catcherRoot.GetComponent<Image>();
            catcherImage.color = new Color(0f, 0f, 0f, 0f);
            Button catcherButton = catcherRoot.GetComponent<Button>();
            catcherButton.targetGraphic = catcherImage;
            catcherButton.onClick.AddListener(Close);
            catcherRoot.SetActive(false);

            panelRoot = new GameObject("HollowmereContextActionMenu", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            panelRoot.transform.SetParent(canvas.transform, false);

            panelRect = (RectTransform)panelRoot.transform;
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.sizeDelta = new Vector2(RowWidth, 0f);
            panelRoot.GetComponent<Image>().color = PanelBackground;

            panelLayout = panelRoot.GetComponent<VerticalLayoutGroup>();
            panelLayout.padding = new RectOffset(4, 4, 4, 4);
            panelLayout.spacing = RowSpacing;
            panelLayout.childForceExpandHeight = false;
            panelLayout.childForceExpandWidth = true;
            panelLayout.childControlHeight = true;
            panelLayout.childControlWidth = true;

            ContentSizeFitter fitter = panelRoot.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            panelRoot.SetActive(false);

            return true;
        }
    }
}
