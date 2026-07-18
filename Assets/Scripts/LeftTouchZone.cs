using UnityEngine;
using UnityEngine.UI;
using ClassicUO.Game.Scenes;
using UnityEngine.EventSystems;

public class LeftTouchZone : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private RectTransform joystick;

    private Image image;

    private void Awake()
    {
        image = GetComponent<Image>();
    }

    private void Update()
    {
        bool inGame = ClassicUO.Client.Game != null && ClassicUO.Client.Game.Scene is GameScene;

        image.raycastTarget = inGame;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Vector2 localPoint;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            joystick.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint);

        joystick.anchoredPosition = localPoint;

        Debug.Log($"Joystick moved to {localPoint}");
    }
}