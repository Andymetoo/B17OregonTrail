using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Oregon Trail style event log that displays recent events.
/// Shows damage, fires, crew injuries, and other important events.
/// </summary>
public class EventLogUI : MonoBehaviour
{
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
        // Subscribe to various event sources
        if (PlaneManager.Instance != null)
        {
            PlaneManager.Instance.OnSectionDamaged += OnSectionDamaged;
            PlaneManager.Instance.OnFireStarted += OnFireStarted;
            PlaneManager.Instance.OnFireExtinguished += OnFireExtinguished;
            PlaneManager.Instance.OnSystemStatusChanged += OnSystemStatusChanged;
        }
        
        if (CrewManager.Instance != null)
        {
            CrewManager.Instance.OnCrewInjuryStageChanged += OnCrewInjuryChanged;
            CrewManager.Instance.OnCrewDied += OnCrewDied;
            CrewManager.Instance.OnCrewActionCompleted += OnCrewActionCompleted;
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
    
    private void AddMessage(string message, Color color)
    {
        logEntries.Enqueue(new LogEntry(message, color));
        Debug.Log($"[EventLog] {message}");
    }
    
    private void OnSectionDamaged(PlaneSectionState section)
    {
        if (section.Integrity <= 0)
        {
            AddMessage($"The {section.Id} is destroyed!", Color.red);
        }
        else if (section.Integrity < 30)
        {
            AddMessage($"The {section.Id} is heavily damaged!", Color.red);
        }
    }
    
    private void OnFireStarted(PlaneSectionState section)
    {
        AddMessage($"Fire breaks out in the {section.Id}!", Color.red);
    }
    
    private void OnFireExtinguished(PlaneSectionState section)
    {
        AddMessage($"Fire in the {section.Id} has been extinguished.", Color.green);
    }
    
    private void OnSystemStatusChanged(PlaneSystemState system)
    {
        if (system.Status == SystemStatus.Destroyed)
        {
            AddMessage($"{system.Id} system is destroyed!", Color.red);
        }
        else if (system.Status == SystemStatus.Damaged)
        {
            AddMessage($"{system.Id} system is damaged.", Color.yellow);
        }
        else if (system.Status == SystemStatus.Operational)
        {
            AddMessage($"{system.Id} system repaired.", Color.green);
        }
    }
    
    private void OnCrewInjuryChanged(CrewMember crew)
    {
        string statusMessage = crew.Status switch
        {
            CrewStatus.Light => $"{crew.Name} is lightly wounded.",
            CrewStatus.Serious => $"{crew.Name} is seriously wounded!",
            CrewStatus.Critical => $"{crew.Name} is critically injured!",
            CrewStatus.Healthy => $"{crew.Name} has recovered.",
            _ => null
        };
        
        if (statusMessage != null)
        {
            Color color = crew.Status == CrewStatus.Healthy ? Color.green : Color.red;
            AddMessage(statusMessage, color);
        }
    }
    
    private void OnCrewDied(CrewMember crew)
    {
        AddMessage($"{crew.Name} has died.", Color.red);
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