using System;
using UnityEngine;

namespace Hollowmere.UI
{
    /// <summary>
    /// Deliberately small: active theme, active typography, and the change event.
    /// Does not grow to own accessibility/localization/notifications/UI scaling -
    /// those become their own services if a real requirement shows up later.
    /// DefaultExecutionOrder guarantees Awake (and Instance assignment) runs before
    /// any HollowmereButton/HollowmerePanel OnEnable in the same scene load, since
    /// Unity does not otherwise order Awake/OnEnable across sibling GameObjects.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class HollowmereUIService : MonoBehaviour
    {
        private const string ActiveThemePrefKey = "Hollowmere.ActiveTheme";

        public static HollowmereUIService Instance { get; private set; }

        [SerializeField] private HollowmereTheme[] availableThemes;
        [SerializeField] private HollowmereTypography typography;

        public HollowmereTheme ActiveTheme { get; private set; }
        public HollowmereTypography ActiveTypography => typography;

        public event Action<HollowmereTheme> ThemeChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            var savedThemeName = PlayerPrefs.GetString(ActiveThemePrefKey, string.Empty);
            ActiveTheme = FindTheme(savedThemeName) ?? FirstAvailableTheme();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public bool SwitchTheme(string themeName)
        {
            var theme = FindTheme(themeName);
            if (theme == null || theme == ActiveTheme)
            {
                return false;
            }

            ActiveTheme = theme;
            PlayerPrefs.SetString(ActiveThemePrefKey, theme.ThemeName);
            PlayerPrefs.Save();
            ThemeChanged?.Invoke(ActiveTheme);
            return true;
        }

        private HollowmereTheme FindTheme(string themeName)
        {
            if (string.IsNullOrEmpty(themeName) || availableThemes == null)
            {
                return null;
            }

            for (var i = 0; i < availableThemes.Length; i++)
            {
                if (availableThemes[i] != null && availableThemes[i].ThemeName == themeName)
                {
                    return availableThemes[i];
                }
            }

            return null;
        }

        private HollowmereTheme FirstAvailableTheme()
        {
            return availableThemes != null && availableThemes.Length > 0 ? availableThemes[0] : null;
        }
    }
}
