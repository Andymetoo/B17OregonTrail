using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Oregon Trail style damage log that ONLY displays damage, fires, and system failures.
/// Separate from crew action logging to avoid visual clutter.
/// </summary>
public class DamageLogUI : MonoBehaviour
{
    public static DamageLogUI Instance { get; private set; }
    
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI logText;
    [SerializeField] private int maxLogEntries = 8;
    [SerializeField] private float messageDisplayTime = 12f; // Longer for important damage events

    [Header("Popup Triggers")]
    [Tooltip("Show popup when a section starts burning.")] public bool popupOnFireStart = true;
    [Tooltip("Show popup when a section is destroyed.")] public bool popupOnSectionDestroyed = true;
    [Tooltip("Show popup on crew death.")] public bool popupOnCrewDeath = true;
    [Tooltip("NEVER show popup for regular damage - use EventLogUI stacking text only.")] public bool popupOnSectionDamage = false;
    
    private Queue<LogEntry> logEntries = new Queue<LogEntry>();
    
    private struct LogEntry
    {
        public string message;
        public float timeAdded;
        public Color color;
        
        public LogEntry(string message, Color color)
        {
            this.message = message;
            this.timeAdded = Time.time;
            this.color = color;
        }
    }
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple DamageLogUI instances found! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Debug.Log("[DamageLogUI] Instance created and ready.");
    }
    
    private void Start()
    {
        // Validate UI reference
        if (logText == null)
        {
            Debug.LogError("[DamageLogUI] logText is NULL! Assign TextMeshProUGUI in Inspector!");
        }
        else
        {
            Debug.Log($"[DamageLogUI] logText found: {logText.name}");
        }
        
        // Subscribe to PlaneManager damage events ONLY
        if (PlaneManager.Instance != null)
        {
            Debug.Log("[DamageLogUI] Subscribing to PlaneManager events...");
            PlaneManager.Instance.OnSectionDamaged += OnSectionDamaged;
            PlaneManager.Instance.OnFireStarted += OnFireStarted;
            PlaneManager.Instance.OnFireExtinguished += OnFireExtinguished;
            PlaneManager.Instance.OnSystemStatusChanged += OnSystemStatusChanged;
            PlaneManager.Instance.OnSectionDestroyed += OnSectionDestroyed;
        }
        else
        {
            Debug.LogError("[DamageLogUI] PlaneManager.Instance is NULL! Cannot subscribe to events.");
        }
        
        // Subscribe to crew deaths (critical event)
        if (CrewManager.Instance != null)
        {
            CrewManager.Instance.OnCrewDied += OnCrewDied;
            CrewManager.Instance.OnCrewInjuryStageChanged += OnCrewInjuryChanged;
        }
        
        UpdateDisplay();
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (PlaneManager.Instance != null)
        {
            PlaneManager.Instance.OnSectionDamaged -= OnSectionDamaged;
            PlaneManager.Instance.OnFireStarted -= OnFireStarted;
            PlaneManager.Instance.OnFireExtinguished -= OnFireExtinguished;
            PlaneManager.Instance.OnSystemStatusChanged -= OnSystemStatusChanged;
            PlaneManager.Instance.OnSectionDestroyed -= OnSectionDestroyed;
        }
        
        if (CrewManager.Instance != null)
        {
            CrewManager.Instance.OnCrewDied -= OnCrewDied;
            CrewManager.Instance.OnCrewInjuryStageChanged -= OnCrewInjuryChanged;
        }
    }
    
    private void Update()
    {
        // Remove old entries
        while (logEntries.Count > 0)
        {
            var oldest = logEntries.Peek();
            if (Time.time - oldest.timeAdded > messageDisplayTime)
            {
                logEntries.Dequeue();
            }
            else
            {
                break;
            }
        }
        
        // Keep entry count reasonable
        while (logEntries.Count > maxLogEntries)
        {
            logEntries.Dequeue();
        }
        
        UpdateDisplay();
    }
    
    /// <summary>
    /// Add a damage-related message to the log
    /// </summary>
    private void AddMessage(string message, Color color)
    {
        logEntries.Enqueue(new LogEntry(message, color));
        Debug.Log($"[DamageLogUI] Added message (queue size: {logEntries.Count}): {message}");
        UpdateDisplay(); // Force immediate update
    }
    
    /// <summary>
    /// Public API for manual logging (e.g., from ChaosSimulator)
    /// </summary>
    public void Log(string message, Color color)
    {
        AddMessage(message, color);
    }
    
    private void OnSectionDamaged(PlaneSectionState section)
    {
        Debug.Log($"[DamageLogUI] OnSectionDamaged called for {section.Id} (Integrity: {section.Integrity})");
        
        // Use EventLogUI for stacking text display - no toast popup
        // Color-coded by severity
        if (section.Integrity <= 0)
        {
            AddMessage($"The {section.Id} is DESTROYED!", Color.red);
        }
        else if (section.Integrity < 30)
        {
            AddMessage($"The {section.Id} is heavily damaged! ({section.Integrity})", Color.red);
        }
        else if (section.Integrity < 60)
        {
            AddMessage($"The {section.Id} damaged. ({section.Integrity})", new Color(1f, 0.5f, 0f)); // orange
        }
        else
        {
            AddMessage($"{section.Id} hit. ({section.Integrity})", new Color(1f, 0.8f, 0f)); // amber
        }
        
        // No toast popup for regular damage - only EventLogUI stacking text
    }
    
    private void OnSectionDestroyed(PlaneSectionState section)
    {
        Debug.Log($"[DamageLogUI] OnSectionDestroyed called for {section.Id}");
        AddMessage($"CRITICAL: The {section.Id} has been destroyed!", Color.red);
        if (popupOnSectionDestroyed)
        {
            EventPopupUI.Instance?.Show($"{section.Id} destroyed!", Color.red, pause:false);
        }
    }
    
    private void OnFireStarted(PlaneSectionState section)
    {
        Debug.Log($"[DamageLogUI] OnFireStarted called for {section.Id}");
        AddMessage($"FIRE breaks out in the {section.Id}!", new Color(1f, 0.3f, 0f)); // bright red-orange
        if (popupOnFireStart)
        {
            EventPopupUI.Instance?.Show($"Fire in {section.Id}!", new Color(1f,0.3f,0f), pause:true);
        }
    }
    
    private void OnFireExtinguished(PlaneSectionState section)
    {
        Debug.Log($"[DamageLogUI] OnFireExtinguished called for {section.Id}");
        AddMessage($"Fire in the {section.Id} has been extinguished.", Color.green);
    }
    
    private void OnSystemStatusChanged(PlaneSystemState system)
    {
        Debug.Log($"[DamageLogUI] OnSystemStatusChanged called for {system.Id} (Status: {system.Status})");
        
        if (system.Status == SystemStatus.Destroyed)
        {
            AddMessage($"{system.Id} system is DESTROYED!", Color.red);
        }
        else if (system.Status == SystemStatus.Damaged)
        {
            AddMessage($"{system.Id} system is damaged.", new Color(1f, 0.65f, 0f)); // orange
        }
        else if (system.Status == SystemStatus.Operational)
        {
            AddMessage($"{system.Id} system operational.", Color.green);
        }
    }
    
    private void OnCrewInjuryChanged(CrewMember crew)
    {
        string statusMessage = crew.Status switch
        {
            CrewStatus.Light => $"{crew.Name} is lightly wounded.",
            CrewStatus.Serious => $"{crew.Name} is seriously wounded!",
            CrewStatus.Critical => $"{crew.Name} is CRITICALLY injured!",
            _ => null
        };
        
        if (statusMessage != null)
        {
            Debug.Log($"[DamageLogUI] Crew injury: {statusMessage}");
            AddMessage(statusMessage, Color.red);
            // Crew injuries now handled by EventLogUI stacking text only - no toast popups
        }
    }
    
    private void OnCrewDied(CrewMember crew)
    {
        Debug.Log($"[DamageLogUI] Crew died: {crew.Name}");
        AddMessage($"CASUALTY: {crew.Name} has died.", new Color(0.5f, 0f, 0f)); // dark red
        if (popupOnCrewDeath)
        {
            EventPopupUI.Instance?.Show($"{crew.Name} has died.", new Color(0.5f,0f,0f), pause:true);
        }
    }
    
    private void UpdateDisplay()
    {
        if (logText == null)
        {
            Debug.LogWarning("[DamageLogUI] logText is NULL! Cannot update display.");
            return;
        }
        
        string displayText = "";
        
        foreach (var entry in logEntries)
        {
            // Add color tags for Unity's rich text
            string colorHex = ColorUtility.ToHtmlStringRGB(entry.color);
            displayText += $"<color=#{colorHex}>{entry.message}</color>\n";
        }
        
        logText.text = displayText.TrimEnd();
    }
    
    /// <summary>
    /// For testing - add a manual message
    /// </summary>
    [ContextMenu("Test Damage Message")]
    private void TestMessage()
    {
        AddMessage("TEST: The tail section is damaged! (Integrity: 45)", Color.yellow);
    }
}
