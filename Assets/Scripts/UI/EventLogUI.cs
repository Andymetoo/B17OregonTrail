using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Oregon Trail style event log that displays recent events.
/// Shows damage, fires, crew injuries, and other important events.
/// </summary>
public class EventLogUI : MonoBehaviour
{
    public static EventLogUI Instance { get; private set; }
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI logText;
    [SerializeField] private int maxLogEntries = 5;
    [SerializeField] private float messageDisplayTime = 8f; // How long each message stays visible
    
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
    
    private void Start()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // NOTE: This EventLogUI now handles ONLY crew actions.
        // Damage/fire/system events are handled by DamageLogUI.
        
        if (CrewManager.Instance != null)
        {
            CrewManager.Instance.OnCrewActionCompleted += OnCrewActionCompleted;
            CrewManager.Instance.OnCrewActionCancelled += OnCrewActionCancelled;
            CrewManager.Instance.OnCrewActionAssigned += OnCrewActionAssigned;
        }
        
        UpdateDisplay();
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
    
    // Public logging API for other systems
    public void Log(string message, Color color)
    {
        logEntries.Enqueue(new LogEntry(message, color));
        Debug.Log($"[EventLog] {message}");
    }

    // Keep private fallback for internal callers
    private void AddMessage(string message, Color color)
    {
        Log(message, color);
    }
    
    private void OnSectionDamaged(PlaneSectionState section)
    {
        // MOVED TO DamageLogUI - keeping this stub for now in case of references
    }
    
    private void OnFireStarted(PlaneSectionState section)
    {
        // MOVED TO DamageLogUI
    }
    
    private void OnFireExtinguished(PlaneSectionState section)
    {
        // MOVED TO DamageLogUI
    }
    
    private void OnSystemStatusChanged(PlaneSystemState system)
    {
        // MOVED TO DamageLogUI
    }
    
    private void OnCrewInjuryChanged(CrewMember crew)
    {
        // MOVED TO DamageLogUI
    }
    
    private void OnCrewDied(CrewMember crew)
    {
        // MOVED TO DamageLogUI
    }
    
    private void OnCrewActionCompleted(CrewMember crew)
    {
        // Only log significant completions to avoid spam
        if (crew.CurrentAction?.Type == ActionType.ExtinguishFire)
        {
            AddMessage($"{crew.Name} finished fighting fire.", Color.green);
        }
        else if (crew.CurrentAction?.Type == ActionType.Repair)
        {
            AddMessage($"{crew.Name} completed repairs.", Color.green);
        }
        else if (crew.CurrentAction?.Type == ActionType.TreatInjury)
        {
            AddMessage($"{crew.Name} finished medical treatment.", Color.green);
        }
    }

    private void OnCrewActionCancelled(CrewMember crew)
    {
        AddMessage($"{crew.Name}'s action was cancelled.", Color.yellow);
    }

    private void OnCrewActionAssigned(CrewMember crew)
    {
        var act = crew.CurrentAction;
        if (act == null) return;
        string msg = act.Type switch
        {
            ActionType.ExtinguishFire => $"{crew.Name} started fighting fire in {act.TargetId}.",
            ActionType.Repair => $"{crew.Name} started repairing {act.TargetId}.",
            ActionType.TreatInjury => $"{crew.Name} started medical treatment on {act.TargetId}.",
            ActionType.Move => $"{crew.Name} is moving to {act.TargetId}.",
            ActionType.ManStation => $"{crew.Name} is manning {act.TargetId}.",
            _ => null
        };
        if (msg != null)
        {
            AddMessage(msg, Color.yellow);
        }
    }
    
    private void UpdateDisplay()
    {
        if (logText == null) return;
        
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
    [ContextMenu("Test Message")]
    private void TestMessage()
    {
        AddMessage("Test event message!", Color.white);
    }
}