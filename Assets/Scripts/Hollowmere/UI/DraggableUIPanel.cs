using UnityEngine;
using UnityEngine.EventSystems;

namespace Hollowmere.UI
{
    /// <summary>
    /// Drag-to-move for a Unity UI panel's RectTransform - Gump-side floating windows
    /// (ActionBar, etc.) get this for free via CanMove; plain Unity UI popups
    /// (ChatPopupPresenter's ChatPopup) don't have an equivalent, so this is that.
    /// Add to whatever raycastable area should act as the drag handle - a click/tap
    /// starting on a child with its own raycast target (a button, the input field)
    /// is routed to that child instead, same as any other UI overlap.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class DraggableUIPanel : MonoBehaviour, IDragHandler
    {
        [SerializeField] private RectTransform target;

        private void Awake()
        {
            if (target == null)
            {
                target = GetComponent<RectTransform>();
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (target == null)
            {
                return;
            }

            // Divide out the Canvas Scaler's scale factor so drag speed matches finger
            // movement 1:1 regardless of resolution/DPI scaling - eventData.delta is in
            // real screen pixels, anchoredPosition is in canvas units.
            Canvas canvas = target.GetComponentInParent<Canvas>();
            float scaleFactor = canvas != null && canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;

            target.anchoredPosition += eventData.delta / scaleFactor;
            ClampToParent();
        }

        // Without this, dragging past the edge leaves the panel stuck off-screen with
        // no way back except relogin (its position isn't persisted the way ActionBar's
        // is). Assumes a single-point anchor (anchorMin == anchorMax), true for both
        // current consumers (ActionPicker, ChatPopup) - a stretched-anchor panel would
        // need different math, not something either of these does.
        private void ClampToParent()
        {
            if (!(target.parent is RectTransform parent))
            {
                return;
            }

            Vector2 parentSize = parent.rect.size;
            Vector2 size = target.rect.size;
            Vector2 pivot = target.pivot;

            Vector2 anchorPoint = new Vector2(
                Mathf.Lerp(-parentSize.x * 0.5f, parentSize.x * 0.5f, target.anchorMin.x),
                Mathf.Lerp(-parentSize.y * 0.5f, parentSize.y * 0.5f, target.anchorMin.y));

            float minX = -parentSize.x * 0.5f - anchorPoint.x + size.x * pivot.x;
            float maxX = parentSize.x * 0.5f - anchorPoint.x - size.x * (1f - pivot.x);
            float minY = -parentSize.y * 0.5f - anchorPoint.y + size.y * pivot.y;
            float maxY = parentSize.y * 0.5f - anchorPoint.y - size.y * (1f - pivot.y);

            Vector2 pos = target.anchoredPosition;
            pos.x = Mathf.Clamp(pos.x, Mathf.Min(minX, maxX), Mathf.Max(minX, maxX));
            pos.y = Mathf.Clamp(pos.y, Mathf.Min(minY, maxY), Mathf.Max(minY, maxY));
            target.anchoredPosition = pos;
        }
    }
}
