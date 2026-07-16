using UnityEngine;
using ClassicUO.Game.Scenes;

public class MobileInputController : MonoBehaviour
{
    [SerializeField] private FloatingJoystick joystick;

    private int activePointerId = -1;
    private UnityEngine.UI.Image image;

    private void Awake()
    {
        image = GetComponent<UnityEngine.UI.Image>();
    }

    private void Update()
    {
        if (image != null)
        {
            image.raycastTarget = false;
        }

        HandleTouchInput();
    }

    private void HandleTouchInput()
    {
        var fingers = Lean.Touch.LeanTouch.GetFingers(true, false);

        if (fingers.Count == 0)
        {
            if (activePointerId != -1)
            {
                joystick.Hide();
                activePointerId = -1;
            }
            return;
        }

        var finger = fingers[0];

        if (finger.Down)
        {
            if (activePointerId == -1)
            {
                activePointerId = finger.Index;
                var localPos = ScreenToJoystickSpace(finger.ScreenPosition);
                joystick.Show(localPos);
            }
        }

        if (activePointerId == finger.Index && finger.Set)
        {
            var localPos = ScreenToJoystickSpace(finger.ScreenPosition);
            joystick.Drag(localPos);
        }

        if (finger.Up && activePointerId == finger.Index)
        {
            joystick.Hide();
            activePointerId = -1;
        }
    }

    private Vector2 ScreenToJoystickSpace(Vector2 screenPosition)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            joystick.transform.parent as RectTransform,
            screenPosition,
            null,
            out var localPoint);

        return localPoint;
    }
}
