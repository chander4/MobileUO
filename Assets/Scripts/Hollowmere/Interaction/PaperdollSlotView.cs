using System;
using UnityEngine;
using UnityEngine.UI;

namespace Hollowmere.Interaction
{
    /// <summary>
    /// One equipment slot cell in the Paperdoll grid. Pure presentation -
    /// PaperdollPresenter owns deciding what's equipped where; this only renders
    /// one slot and reports taps.
    ///
    /// Mirrors BackpackItemView's responsibilities (per-row Initialize, SetSelected,
    /// icon rendering via the same ArtLoader bridge) but builds its own child
    /// hierarchy in Build() rather than expecting a pre-wired prefab with
    /// [SerializeField] references - see PaperdollPresenter's class doc for why
    /// this slice uses the self-constructing delivery mechanism instead.
    /// </summary>
    internal class PaperdollSlotView : MonoBehaviour
    {
        // Mobile Paperdoll/Backpack Read-Only Polish v1: pushed further apart
        // (was 0.12/0.7 vs 0.22/0.95 - too close in luminance to read at a glance
        // on a phone) plus a faint inset "socket" block for empty slots (see
        // EmptySocketColor below) rather than relying on background alone.
        private static readonly Color EmptyBackground = new Color(0.08f, 0.08f, 0.08f, 0.75f);
        private static readonly Color FilledBackground = new Color(0.26f, 0.26f, 0.29f, 0.95f);
        private static readonly Color SelectedBackground = new Color(0.85f, 0.65f, 0.2f, 1f);
        private static readonly Color EmptySocketColor = new Color(1f, 1f, 1f, 0.08f);
        private static readonly Color EmptyLabelColor = new Color(1f, 1f, 1f, 0.4f);
        private static readonly Color FilledLabelColor = new Color(1f, 1f, 1f, 0.85f);

        private Button button;
        private Image background;
        private Image iconImage;
        private Text layerLabel;
        private Sprite iconSprite;

        private PaperdollSlotRow row;
        private Action<PaperdollSlotRow> onSelected;
        private bool selected;

        internal void Build(Transform parent)
        {
            var rect = gameObject.AddComponent<RectTransform>();
            rect.SetParent(parent, false);

            background = gameObject.AddComponent<Image>();
            background.color = EmptyBackground;

            button = gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(() => onSelected?.Invoke(row));

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(rect, false);
            var iconRect = (RectTransform)iconGo.transform;
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(4f, 13f);
            iconRect.offsetMax = new Vector2(-4f, -4f);
            iconImage = iconGo.GetComponent<Image>();
            iconImage.raycastTarget = false;
            iconImage.enabled = false;
            iconImage.preserveAspect = true;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(rect, false);
            var labelRect = (RectTransform)labelGo.transform;
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 0f);
            labelRect.pivot = new Vector2(0.5f, 0f);
            labelRect.sizeDelta = new Vector2(0f, 12f);
            labelRect.anchoredPosition = Vector2.zero;
            layerLabel = labelGo.GetComponent<Text>();
            layerLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            layerLabel.fontSize = 9;
            layerLabel.alignment = TextAnchor.MiddleCenter;
            layerLabel.color = new Color(1f, 1f, 1f, 0.7f);
            layerLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            layerLabel.verticalOverflow = VerticalWrapMode.Truncate;
            layerLabel.raycastTarget = false;
        }

        internal void SetRow(PaperdollSlotRow newRow, Action<PaperdollSlotRow> selectedCallback)
        {
            row = newRow;
            onSelected = selectedCallback;
            layerLabel.text = row.Layer.ToString();

            if (row.IsEmpty)
            {
                if (iconSprite != null)
                {
                    Destroy(iconSprite);
                    iconSprite = null;
                }

                // Keep the icon quad enabled but empty - a faint inset block reads
                // as "this slot exists and is empty" rather than a flat cell that's
                // only distinguishable by a subtle background shade. No placeholder
                // artwork: just a plain colored quad, same Image component the real
                // icon uses.
                iconImage.sprite = null;
                iconImage.color = EmptySocketColor;
                iconImage.enabled = true;

                layerLabel.color = EmptyLabelColor;
                background.color = selected ? SelectedBackground : EmptyBackground;
                return;
            }

            ApplyIcon(row.Graphic);
            layerLabel.color = FilledLabelColor;
            background.color = selected ? SelectedBackground : FilledBackground;
        }

        internal void SetSelected(bool value)
        {
            selected = value;
            background.color = row.IsEmpty ? EmptyBackground : (selected ? SelectedBackground : FilledBackground);
        }

        // Icon art: ArtLoader.Instance.GetTexture(graphic) already returns an ArtTexture
        // whose .UnityTexture is a real, already-populated UnityEngine.Texture2D - the
        // exact bridge BackpackItemView.ApplyIcon already uses. Full-texture-rect Sprite
        // (not ArtTexture.ImageRectangle-cropped like the desktop Paperdoll's
        // ItemGumpFixed) deliberately matches Backpack's already-proven-working
        // approach rather than an unverified crop - see class doc's limitations note.
        private void ApplyIcon(ushort graphic)
        {
            Sprite sprite = TryBuildIconSprite(graphic);

            if (iconSprite != null)
            {
                Destroy(iconSprite);
            }

            iconSprite = sprite;

            if (sprite == null)
            {
                iconImage.sprite = null;
                iconImage.color = EmptySocketColor;
            }
            else
            {
                iconImage.sprite = sprite;
                iconImage.color = Color.white;
            }

            iconImage.enabled = true;
        }

        // Mobile Paperdoll/Backpack Read-Only Polish v1: shared with
        // PaperdollPresenter's detail-panel icon preview, so there's one copy of
        // the ArtLoader bridge call in this file rather than a third copy of it.
        internal static Sprite TryBuildIconSprite(ushort graphic)
        {
            var artTexture = ClassicUO.IO.Resources.ArtLoader.Instance.GetTexture(graphic);
            var unityTexture = artTexture?.UnityTexture as Texture2D;

            return unityTexture != null
                ? Sprite.Create(unityTexture, new Rect(0, 0, unityTexture.width, unityTexture.height), new Vector2(0.5f, 0.5f))
                : null;
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
