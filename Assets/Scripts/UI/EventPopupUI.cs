using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Simple modal popup that pauses the game until the player clicks Continue.
/// Used for Oregon Trail-style event notifications.
/// </summary>
    public class EventPopupUI : MonoBehaviour
{
    public static EventPopupUI Instance { get; private set; }

    [Header("UI Refs")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button continueButton;

    private bool wasPausedByPopup = false;
    private System.Action onContinue;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueClicked);
        }

        HideImmediate();
    }

    public void Show(string message, Color color, bool pause, System.Action onContinueAction = null)
    {
        onContinue = onContinueAction;
        if (messageText != null)
        {
            messageText.text = message;
            messageText.color = color;
        }
        if (panel != null)
        {
            panel.SetActive(true);
        }

        if (pause && GameStateManager.Instance != null && !GameStateManager.Instance.IsPaused)
        {
            GameStateManager.Instance.PauseGame();
            wasPausedByPopup = true;
        }
    }

    public void OnContinueClicked()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }
        onContinue?.Invoke();
        onContinue = null;
        if (wasPausedByPopup && GameStateManager.Instance != null)
        {
            // Resume to Cruise by default; callers can change phase in onContinue
            GameStateManager.Instance.ResumeGame(GamePhase.Cruise);
        }
        wasPausedByPopup = false;
    }

    private void HideImmediate()
    {
        if (panel != null) panel.SetActive(false);
        wasPausedByPopup = false;
    }
}
