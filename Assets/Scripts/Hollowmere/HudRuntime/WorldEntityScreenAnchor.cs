using ClassicUO.Game.GameObjects;
using Hollowmere.Interaction;
using UnityEngine;
using UnityEngine.UI;
using GameObject = UnityEngine.GameObject;

namespace Hollowmere.HudRuntime
{
    /// <summary>
    /// Mobile Target Feedback v1: a Unity-owned ring marker that follows
    /// WorldTapTargeting.CurrentSelection - the same "what is currently selected
    /// on mobile" answer PrimaryActionPresenter itself consults (see that
    /// property's doc). Started life as a POC proving ClassicUO world entity ->
    /// BaseGameObject.RealScreenPosition -> Unity-owned visual presentation;
    /// this is that same seam, now pointed at the real mobile selection instead
    /// of the (unrelated) combat-target concept TargetVitalsPresenter tracks.
    ///
    /// Deliberately reads only WorldTapTargeting.CurrentSelection - no
    /// World.Mobiles scan, no independent resolution, no second selection
    /// store. Purely presentation: this never writes back into SelectedObject
    /// or WorldTapTargeting's pin, matching the architecture's separation of
    /// "target feedback" and "state" as two independent readers of the same
    /// source (see WorldTapTargeting's class doc diagram).
    ///
    /// Renderer boundary: this only ever reads BaseGameObject.RealScreenPosition
    /// (a plain field, refreshed every frame by the existing ClassicUO draw
    /// pass) and positions a Unity UI Image. It never touches GameScene,
    /// UltimaBatcher2D, GameObject.Draw(), or any ClassicUO rendering code.
    ///
    /// Same self-installing runtime-bootstrap shape already used by
    /// ActionBarBootstrap / PrimaryActionButtonBootstrap / QuickChatBarBootstrap /
    /// BackpackTriggerBootstrap / WorldTapTargeting - no scene/prefab authoring
    /// required, single file, trivially removable.
    /// </summary>
    internal static class WorldEntityScreenAnchor
    {
        private const float MarkerDiameter = 56f;
        private const float RingThickness = 5f;
        private static readonly Color MarkerColor = new Color(1f, 0.85f, 0.15f, 0.9f);

        // Mobile Target Info v1 sizing - a small label (+ optional HP fill for
        // Mobiles) anchored above the ring, not a target frame.
        private const float InfoWidth = 140f;
        private const float InfoHeight = 28f;
        private const float InfoOffsetAboveRing = MarkerDiameter * 0.5f + 26f;
        private const float HpBarHeight = 6f;
        private static readonly Color InfoBackgroundColor = new Color(0f, 0f, 0f, 0.6f);
        private static readonly Color HpBarBackgroundColor = new Color(0.15f, 0f, 0f, 0.8f);
        private static readonly Color HpFillColor = new Color(0.75f, 0.15f, 0.15f, 0.95f);

        // Mobile Combat Feedback v1 pulse tuning.
        private const float PulseDuration = 0.35f;
        private const float PulseStartScale = 0.8f;
        private const float PulseEndScale = 1.6f;
        private static readonly Color PulseColor = new Color(1f, 0.25f, 0.15f, 0.95f);

        // Mobile Long-Press Visual Feedback v1: a brief emphasis on the
        // EXISTING marker ring itself (scale + brightness settling back to
        // normal) rather than a second indicator - deliberately a different
        // shape (settles down, not fades out), duration, and color from the
        // attack pulse above so the two never read as the same effect.
        private const float SelectionEmphasisDuration = 0.25f;
        private const float SelectionEmphasisPeakScale = 1.5f;
        private static readonly Color SelectionEmphasisColor = new Color(1f, 1f, 0.75f, 1f);

        // Mobile Empty-Target Feedback v1: a brief, subtle "tap acknowledged,
        // nothing there" ping at the raw tap location - small, dim, fade-only
        // (no scale animation), so it reads as a quiet acknowledgement rather
        // than competing with the brighter/larger selection ring or attack pulse.
        private const float EmptyTapDuration = 0.2f;
        private const float EmptyTapDiameter = 24f;
        private static readonly Color EmptyTapColor = new Color(0.9f, 0.9f, 0.9f, 0.5f);

        private static Runner instance;

