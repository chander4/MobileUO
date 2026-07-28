using UnityEngine;
using UnityEngine.UI;

namespace Hollowmere.UI
{
    public enum HollowmerePanelVariant
    {
        Card,
        Dialog,
        Toolbar,
        Sidebar,
        Popup,
        FloatingPanel,
        ScrollablePanel
    }

    /// <summary>
    /// Wraps Image plus the built-in Shadow/Outline UGUI effects and drives them
    /// from the active theme. Dialog/ScrollablePanel add a title bar or ScrollRect
    /// at the prefab level - this component only themes the shared background,
    /// border, and shadow.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class HollowmerePanel : MonoBehaviour
    {
        [SerializeField] private HollowmerePanelVariant variant = HollowmerePanelVariant.Card;
        [SerializeField] private Image background;
        [SerializeField] private Outline border;
        [SerializeField] private Shadow shadow;

        public HollowmerePanelVariant Variant => variant;

        private void Reset()
        {
            background = GetComponent<Image>();
        }

        private void OnEnable()
        {
            if (background == null)
            {
                background = GetComponent<Image>();
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
            if (theme == null || background == null)
            {
                return;
            }

            background.color = theme.Surface;

            if (border != null)
            {
                border.effectColor = theme.Border;
            }

            if (shadow != null)
            {
                var preset = ShadowForVariant();
                shadow.effectDistance = preset.distance;
                var shadowColor = theme.Background;
                shadowColor.a = preset.alpha;
                shadow.effectColor = shadowColor;
            }
        }

        private HollowmereMetrics.ShadowPreset ShadowForVariant()
        {
            switch (variant)
            {
                case HollowmerePanelVariant.Dialog:
                case HollowmerePanelVariant.Popup:
                    return HollowmereMetrics.Shadow.Strong;
                case HollowmerePanelVariant.FloatingPanel:
                case HollowmerePanelVariant.Sidebar:
                    return HollowmereMetrics.Shadow.Medium;
                default:
                    return HollowmereMetrics.Shadow.Soft;
            }
        }
    }
}
