using UnityEngine;
using UnityEngine.EventSystems;

public class MobileInputController :
    MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IPointerUpHandler
{
    [SerializeField] private FloatingJoystick joystick;

    private bool hasActivePointer;
    private int activePointerId;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (hasActivePointer)
        {
            return;
        }

        hasActivePointer = true;
        activePointerId = eventData.pointerId;

        joystick.Show(ScreenToJoystickSpace(eventData));
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!hasActivePointer || eventData.pointerId != activePointerId)
        {
            return;
        }

        joystick.Drag(ScreenToJoystickSpace(eventData));
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!hasActivePointer || eventData.pointerId != activePointerId)
        {
            return;
        }

        hasActivePointer = false;
        joystick.Hide();
    }

    private Vector2 ScreenToJoystickSpace(PointerEventData eventData)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            joystick.transform.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out var localPoint);

        return localPoint;
    }
}
