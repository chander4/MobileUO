using UnityEngine;

public class FloatingJoystick : MonoBehaviour
{
    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform handle;

    [SerializeField] private float radius = 80f;

    // Consumed by the movement system (ClientRunner). Serialized so it is
    // inspector-editable and settable at runtime, mirroring the surface that
    // MobileJoystick exposed. Values below this magnitude report as no input.
    [SerializeField] public float deadZone;

    public Vector2 Input { get; private set; }

    public void Show(Vector2 anchoredPosition)
    {
        gameObject.SetActive(true);

        background.anchoredPosition = anchoredPosition;
        handle.anchoredPosition = Vector2.zero;

        Input = Vector2.zero;
    }

    public void Hide()
    {
        Input = Vector2.zero;
        handle.anchoredPosition = Vector2.zero;

        gameObject.SetActive(false);
    }

    public void Drag(Vector2 localPosition)
    {
        Vector2 delta = localPosition - background.anchoredPosition;

        delta = Vector2.ClampMagnitude(delta, radius);

        handle.anchoredPosition = delta;

        Vector2 input = delta / radius;
        Input = input.magnitude < deadZone ? Vector2.zero : input;
    }
}