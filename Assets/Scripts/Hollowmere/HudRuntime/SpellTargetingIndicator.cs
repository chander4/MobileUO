using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using UnityEngine;
using UnityEngine.UI;
using GameObject = UnityEngine.GameObject;

namespace Hollowmere.HudRuntime
{
    /// <summary>
    /// Mobile Spell Casting & Targeting v1: a thin, read-only reflection of
    /// TargetManager.IsTargeting - not a second targeting system, not a cursor
    /// replacement. Owns no targeting state of its own: every frame it just
    /// reads the same public TargetManager.IsTargeting/TargetingType the
    /// desktop's own GameCursor already reads to swap in the crosshair cursor
    /// (see GameCursor.cs:370) - that cursor swap doesn't render usefully on
    /// mobile (RunMouseInASeparateThread suppresses GameCursor's own draw on
    /// Application.isMobilePlatform - see ClientRunner.cs), so this is the
    /// mobile-visible equivalent of the same existing signal, not a new one.
    ///
    /// Cancel button calls TargetManager.CancelTarget() directly - the exact
    /// same call GameSceneInputHandler.OnKeyDown makes for the desktop Escape
    /// key (GameSceneInputHandler.cs:772) - no new cancellation protocol.
    ///
    /// Same self-installing runtime-bootstrap shape as WorldEntityScreenAnchor/
    /// WorldTapTargeting - no scene/prefab authoring required.
    /// </summary>
    internal static class SpellTargetingIndicator
    {
        private static readonly Color BannerBackground = new Color(0.08f, 0.08f, 0.1f, 0.92f);
        private static readonly Color NeutralColor = new Color(0.8f, 0.8f, 0.85f, 1f);
        private static readonly Color HarmfulColor = new Color(0.9f, 0.35f, 0.3f, 1f);
        private static readonly Color BeneficialColor = new Color(0.4f, 0.8f, 0.4f, 1f);

        private static Runner instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var go = new GameObject("HollowmereSpellTargetingIndicator");
            Object.DontDestroyOnLoad(go);
            instance = go.AddComponent<Runner>();
        }

        private sealed class Runner : MonoBehaviour
        {
            private GameObject bannerRoot;
            private Text bannerText;
            private bool wasTargeting;

            private void Update()
            {
                if (ClassicUO.Client.Game == null || !(ClassicUO.Client.Game.Scene is GameScene))
                {
                    SetVisible(false);
                    return;
                }

                bool isTargeting = TargetManager.IsTargeting;

                if (isTargeting == wasTargeting)
                {
                    return;
                }

                wasTargeting = isTargeting;

                if (!isTargeting)
                {
                    SetVisible(false);
                    return;
                }

                if (!EnsureBanner())
                {
                    return;
                }

                bannerText.text = "Select a target";
                bannerText.color = TargetManager.TargetingType switch
                {
                    TargetType.Harmful => HarmfulColor,
                    TargetType.Beneficial => BeneficialColor,
                    _ => NeutralColor
                };

                SetVisible(true);
            }

            private void SetVisible(bool visible)
            {
                if (bannerRoot != null)
                {
                    bannerRoot.SetActive(visible);
                }
            }

            private void Cancel()
            {
                if (TargetManager.IsTargeting)
                {
                    TargetManager.CancelTarget();
                }
            }

            private bool EnsureBanner()
            {
                if (bannerRoot != null)
                {
                    return true;
                }

                Canvas canvas = Object.FindFirstObjectByType<Canvas>();
                if (canvas == null)
                {
                    return false;
                }

                bannerRoot = new GameObject("HollowmereSpellTargetingBanner", typeof(RectTransform), typeof(Image));
                bannerRoot.transform.SetParent(canvas.transform, false);

                var bannerRect = (RectTransform)bannerRoot.transform;
                bannerRect.anchorMin = new Vector2(0.5f, 1f);
                bannerRect.anchorMax = new Vector2(0.5f, 1f);
                bannerRect.pivot = new Vector2(0.5f, 1f);
                bannerRect.sizeDelta = new Vector2(280f, 40f);
                bannerRect.anchoredPosition = new Vector2(0f, -8f);
                bannerRoot.GetComponent<Image>().color = BannerBackground;

                var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
                textGo.transform.SetParent(bannerRect, false);
                var textRect = (RectTransform)textGo.transform;
                textRect.anchorMin = new Vector2(0f, 0f);
                textRect.anchorMax = new Vector2(1f, 1f);
                textRect.offsetMin = new Vector2(12f, 0f);
                textRect.offsetMax = new Vector2(-84f, 0f);
                bannerText = textGo.GetComponent<Text>();
                bannerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                bannerText.fontSize = 14;
                bannerText.alignment = TextAnchor.MiddleLeft;
                bannerText.raycastTarget = false;

                var cancelGo = new GameObject("Cancel", typeof(RectTransform), typeof(Image), typeof(Button));
                cancelGo.transform.SetParent(bannerRect, false);
                var cancelRect = (RectTransform)cancelGo.transform;
                cancelRect.anchorMin = new Vector2(1f, 0.5f);
                cancelRect.anchorMax = new Vector2(1f, 0.5f);
                cancelRect.pivot = new Vector2(1f, 0.5f);
                cancelRect.sizeDelta = new Vector2(72f, 30f);
                cancelRect.anchoredPosition = new Vector2(-6f, 0f);
                cancelGo.GetComponent<Image>().color = new Color(0.3f, 0.15f, 0.15f, 1f);
                Button cancelButton = cancelGo.GetComponent<Button>();
                cancelButton.onClick.AddListener(Cancel);

                var cancelLabelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
                cancelLabelGo.transform.SetParent(cancelRect, false);
                var cancelLabelRect = (RectTransform)cancelLabelGo.transform;
                cancelLabelRect.anchorMin = Vector2.zero;
                cancelLabelRect.anchorMax = Vector2.one;
                cancelLabelRect.offsetMin = Vector2.zero;
                cancelLabelRect.offsetMax = Vector2.zero;
                Text cancelLabel = cancelLabelGo.GetComponent<Text>();
                cancelLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                cancelLabel.fontSize = 12;
                cancelLabel.alignment = TextAnchor.MiddleCenter;
                cancelLabel.color = Color.white;
                cancelLabel.text = "Cancel";
                cancelLabel.raycastTarget = false;

                bannerRoot.SetActive(false);

                return true;
            }
        }
    }
}
