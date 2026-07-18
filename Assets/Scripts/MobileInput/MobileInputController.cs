using System.Linq;
using PreferenceEnums;
using UnityEngine;
using ClassicUO.Game.Scenes;

public class MobileInputController : MonoBehaviour
{
    [SerializeField] private FloatingJoystick joystick;
    private CanvasGroup joystickCanvasGroup;

    private int activePointerId = -1;
    private UnityEngine.UI.Image image;

    private void Awake()
    {
        image = GetComponent<UnityEngine.UI.Image>();
    }

    private void Start()
    {
        if (joystick != null)
        {
            joystickCanvasGroup = joystick.GetComponent<CanvasGroup>();
            joystick.Hide();
        }

        InitializeSettings();
    }

    private void OnEnable()
    {
        if (UserPreferences.JoystickOpacity != null)
            UserPreferences.JoystickOpacity.ValueChanged += OnOpacityChanged;
        if (UserPreferences.JoystickDeadZone != null)
            UserPreferences.JoystickDeadZone.ValueChanged += OnDeadZoneChanged;
    }

    private void OnDisable()
    {
        if (UserPreferences.JoystickOpacity != null)
            UserPreferences.JoystickOpacity.ValueChanged -= OnOpacityChanged;
        if (UserPreferences.JoystickDeadZone != null)
            UserPreferences.JoystickDeadZone.ValueChanged -= OnDeadZoneChanged;
    }

    private void InitializeSettings()
    {
        if (UserPreferences.JoystickOpacity != null)
            ApplyOpacity();
        if (UserPreferences.JoystickDeadZone != null)
            ApplyDeadZone();
    }

    private void OnOpacityChanged(int value) => ApplyOpacity();

    private void ApplyOpacity()
    {
        if (joystickCanvasGroup == null)
            return;

        var opacityEnum = (JoystickOpacity)UserPreferences.JoystickOpacity.CurrentValue;
        joystickCanvasGroup.alpha = opacityEnum switch
        {
            JoystickOpacity.VeryLow => 0.3f,
            JoystickOpacity.Low => 0.5f,
            JoystickOpacity.Normal => 0.7f,
            JoystickOpacity.High => 1f,
            _ => 0.7f
        };
    }

    private void OnDeadZoneChanged(int value) => ApplyDeadZone();

    private void ApplyDeadZone()
    {
        if (joystick == null)
            return;

        var deadZoneEnum = (JoystickDeadZone)UserPreferences.JoystickDeadZone.CurrentValue;
        joystick.deadZone = deadZoneEnum switch
        {
            JoystickDeadZone.Low => 0.1f,
            JoystickDeadZone.Medium => 0.2f,
            JoystickDeadZone.High => 0.3f,
            _ => 0.1f
        };
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

        // Once a finger starts controlling the joystick, keep ownership of it
        // for the whole gesture (even if it strays into the right half) so a
        // quick swipe back onto the left half resumes instead of restarting.
        if (activePointerId != -1)
        {
            var ownedFinger = fingers.FirstOrDefault(f => f.Index == activePointerId);
            if (ownedFinger == null || ownedFinger.Up)
            {
                joystick.Hide();
                activePointerId = -1;
                return;
            }

            if (IsPointInLeftHalf(ownedFinger.ScreenPosition))
            {
                var localPos = ScreenToJoystickSpace(ownedFinger.ScreenPosition);
                if (!joystick.gameObject.activeSelf)
                    joystick.Show(localPos);
                joystick.Drag(localPos);
            }
            else
            {
                joystick.Hide();
            }
            return;
        }

        foreach (var finger in fingers)
        {
            if (finger.Down && IsPointInLeftHalf(finger.ScreenPosition))
            {
                activePointerId = finger.Index;
                joystick.Show(ScreenToJoystickSpace(finger.ScreenPosition));
                break;
            }
        }
    }

    private bool IsPointInLeftHalf(Vector2 screenPosition)
    {
        return screenPosition.x < Screen.width * 0.5f;
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
