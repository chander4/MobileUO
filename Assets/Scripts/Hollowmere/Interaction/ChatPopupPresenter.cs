using UnityEngine;
using UnityEngine.UI;
using ClassicUO.Game;

namespace Hollowmere.Interaction
{
    /// <summary>
    /// Small standalone chat popup - deliberately bypasses SystemChatControl's
    /// always-visible bottom strip entirely rather than reusing it: that strip is a
    /// full-width easy mis-tap target on touch, and its IsActive flag is a one-way
    /// "show" (see QuickChatBarBootstrap's old "Chat" button), not a real toggle.
    /// This popup opens on demand, closes on demand, and sends via the exact same
    /// GameActions.Say the built-in box uses - same network path, different (and
    /// touch-sized) presentation.
    ///
    /// Same Awake-on-inactive-GameObject pitfall as ActionPickerPresenter: this
    /// component's own GameObject must stay active - panelRoot is the thing that
    /// starts hidden, never this component's own object.
    /// </summary>
    internal class ChatPopupPresenter : MonoBehaviour
    {
        internal static ChatPopupPresenter Instance { get; private set; }

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private InputField chatInput;
        [SerializeField] private Button sendButton;
        [SerializeField] private Button closeButton;

        private void Awake()
        {
            Instance = this;

            if (sendButton != null)
            {
                sendButton.onClick.AddListener(Send);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
            }

            if (chatInput != null)
            {
                chatInput.onSubmit.AddListener(_ => Send());
            }

            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        public void Open()
        {
            // Both popups are top-center-anchored on a small mobile canvas and
            // genuinely overlap in screen space - two open at once means whichever
            // renders later (this one, since it's a later sibling under Ui) visually
            // covers and steals taps from the other. Simplest correct fix: only one
            // floating popup on screen at a time.
            ActionPickerPresenter.Instance?.Close();

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }

            if (chatInput != null)
            {
                chatInput.SetTextWithoutNotify(string.Empty);
                chatInput.ActivateInputField();
            }
        }

        public void Close()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        private void Send()
        {
            if (chatInput == null || string.IsNullOrWhiteSpace(chatInput.text))
            {
                return;
            }

            GameActions.Say(chatInput.text);
            chatInput.SetTextWithoutNotify(string.Empty);
            chatInput.ActivateInputField();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
