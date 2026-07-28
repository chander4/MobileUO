using UnityEngine;

namespace Hollowmere.UI
{
    [CreateAssetMenu(fileName = "HollowmereTheme", menuName = "Hollowmere/UI/Theme")]
    public class HollowmereTheme : ScriptableObject
    {
        [SerializeField] private string themeName = "Theme";

        [Header("Core")]
        [SerializeField] private Color primary = new Color(0.55f, 0.45f, 0.85f);
        [SerializeField] private Color secondary = new Color(0.35f, 0.35f, 0.4f);
        [SerializeField] private Color accent = new Color(0.85f, 0.65f, 0.2f);

        [Header("Surfaces")]
        [SerializeField] private Color background = new Color(0.07f, 0.07f, 0.09f);
        [SerializeField] private Color surface = new Color(0.12f, 0.12f, 0.15f);
        [SerializeField] private Color border = new Color(0.25f, 0.25f, 0.3f);

        [Header("Status")]
        [SerializeField] private Color success = new Color(0.3f, 0.7f, 0.4f);
        [SerializeField] private Color warning = new Color(0.85f, 0.65f, 0.2f);
        [SerializeField] private Color danger = new Color(0.8f, 0.3f, 0.3f);
        [SerializeField] private Color magic = new Color(0.4f, 0.55f, 0.9f);

        [Header("Text")]
        [SerializeField] private Color text = new Color(0.95f, 0.95f, 0.95f);
        [SerializeField] private Color mutedText = new Color(0.65f, 0.65f, 0.68f);
        [SerializeField] private Color disabled = new Color(0.4f, 0.4f, 0.42f);

        public string ThemeName => themeName;

        public Color Primary => primary;
        public Color Secondary => secondary;
        public Color Accent => accent;

        public Color Background => background;
        public Color Surface => surface;
        public Color Border => border;

        public Color Success => success;
        public Color Warning => warning;
        public Color Danger => danger;
        public Color Magic => magic;

        public Color Text => text;
        public Color MutedText => mutedText;
        public Color Disabled => disabled;
    }
}
