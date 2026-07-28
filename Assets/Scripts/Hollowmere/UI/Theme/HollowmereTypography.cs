using System;
using UnityEngine;
using UnityEngine.UI;

namespace Hollowmere.UI
{
    [Serializable]
    public struct HollowmereTextStyle
    {
        public Font font;
        public int fontSize;
        public FontStyle fontStyle;
        public float lineSpacing;

        public void ApplyTo(Text target)
        {
            if (target == null)
            {
                return;
            }

            if (font != null)
            {
                target.font = font;
            }

            target.fontSize = fontSize;
            target.fontStyle = fontStyle;
            target.lineSpacing = lineSpacing;
        }
    }

    [CreateAssetMenu(fileName = "HollowmereTypography", menuName = "Hollowmere/UI/Typography")]
    public class HollowmereTypography : ScriptableObject
    {
        [SerializeField] private HollowmereTextStyle header = new HollowmereTextStyle { fontSize = 32, fontStyle = FontStyle.Bold, lineSpacing = 1f };
        [SerializeField] private HollowmereTextStyle subheader = new HollowmereTextStyle { fontSize = 22, fontStyle = FontStyle.Bold, lineSpacing = 1f };
        [SerializeField] private HollowmereTextStyle body = new HollowmereTextStyle { fontSize = 16, fontStyle = FontStyle.Normal, lineSpacing = 1f };
        [SerializeField] private HollowmereTextStyle caption = new HollowmereTextStyle { fontSize = 12, fontStyle = FontStyle.Normal, lineSpacing = 1f };
        [SerializeField] private HollowmereTextStyle button = new HollowmereTextStyle { fontSize = 16, fontStyle = FontStyle.Bold, lineSpacing = 1f };
        [SerializeField] private HollowmereTextStyle tooltip = new HollowmereTextStyle { fontSize = 12, fontStyle = FontStyle.Italic, lineSpacing = 1f };

        public HollowmereTextStyle Header => header;
        public HollowmereTextStyle Subheader => subheader;
        public HollowmereTextStyle Body => body;
        public HollowmereTextStyle Caption => caption;
        public HollowmereTextStyle Button => button;
        public HollowmereTextStyle Tooltip => tooltip;
    }
}
