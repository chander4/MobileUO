using System;
using UnityEngine;
using UnityEngine.UI;

namespace Hollowmere.Interaction
{
    /// <summary>
    /// One row in the Spell/Skill list. Pure presentation - SpellSkillPresenter owns
    /// deciding which CatalogEntry values to show; this only renders one and reports
    /// taps. Self-constructs its own child hierarchy in Build(), same delivery
    /// mechanism PaperdollSlotView already uses (see SpellSkillPresenter's class doc).
    /// </summary>
    internal class SpellSkillItemView : MonoBehaviour
    {
        private static readonly Color NormalBackground = new Color(0.15f, 0.15f, 0.15f, 0.9f);
        private static readonly Color SelectedBackground = new Color(0.85f, 0.65f, 0.2f, 1f);

        private Button button;
        private Image background;
        private Image iconImage;
        private Text nameText;
        private Text categoryText;
        private Sprite iconSprite;

        private CatalogEntry entry;
        private Action<CatalogEntry, SpellSkillItemView> onSelected;

        internal void Build(Transform parent, float height)
        {
            var rect = gameObject.AddComponent<RectTransform>();
            rect.SetParent(parent, false);

            var layoutElement = gameObject.AddComponent<LayoutElement>();
            layoutElement.minHeight = height;
            layoutElement.preferredHeight = height;

            background = gameObject.AddComponent<Image>();
            background.color = NormalBackground;

            button = gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(() => onSelected?.Invoke(entry, this));

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(rect, false);
            var iconRect = (RectTransform)iconGo.transform;
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.sizeDelta = new Vector2(height - 8f, height - 8f);
            iconRect.anchoredPosition = new Vector2(4f, 0f);
            iconImage = iconGo.GetComponent<Image>();
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            iconImage.enabled = false;

            var nameGo = new GameObject("Name", typeof(RectTransform), typeof(Text));
            nameGo.transform.SetParent(rect, false);
            var nameRect = (RectTransform)nameGo.transform;
            nameRect.anchorMin = new Vector2(0f, 0.5f);
            nameRect.anchorMax = new Vector2(1f, 1f);
            nameRect.offsetMin = new Vector2(height + 4f, 0f);
            nameRect.offsetMax = new Vector2(-8f, -2f);
            nameText = nameGo.GetComponent<Text>();
            nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameText.fontSize = 14;
            nameText.alignment = TextAnchor.MiddleLeft;
            nameText.color = Color.white;
            nameText.horizontalOverflow = HorizontalWrapMode.Overflow;
            nameText.raycastTarget = false;

            var categoryGo = new GameObject("Category", typeof(RectTransform), typeof(Text));
            categoryGo.transform.SetParent(rect, false);
            var categoryRect = (RectTransform)categoryGo.transform;
            categoryRect.anchorMin = new Vector2(0f, 0f);
            categoryRect.anchorMax = new Vector2(1f, 0.5f);
            categoryRect.offsetMin = new Vector2(height + 4f, 2f);
            categoryRect.offsetMax = new Vector2(-8f, 0f);
            categoryText = categoryGo.GetComponent<Text>();
            categoryText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            categoryText.fontSize = 10;
            categoryText.alignment = TextAnchor.MiddleLeft;
            categoryText.color = new Color(1f, 1f, 1f, 0.6f);
            categoryText.raycastTarget = false;
        }

        internal void Initialize(CatalogEntry catalogEntry, Action<CatalogEntry, SpellSkillItemView> selectedCallback)
        {
            entry = catalogEntry;
            onSelected = selectedCallback;

            nameText.text = entry.Definition.Label;
            categoryText.text = entry.Category.ToString();

            ApplyIcon(entry.Definition.IconGraphic);
            SetSelected(false);
        }

        internal void SetSelected(bool selected)
        {
            background.color = selected ? SelectedBackground : NormalBackground;
        }

        // Spell/skill icons are Gump art (the small hotbar-style icon
        // ActionButtonGump's own GumpPic already renders natively for these same
        // entries on a real Action Bar button - see ActionButtonGump.cs), not Item
        // art - GumpsLoader is the Gump-art equivalent of ArtLoader, same
        // UOTexture32/.UnityTexture bridge BackpackItemView/PaperdollSlotView
        // already use for Item art.
        private void ApplyIcon(ushort? graphic)
        {
            if (iconSprite != null)
            {
                Destroy(iconSprite);
                iconSprite = null;
            }

            if (!graphic.HasValue)
            {
                iconImage.enabled = false;
                return;
            }

            var gumpTexture = ClassicUO.IO.Resources.GumpsLoader.Instance.GetTexture(graphic.Value);
            var unityTexture = gumpTexture?.UnityTexture as Texture2D;

            if (unityTexture == null)
            {
                iconImage.enabled = false;
                return;
            }

            iconSprite = Sprite.Create(
                unityTexture,
                new Rect(0, 0, unityTexture.width, unityTexture.height),
                new Vector2(0.5f, 0.5f));

            iconImage.sprite = iconSprite;
            iconImage.enabled = true;
        }

        private void OnDestroy()
        {
            if (iconSprite != null)
            {
                Destroy(iconSprite);
            }
        }
    }
}