        // Mobile UO Context Menu v1: promoted from Runner-private to here so
        // ContextActionMenuPresenter can position itself relative to the same
        // target without a second copy of this formula.
        internal static Vector2 WorldToScreenPoint(ClassicUO.Game.GameObjects.GameObject target)
        {
            float scale = (float)ClassicUO.Client.Game.Batcher.scale;
            return new Vector2(
                target.RealScreenPosition.X * scale,
                Screen.height - target.RealScreenPosition.Y * scale);
        }

        // Mobile UO Context Menu v1: promoted from Runner-private to here so
        // ContextActionMenuPresenter can label its "Examine" action with the
        // same name resolution, not a second copy of this fallback chain.
        internal static string ResolveEntityName(ClassicUO.Game.GameObjects.GameObject target)
        {
            switch (target)
            {
                case Item item:
                    return BackpackPresenter.ResolveDisplayName(item);
                case Mobile mobile:
                    return string.IsNullOrEmpty(mobile.Name) ? "Unknown" : mobile.Name;
                default:
                    return "Unknown";
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var go = new GameObject("HollowmereWorldEntityScreenAnchor");
            Object.DontDestroyOnLoad(go);
            instance = go.AddComponent<Runner>();
        }

        /// <summary>Mobile Combat Feedback v1: a brief pulse at the attacked mobile's
        /// current screen position, independent of WorldTapTargeting.CurrentSelection -
        /// the selection clears the same frame this fires (see
        /// PrimaryActionDefinitionFactory.BuildAttack/ClearSelection), so this tracks
        /// the mobile reference directly rather than the (already-clearing) selection.</summary>
        internal static void TriggerAttackFeedback(BaseGameObject target)
        {
            instance?.TriggerAttackPulse(target);
        }

        /// <summary>Mobile Long-Press Visual Feedback v1: called only from
        /// WorldTapTargeting's long-press handler, right after a successful pin -
        /// never from short tap, and never for a long press that didn't resolve to
        /// a Mobile/Item (ground/no-op) or that UI/joystick exclusion already
        /// rejected, since those paths never reach the pin that calls this.</summary>
        internal static void TriggerSelectionFeedback(BaseGameObject target)
        {
            instance?.TriggerSelectionEmphasis(target);
        }

        /// <summary>Mobile Empty-Target Feedback v1: called only from WorldTapTargeting's
        /// captureNextFrame path, and only when a tap resolved to neither an entity nor a
        /// walkable destination - never for entity taps, never for ground taps that issued
        /// movement, never for UI/joystick-excluded taps (those never reach captureNextFrame
        /// at all).</summary>
        internal static void TriggerEmptyTapFeedback(Vector2 screenPosition)
        {
            instance?.TriggerEmptyTapPing(screenPosition);
        }

        private sealed class Runner : MonoBehaviour
        {
            private RectTransform canvasRect;
            private RectTransform markerTransform;
            private GameObject markerObject;

            private GameObject infoObject;
            private Text nameLabel;
            private GameObject hpBarObject;
            private RectTransform hpFillTransform;
            private BaseGameObject lastLabeledTarget;

            private GameObject pulseObject;
            private RectTransform pulseTransform;
            private Image pulseImage;
            private Coroutine pulseCoroutine;

            private Image markerImage;
            private Coroutine selectionEmphasisCoroutine;

            private GameObject emptyTapObject;
            private RectTransform emptyTapTransform;
            private Image emptyTapImage;
            private Coroutine emptyTapCoroutine;

            private void Update()
            {
                if (ClassicUO.Client.Game == null || !(ClassicUO.Client.Game.Scene is ClassicUO.Game.Scenes.GameScene))
                {
                    HideMarker();
                    return;
                }

                BaseGameObject selected = WorldTapTargeting.CurrentSelection;

                // Defensive: WorldTapTargeting.PinnedEntity already self-clears on
                // IsDestroyed (see its Pinned getter), but SelectedObject.LastObject
                // is engine-owned - guard here too rather than ever showing a marker
                // on something already gone this frame. Presentation-only check:
                // this never writes back to SelectedObject/the pin.
                if (!(selected is ClassicUO.Game.GameObjects.GameObject target) || target.IsDestroyed)
                {
                    HideMarker();
                    return;
                }

                if (!EnsureMarker())
                {
                    return;
                }

                markerObject.SetActive(true);

                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, WorldToScreenPoint(target), null, out Vector2 localPoint))
                {
                    markerTransform.anchoredPosition = localPoint;
                }

                UpdateInfo(target);
            }

