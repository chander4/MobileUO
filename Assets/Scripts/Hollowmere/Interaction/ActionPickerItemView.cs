using System;
using UnityEngine;
using UnityEngine.UI;
using Hollowmere.UI;

namespace Hollowmere.Interaction
{
    /// <summary>
    /// A single row in the Action Picker list. Pure presentation - the picker owns
    /// deciding which entries to show; this only renders one and reports taps.
    /// Needs Inspector wiring once a prefab exists: a Button, a Text label for
    /// GameActionDefinition.Label, and a smaller Text for the category name.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ActionPickerItemView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Text labelText;
        [SerializeField] private Text categoryText;

        private CatalogEntry entry;
        private Action<CatalogEntry> onSelected;

        private void Reset()
        {
            button = GetComponent<Button>();
        }

        public void Initialize(CatalogEntry catalogEntry, Action<CatalogEntry> selectedCallback, bool alreadyOnBar = false)
        {
            entry = catalogEntry;
            onSelected = selectedCallback;

            if (labelText != null)
            {
                // Suffix rather than a separate UI element - keeps the "already added"
                // signal readable without needing another wired reference per row for
                // what's otherwise a rare state (most rows aren't already on the bar).
                labelText.text = alreadyOnBar ? entry.Definition.Label + "  ✓ Added" : entry.Definition.Label;
            }

            if (categoryText != null)
            {
                categoryText.text = entry.Category.ToString();
            }

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.interactable = !alreadyOnBar;

                if (!alreadyOnBar)
                {
                    button.onClick.AddListener(() => onSelected?.Invoke(entry));
                }
            }
        }
    }
}
