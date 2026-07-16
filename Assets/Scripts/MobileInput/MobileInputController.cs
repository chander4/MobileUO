using UnityEngine;
using UnityEngine.EventSystems;

public class MobileInputController :
    MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IPointerUpHandler
{
    [SerializeField] private FloatingJoystick joystick;

    public void OnPointerDown(PointerEventData eventData)
    {
        joystick.Show(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        joystick.Drag(eventData.position);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        joystick.Hide();
    }
}