            // infoObject/hpBarObject are children of markerTransform (see
            // EnsureMarker), so repositioning the ring above already moves both -
            // this only needs to refresh their content, not their position.
            private void UpdateInfo(ClassicUO.Game.GameObjects.GameObject target)
            {
                if (!ReferenceEquals(target, lastLabeledTarget))
                {
                    lastLabeledTarget = target;
                    nameLabel.text = ResolveEntityName(target);
                }

                if (target is Mobile mobile && mobile.HitsMax > 0)
                {
                    hpBarObject.SetActive(true);
                    float ratio = Mathf.Clamp01(mobile.Hits / (float)mobile.HitsMax);
                    Vector2 size = hpFillTransform.sizeDelta;
                    size.x = (InfoWidth - 4f) * ratio;
                    hpFillTransform.sizeDelta = size;
                }
                else
                {
                    hpBarObject.SetActive(false);
                }
            }

            private void HideMarker()
            {
                if (markerObject != null)
                {
                    markerObject.SetActive(false);
                }

                // Selection changed/cleared mid-emphasis (e.g. target destroyed,
                // action fired and cleared selection) - stop and reset rather than
                // let a stale scale/color carry into the marker's next appearance.
                if (selectionEmphasisCoroutine != null)
                {
                    StopCoroutine(selectionEmphasisCoroutine);
                    selectionEmphasisCoroutine = null;
                }

                if (markerTransform != null)
                {
                    markerTransform.localScale = Vector3.one;
                }

                if (markerImage != null)
                {
                    markerImage.color = MarkerColor;
                }

                lastLabeledTarget = null;
            }

            internal void TriggerAttackPulse(BaseGameObject target)
            {
                if (!(target is ClassicUO.Game.GameObjects.GameObject pulseTarget) || pulseTarget.IsDestroyed)
                {
                    return;
                }

                if (!EnsurePulse())
                {
                    return;
                }

                // Only one pulse at a time (same "single marker" simplicity as the
                // persistent ring) - a fresh attack restarts it rather than stacking.
                if (pulseCoroutine != null)
                {
                    StopCoroutine(pulseCoroutine);
                }

                pulseCoroutine = StartCoroutine(PulseRoutine(pulseTarget));
            }

            private System.Collections.IEnumerator PulseRoutine(ClassicUO.Game.GameObjects.GameObject target)
            {
                pulseObject.SetActive(true);
                float elapsed = 0f;

                while (elapsed < PulseDuration)
                {
                    // The mobile can die/despawn mid-pulse (server confirms the hit) -
                    // just end early rather than animate against a stale position.
                    if (target.IsDestroyed)
                    {
                        break;
                    }

                    float t = elapsed / PulseDuration;
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, WorldToScreenPoint(target), null, out Vector2 localPoint))
                    {
                        pulseTransform.anchoredPosition = localPoint;
                    }

                    float scale = Mathf.Lerp(PulseStartScale, PulseEndScale, t);
                    pulseTransform.localScale = new Vector3(scale, scale, 1f);

                    Color c = PulseColor;
                    c.a = PulseColor.a * (1f - t);
                    pulseImage.color = c;

                    elapsed += Time.deltaTime;
                    yield return null;
                }

