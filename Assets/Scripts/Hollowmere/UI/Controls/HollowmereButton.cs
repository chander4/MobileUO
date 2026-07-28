using UnityEngine;
using UnityEngine.UI;

namespace Hollowmere.UI
{
    public enum HollowmereButtonVariant
    {
        Primary,
        Secondary,
        Danger,
        Icon,
        Toolbar
    }

    /// <summary>
    /// Wraps Unity's Button/Selectable and drives its existing ColorBlock from
    /// the active theme. Does not reimplement pointer/hover/press handling.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class HollowmereButton : MonoBehaviour
    {
        [SerializeField] private HollowmereButtonVariant variant = HollowmereButtonVariant.Primary;
        [SerializeField] private Button button;

        public HollowmereButtonVariant Variant => variant;

        public bool Interactable
        {
            get => button != null && button.interactable;
            set
            {
                if (button != null)
                {
                    button.interactable = value;
                }
            }
        }

        private void Reset()
        {
            button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            var service = HollowmereUIService.Instance;
            if (service != null)
            {
                ApplyTheme(service.ActiveTheme);
                service.ThemeChanged += ApplyTheme;
            }
        }

        private void OnDisable()
        {
            var service = HollowmereUIService.Instance;
            if (service != null)
            {
                service.ThemeChanged -= ApplyTheme;
            }
        }

        public void ApplyTheme(HollowmereTheme theme)
        {
            if (theme == null || button == null)
            {
                return;
            }

            var baseColor = VariantColor(theme);
            var colors = button.colors;
            colors.normalColor = baseColor;
            colors.highlightedColor = Color.Lerp(baseColor, Color.white, 0.15f);
            colors.pressedColor = Color.Lerp(baseColor, Color.black, 0.15f);
            colors.selectedColor = Color.Lerp(baseColor, Color.white, 0.1f);
            colors.disabledColor = theme.Disabled;
            button.colors = colors;
        }

        private Color VariantColor(HollowmereTheme theme)
        {
            switch (variant)
            {
                case HollowmereButtonVariant.Primary:
                    return theme.Primary;
                case HollowmereButtonVariant.Secondary:
                    return theme.Secondary;
                case HollowmereButtonVariant.Danger:
                    return theme.Danger;
                case HollowmereButtonVariant.Icon:
                case HollowmereButtonVariant.Toolbar:
                    return theme.Surface;
                default:
                    return theme.Primary;
            }
        }
    }
}
