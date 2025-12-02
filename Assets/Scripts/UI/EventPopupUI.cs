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
    private GamePhase? phaseBeforePause;
    private System.Action onContinue;

    private void Awake()
    {
        // Handle singleton
        if (Instance != null && Instance != this)
        {
            bool currentHasMsg = messageText != null;
            bool existingHasMsg = Instance.messageText != null;
            if (currentHasMsg && !existingHasMsg)
            {
                Debug.LogWarning("[EventPopupUI] Switching to instance with assigned messageText; removing previous component.");
                Destroy(Instance.gameObject);
                Instance = this;
            }
            else
            {
                Debug.LogWarning("[EventPopupUI] Duplicate component found; removing this duplicate to preserve UI.");
                Destroy(gameObject);
                return;
            }
        }
        else
        {
            Instance = this;
        }

        // Establish all references BEFORE DontDestroyOnLoad
        if (messageText == null)
        {
            messageText = GetComponentInChildren<TextMeshProUGUI>(includeInactive: true);
            if (messageText == null)
            {
                Debug.LogError("[EventPopupUI] CRITICAL: messageText not found! Assign it in Inspector.");
            }
            else
            {
                Debug.Log($"[EventPopupUI] Auto-wired messageText: {messageText.gameObject.name}");
            }
        }

        if (continueButton == null)
        {
            continueButton = GetComponentInChildren<Button>(includeInactive: true);
        }

        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners(); // Prevent duplicates
            continueButton.onClick.AddListener(OnContinueClicked);
        }
        else
        {
            Debug.LogError("[EventPopupUI] CRITICAL: continueButton not found!");
        }

        // NOW apply DontDestroyOnLoad after everything is wired
        DontDestroyOnLoad(gameObject);

        HideImmediate();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log($"[EventPopupUI Debug] Panel active: {panel.activeSelf}, " +
                      $"SimTicker paused: {SimulationTicker.Instance?.IsPaused}, " +
                      $"wasPausedByPopup: {wasPausedByPopup}");
        }
    }

    public void Show(string message, Color color, bool pause, System.Action onContinueAction = null)
    {
        onContinue = onContinueAction;

        if (messageText == null)
        {
            Debug.LogError("[EventPopupUI] Cannot show popup - messageText is null!");
            return;
        }

        messageText.text = message;
        messageText.color = color;

        if (panel != null)
        {
            panel.SetActive(true);
        }

        Debug.Log($"[EventPopupUI] Show called with pause={pause}, SimTicker.Instance={(SimulationTicker.Instance != null ? "exists" : "NULL")}");

        if (pause)
        {
            wasPausedByPopup = false; // Reset flag
            phaseBeforePause = null;

            // Pause SimulationTicker AGGRESSIVELY
            if (SimulationTicker.Instance != null)
            {
                Debug.Log($"[EventPopupUI] BEFORE pause - SimTicker.isPaused = {SimulationTicker.Instance.isPaused}");
                
                // Set directly AND call Pause() to ensure it sticks
                SimulationTicker.Instance.isPaused = true;
                SimulationTicker.Instance.Pause();
                wasPausedByPopup = true;
                
                Debug.Log($"[EventPopupUI] AFTER pause - SimTicker.isPaused = {SimulationTicker.Instance.isPaused}");
            }
            else
            {
                Debug.LogError("[EventPopupUI] SimulationTicker.Instance is NULL! Cannot pause.");
            }

            // Also pause GameStateManager if it exists
            if (GameStateManager.Instance != null)
            {
                if (!GameStateManager.Instance.IsPaused)
                {
                    phaseBeforePause = GameStateManager.Instance.CurrentPhase;
                    GameStateManager.Instance.PauseGame();
                    Debug.Log("[EventPopupUI] Paused GameStateManager");
                }
            }
        }
    }

    public void OnContinueClicked()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }

        // Invoke callback first
        onContinue?.Invoke();
        onContinue = null;

        // Only resume if WE paused it
        if (wasPausedByPopup)
        {
            // Resume SimulationTicker
            if (SimulationTicker.Instance != null)
            {
                SimulationTicker.Instance.Resume();
                Debug.Log("[EventPopupUI] Resumed SimulationTicker");
            }

            // Resume GameStateManager
            if (GameStateManager.Instance != null)
            {
                var resumePhase = phaseBeforePause ?? GamePhase.Cruise;
                GameStateManager.Instance.ResumeGame(resumePhase);
                Debug.Log("[EventPopupUI] Resumed GameStateManager");
            }

            wasPausedByPopup = false;
            phaseBeforePause = null;
        }
    }

    private void HideImmediate()
    {
        if (panel != null) panel.SetActive(false);
        wasPausedByPopup = false;
        phaseBeforePause = null;
    }

    // Overload for future GameEvent usage
    public void Show(GameEvent evt, bool pause)
    {
        if (evt == null)
        {
            Show("(Null Event)", Color.white, pause);
            return;
        }
        
        // Apply effects first to generate outcomes
        evt.ApplyEffects();
        
        // Build message: flavor text + outcomes
        string message = evt.Title + "\n" + evt.Description;
        
        if (evt.LastOutcomes != null && evt.LastOutcomes.Count > 0)
        {
            message += "\n\n" + string.Join("\n", evt.LastOutcomes);
        }
        
        Show(message, evt.DisplayColor, pause);
    }
}