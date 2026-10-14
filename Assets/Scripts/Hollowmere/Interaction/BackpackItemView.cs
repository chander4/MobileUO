using System;
using UnityEngine;
using UnityEngine.UI;

namespace Hollowmere.Interaction
{
    /// <summary>
    /// A single cell in the Backpack grid. Pure presentation - BackpackPresenter owns
    /// deciding which items to show; this only renders one and reports taps.
    ///
    /// Icon art: ArtLoader.Instance.GetTexture(graphic) already returns an ArtTexture
    /// (XNAEmulator Texture2D subclass) whose .UnityTexture is a real, already-populated
    /// UnityEngine.Texture2D - UnityMainThreadDispatcher.Dispatch runs synchronously
    /// when already on the main thread (confirmed in UnityMainThreadDispatcher.cs), which
    /// this always is here. No new asset pipeline; this wraps that existing bridge in a
    /// Sprite for the one Image that needs it.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class BackpackItemView : MonoBehaviour
    {
        private static readonly Color NormalBackground = new Color(0.15f, 0.15f, 0.15f, 0.9f);
        private static readonly Color SelectedBackground = new Color(0.85f, 0.65f, 0.2f, 1f);

        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private Image iconImage;
        [SerializeField] private Text nameText;
        [SerializeField] private Text amountText;

        private BackpackItemRow row;
        private Action<BackpackItemRow, BackpackItemView> onSelected;
        private Sprite iconSprite;

        private void Reset()
        {
            button = GetComponent<Button>();
        }

        public void Initialize(BackpackItemRow itemRow, Action<BackpackItemRow, BackpackItemView> selectedCallback)
        {
            row = itemRow;
            onSelected = selectedCallback;

            if (nameText != null)
            {
                nameText.text = row.DisplayName;
            }

            if (amountText != null)
            {
                amountText.text = row.Amount > 1 ? $"x{row.Amount}" : string.Empty;
            }

            ApplyIcon(row.Graphic);
            SetSelected(false);

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => onSelected?.Invoke(row, this));
            }
        }

        public void SetSelected(bool selected)
        {
            if (background != null)
            {
                background.color = selected ? SelectedBackground : NormalBackground;
            }
        }

        private void ApplyIcon(ushort graphic)
        {
            if (iconImage == null)
            {
                return;
            }

            var artTexture = ClassicUO.IO.Resources.ArtLoader.Instance.GetTexture(graphic);
            var unityTexture = artTexture?.UnityTexture as Texture2D;

            if (unityTexture == null)
            {
                iconImage.enabled = false;
                return;
            }

            if (iconSprite != null)
            {
                Destroy(iconSprite);
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