                pulseObject.SetActive(false);
                pulseCoroutine = null;
            }

            internal void TriggerSelectionEmphasis(BaseGameObject target)
            {
                if (!(target is ClassicUO.Game.GameObjects.GameObject emphasisTarget) || emphasisTarget.IsDestroyed)
                {
                    return;
                }

                if (!EnsureMarker())
                {
                    return;
                }

                // Same single-effect-at-a-time simplicity as the attack pulse -
                // a fresh long press restarts the emphasis rather than stacking.
                if (selectionEmphasisCoroutine != null)
                {
                    StopCoroutine(selectionEmphasisCoroutine);
                }

                selectionEmphasisCoroutine = StartCoroutine(SelectionEmphasisRoutine());
            }

            private System.Collections.IEnumerator SelectionEmphasisRoutine()
            {
                float elapsed = 0f;

                while (elapsed < SelectionEmphasisDuration)
                {
                    float t = elapsed / SelectionEmphasisDuration;
                    float scale = Mathf.Lerp(SelectionEmphasisPeakScale, 1f, t);
                    markerTransform.localScale = new Vector3(scale, scale, 1f);
                    markerImage.color = Color.Lerp(SelectionEmphasisColor, MarkerColor, t);
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                markerTransform.localScale = Vector3.one;
                markerImage.color = MarkerColor;
                selectionEmphasisCoroutine = null;
            }

            internal void TriggerEmptyTapPing(Vector2 screenPosition)
            {
                if (!EnsureEmptyTapMarker())
                {
                    return;
                }

                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, null, out Vector2 localPoint))
                {
                    emptyTapTransform.anchoredPosition = localPoint;
                }

                if (emptyTapCoroutine != null)
                {
                    StopCoroutine(emptyTapCoroutine);
                }

                emptyTapCoroutine = StartCoroutine(EmptyTapRoutine());
            }

            private System.Collections.IEnumerator EmptyTapRoutine()
            {
                emptyTapObject.SetActive(true);
                float elapsed = 0f;

                // The tap location is static (unlike a moving mobile) - position once
                // above, then just fade in place. No scale change, deliberately: the
                // subtlety itself is what distinguishes this from the selection/attack
                // effects, both of which animate scale.
                while (elapsed < EmptyTapDuration)
                {
                    float t = elapsed / EmptyTapDuration;
                    Color c = EmptyTapColor;
                    c.a = EmptyTapColor.a * (1f - t);
                    emptyTapImage.color = c;
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                emptyTapObject.SetActive(false);
                emptyTapCoroutine = null;
            }

            private bool EnsureEmptyTapMarker()
            {
                if (emptyTapObject != null)
                {
                    return true;
                }

                if (canvasRect == null)
                {
                    canvasRect = Object.FindFirstObjectByType<Canvas>()?.transform as RectTransform;
                    if (canvasRect == null)
                    {
                        return false;
                    }
                }

                emptyTapObject = new GameObject("WorldEmptyTapPing", typeof(RectTransform), typeof(Image));
                emptyTapObject.transform.SetParent(canvasRect, false);

                emptyTapTransform = (RectTransform)emptyTapObject.transform;
                emptyTapTransform.anchorMin = new Vector2(0.5f, 0.5f);
                emptyTapTransform.anchorMax = new Vector2(0.5f, 0.5f);
                emptyTapTransform.pivot = new Vector2(0.5f, 0.5f);
                emptyTapTransform.sizeDelta = new Vector2(EmptyTapDiameter, EmptyTapDiameter);

                emptyTapImage = emptyTapObject.GetComponent<Image>();
                emptyTapImage.sprite = BuildRingSprite();
                emptyTapImage.type = Image.Type.Simple;
                emptyTapImage.raycastTarget = false;

                emptyTapObject.SetActive(false);

                return true;
            }


            private bool EnsurePulse()
            {
                if (pulseObject != null)
                {
                    return true;
                }

                if (canvasRect == null)
                {
                    canvasRect = Object.FindFirstObjectByType<Canvas>()?.transform as RectTransform;
                    if (canvasRect == null)
                    {
                        return false;
                    }
                }

                pulseObject = new GameObject("MobileAttackPulse", typeof(RectTransform), typeof(Image));
                pulseObject.transform.SetParent(canvasRect, false);

                pulseTransform = (RectTransform)pulseObject.transform;
                pulseTransform.anchorMin = new Vector2(0.5f, 0.5f);
                pulseTransform.anchorMax = new Vector2(0.5f, 0.5f);
                pulseTransform.pivot = new Vector2(0.5f, 0.5f);
                pulseTransform.sizeDelta = new Vector2(MarkerDiameter, MarkerDiameter);

                pulseImage = pulseObject.GetComponent<Image>();
                pulseImage.sprite = BuildRingSprite();
                pulseImage.type = Image.Type.Simple;
                pulseImage.raycastTarget = false;

                pulseObject.SetActive(false);

                return true;
            }

            private bool EnsureMarker()
            {
                if (markerObject != null)
                {
                    return true;
                }

                Canvas canvas = Object.FindFirstObjectByType<Canvas>();
                if (canvas == null)
                {
                    return false;
                }

                canvasRect = canvas.transform as RectTransform;

                markerObject = new GameObject("MobileTargetIndicator", typeof(RectTransform), typeof(Image));
                markerObject.transform.SetParent(canvas.transform, false);

                markerTransform = (RectTransform)markerObject.transform;
                markerTransform.anchorMin = new Vector2(0.5f, 0.5f);
                markerTransform.anchorMax = new Vector2(0.5f, 0.5f);
                markerTransform.pivot = new Vector2(0.5f, 0.5f);
                markerTransform.sizeDelta = new Vector2(MarkerDiameter, MarkerDiameter);

                markerImage = markerObject.GetComponent<Image>();
                markerImage.color = MarkerColor;
                markerImage.sprite = BuildRingSprite();
                markerImage.type = Image.Type.Simple;
                // A tracking marker must never steal touch input from the world/HUD below it.
                markerImage.raycastTarget = false;

                BuildInfoPanel();

                return true;
            }

            // Name label (+ optional HP fill) anchored above the ring, as a child of
            // markerTransform - one position update (above) moves the whole group,
            // no separate screen-position math needed for these.
            private void BuildInfoPanel()
            {
                infoObject = new GameObject("MobileTargetInfo", typeof(RectTransform), typeof(Image));
                infoObject.transform.SetParent(markerTransform, false);

                var infoTransform = (RectTransform)infoObject.transform;
                infoTransform.anchorMin = new Vector2(0.5f, 0.5f);
                infoTransform.anchorMax = new Vector2(0.5f, 0.5f);
                infoTransform.pivot = new Vector2(0.5f, 0f);
                infoTransform.sizeDelta = new Vector2(InfoWidth, InfoHeight);
                infoTransform.anchoredPosition = new Vector2(0f, InfoOffsetAboveRing);

                Image background = infoObject.GetComponent<Image>();
                background.color = InfoBackgroundColor;
                background.raycastTarget = false;

                var labelObject = new GameObject("Name", typeof(RectTransform), typeof(Text));
                labelObject.transform.SetParent(infoTransform, false);
                var labelTransform = (RectTransform)labelObject.transform;
                labelTransform.anchorMin = Vector2.zero;
                labelTransform.anchorMax = Vector2.one;
                labelTransform.offsetMin = new Vector2(4f, 2f);
                labelTransform.offsetMax = new Vector2(-4f, -2f);

                nameLabel = labelObject.GetComponent<Text>();
                nameLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                nameLabel.fontSize = 16;
                nameLabel.alignment = TextAnchor.MiddleCenter;
                nameLabel.color = Color.white;
                nameLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
                nameLabel.verticalOverflow = VerticalWrapMode.Truncate;
                nameLabel.raycastTarget = false;

                hpBarObject = new GameObject("HpBar", typeof(RectTransform), typeof(Image));
                hpBarObject.transform.SetParent(infoTransform, false);
                var hpBarTransform = (RectTransform)hpBarObject.transform;
                hpBarTransform.anchorMin = new Vector2(0.5f, 1f);
                hpBarTransform.anchorMax = new Vector2(0.5f, 1f);
                hpBarTransform.pivot = new Vector2(0.5f, 1f);
                hpBarTransform.sizeDelta = new Vector2(InfoWidth - 4f, HpBarHeight);
                hpBarTransform.anchoredPosition = new Vector2(0f, HpBarHeight + 2f);
                hpBarObject.GetComponent<Image>().color = HpBarBackgroundColor;
                hpBarObject.GetComponent<Image>().raycastTarget = false;

                var hpFillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
                hpFillObject.transform.SetParent(hpBarTransform, false);
                hpFillTransform = (RectTransform)hpFillObject.transform;
                hpFillTransform.anchorMin = new Vector2(0f, 0f);
                hpFillTransform.anchorMax = new Vector2(0f, 1f);
                hpFillTransform.pivot = new Vector2(0f, 0.5f);
                hpFillTransform.anchoredPosition = Vector2.zero;
                hpFillTransform.sizeDelta = new Vector2(InfoWidth - 4f, 0f);
                Image hpFillImage = hpFillObject.GetComponent<Image>();
                hpFillImage.color = HpFillColor;
                hpFillImage.raycastTarget = false;
            }

            // Simple procedural ring texture - no art dependency, generated once and
            // reused for every marker instance. A thin hollow ring reads as "this is
            // selected" without covering the entity underneath, per the v1 visual
            // design constraint (obvious, not noisy, doesn't obscure the target).
            private static Sprite ringSpriteCache;

            private static Sprite BuildRingSprite()
            {
                if (ringSpriteCache != null)
                {
                    return ringSpriteCache;
                }

                const int size = 64;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.filterMode = FilterMode.Bilinear;

                float outerRadius = size / 2f;
                float innerRadius = outerRadius - RingThickness * (size / MarkerDiameter);
                Vector2 center = new Vector2(outerRadius, outerRadius);

                var pixels = new Color32[size * size];
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                        bool onRing = dist <= outerRadius && dist >= innerRadius;
                        pixels[y * size + x] = onRing ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
                    }
                }

                texture.SetPixels32(pixels);
                texture.Apply();

                ringSpriteCache = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
                return ringSpriteCache;
            }
        }
    }
}
