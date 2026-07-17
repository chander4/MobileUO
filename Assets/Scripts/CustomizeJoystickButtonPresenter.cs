using UnityEngine;
using UnityEngine.UI;

public class CustomizeJoystickButtonPresenter : MonoBehaviour
{
    [SerializeField] private Button button;

    private void Awake()
    {
        button.onClick.AddListener(OnButtonClicked);
    }

    private void OnButtonClicked()
    {
    }
}
