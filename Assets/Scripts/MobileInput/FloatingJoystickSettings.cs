using PreferenceEnums;
using UnityEngine;

public class FloatingJoystickSettings : MonoBehaviour
{
    [SerializeField] private FloatingJoystick floatingJoystick;
    [SerializeField] private CanvasGroup canvasGroup;

    private void Start()
    {
        UserPreferences.JoystickOpacity.ValueChanged += OnOpacityChanged;
        UserPreferences.JoystickDeadZone.ValueChanged += OnDeadZoneChanged;
        UserPreferences.JoystickMode.ValueChanged += OnJoystickModeChanged;
        UserPreferences.LeftHandedMode.ValueChanged += OnLeftHandedModeChanged;

        ApplyAllSettings();
    }

    private void OnDestroy()
    {
        if (UserPreferences.JoystickOpacity != null)
            UserPreferences.JoystickOpacity.ValueChanged -= OnOpacityChanged;
        if (UserPreferences.JoystickDeadZone != null)
            UserPreferences.JoystickDeadZone.ValueChanged -= OnDeadZoneChanged;
        if (UserPreferences.JoystickMode != null)
            UserPreferences.JoystickMode.ValueChanged -= OnJoystickModeChanged;
        if (UserPreferences.LeftHandedMode != null)
            UserPreferences.LeftHandedMode.ValueChanged -= OnLeftHandedModeChanged;
    }

    private void ApplyAllSettings()
    {
        ApplyOpacity();
        ApplyDeadZone();
        ApplyJoystickMode();
        ApplyLeftHandedMode();
    }

    private void OnOpacityChanged(int value)
    {
        ApplyOpacity();
    }

    private void ApplyOpacity()
    {
        if (canvasGroup == null || floatingJoystick == null)
            return;

        var opacityEnum = (JoystickOpacity)UserPreferences.JoystickOpacity.CurrentValue;
        canvasGroup.alpha = opacityEnum switch
        {
            JoystickOpacity.VeryLow => 0.3f,
            JoystickOpacity.Low => 0.5f,
            JoystickOpacity.Normal => 0.7f,
            JoystickOpacity.High => 1f,
            _ => 0.7f
        };
    }

    private void OnDeadZoneChanged(int value)
    {
        ApplyDeadZone();
    }

    private void ApplyDeadZone()
    {
        if (floatingJoystick == null)
            return;

        var deadZoneEnum = (JoystickDeadZone)UserPreferences.JoystickDeadZone.CurrentValue;
        floatingJoystick.deadZone = deadZoneEnum switch
        {
            JoystickDeadZone.Low => 0.1f,
            JoystickDeadZone.Medium => 0.2f,
            JoystickDeadZone.High => 0.3f,
            _ => 0.1f
        };
    }

    private void OnJoystickModeChanged(int value)
    {
        ApplyJoystickMode();
    }

    private void ApplyJoystickMode()
    {
        if (floatingJoystick == null)
            return;

        var modeEnum = (JoystickMode)UserPreferences.JoystickMode.CurrentValue;
        // Floating is always dynamic - it appears where you touch
        // Fixed would keep it in a fixed position, but we'll handle that later
        // For now, we're just enabling/disabling the floating joystick
        floatingJoystick.gameObject.SetActive(modeEnum == JoystickMode.Floating);
    }

    private void OnLeftHandedModeChanged(int value)
    {
        ApplyLeftHandedMode();
    }

    private void ApplyLeftHandedMode()
    {
        if (floatingJoystick == null)
            return;

        var leftHandedEnum = (LeftHandedMode)UserPreferences.LeftHandedMode.CurrentValue;
        // Left-handed mode would mirror the joystick position
        // This is a placeholder for future implementation
        // For now, we just track the setting
    }
}
