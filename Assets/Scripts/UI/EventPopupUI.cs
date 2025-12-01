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
            // Prefer the instance that has messageText assigned
            bool currentHasMsg = messageText != null;
            bool existingHasMsg = Instance.messageText != null;
            if (currentHasMsg && !existingHasMsg)
            {
                Debug.LogWarning("[EventPopupUI] Switching to instance with assigned messageText; removing previous component.");
                Destroy(Instance);
                Instance = this;
            }
            else
            {
                // Keep existing; remove this duplicate component only
                Debug.LogWarning("[EventPopupUI] Duplicate component found; removing this duplicate to preserve UI.");
                Destroy(this);
                return;
            }
        }
        else
        {
            Instance = this;
        }
        DontDestroyOnLoad(gameObject);

        // Auto-wire messageText if missing
        if (messageText == null)
        {
            messageText = GetComponentInChildren<TextMeshProUGUI>(includeInactive: true);
            if (messageText == null)
            {
                Debug.LogWarning("[EventPopupUI] messageText not assigned and not found in children.");
            }
        }

        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueClicked);
        }

        HideImmediate();
    }

    public void Show(string message, Color color, bool pause, System.Action onContinueAction = null)
    {
        onContinue = onContinueAction;
        if (messageText == null)
        {
            Debug.LogWarning("[EventPopupUI] messageText not assigned.");
        }
        else
        {
            messageText.text = message;
            messageText.color = color;
        }
        if (panel != null)
        {
            panel.SetActive(true);
        }

        if (pause)
        {
            // Always pause SimulationTicker to guarantee freeze
            if (SimulationTicker.Instance != null && !SimulationTicker.Instance.IsPaused)
            {
                SimulationTicker.Instance.Pause();
                wasPausedByPopup = true;
            }
            // Also pause via GameStateManager if available
            if (GameStateManager.Instance != null && !GameStateManager.Instance.IsPaused)
            {
                GameStateManager.Instance.PauseGame();
                wasPausedByPopup = true;
            }
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
        if (wasPausedByPopup)
        {
            // Resume SimulationTicker first
            if (SimulationTicker.Instance != null)
            {
                SimulationTicker.Instance.Resume();
            }
            // Then resume GameStateManager if present
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.ResumeGame(GamePhase.Cruise);
            }
        }
        wasPausedByPopup = false;
    }

    private void HideImmediate()
    {
        if (panel != null) panel.SetActive(false);
        wasPausedByPopup = false;
    }

    // Overload for future GameEvent usage
    public void Show(GameEvent evt, bool pause)
    {
        if (evt == null)
        {
            Show("(Null Event)", Color.white, pause);
            return;
        }
        Show(evt.Title + "\n" + evt.Description, evt.DisplayColor, pause, () => evt.ApplyEffects());
    }
}
