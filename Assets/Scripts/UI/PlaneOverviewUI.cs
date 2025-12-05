using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Full-screen overlay that displays a comprehensive overview of all plane systems, crew, and sections.
/// Pauses the game while open. Click anywhere to close.
/// </summary>
public class PlaneOverviewUI : MonoBehaviour
{
    public static PlaneOverviewUI Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject overlayPanel;
    [SerializeField] private TextMeshProUGUI crewText;
    [SerializeField] private TextMeshProUGUI sectionsText;
    [SerializeField] private TextMeshProUGUI systemsText;
    [SerializeField] private TextMeshProUGUI generalText;
    
    [Header("Title Styling")]
    [SerializeField] private int titleSize = 14;
    [SerializeField] private Color titleColor = Color.white;
    
    [Header("Color Coding")]
    [SerializeField] private Color excellentColor = new Color(0.2f, 0.8f, 0.2f); // Green
    [SerializeField] private Color goodColor = new Color(0.6f, 0.8f, 0.2f);      // Yellow-Green
    [SerializeField] private Color fairColor = new Color(0.9f, 0.7f, 0.2f);      // Yellow
    [SerializeField] private Color poorColor = new Color(0.9f, 0.5f, 0.1f);      // Orange
    [SerializeField] private Color criticalColor = new Color(0.9f, 0.2f, 0.2f);  // Red
    [SerializeField] private Color destroyedColor = new Color(0.5f, 0.5f, 0.5f); // Gray
    
    private bool isOpen = false;
    private float previousTimeScale = 1f;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        if (overlayPanel != null)
        {
            overlayPanel.SetActive(false);
        }
    }

    private void Start()
    {
        // No close button needed - click anywhere closes
    }

    private void Update()
    {
        // Close on any click when open
        if (isOpen && Input.GetMouseButtonDown(0))
        {
            CloseOverview();
        }
        
        // Optional: ESC key to toggle
        if (Input.GetKeyDown(KeyCode.Escape) && isOpen)
        {
            CloseOverview();
        }
    }

    /// <summary>
    /// Opens the overview panel, pauses the game, and generates the status report.
    /// </summary>
    public void OpenOverview()
    {
        if (isOpen) return;
        
        isOpen = true;
        
        // Pause the game
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        
        // Show overlay
        if (overlayPanel != null)
        {
            overlayPanel.SetActive(true);
        }
        
        // Generate and display the overview
        GenerateOverview();
        
        Debug.Log("[PlaneOverviewUI] Overview opened - game paused");
    }

    /// <summary>
    /// Closes the overview panel and resumes the game.
    /// </summary>
    public void CloseOverview()
    {
        if (!isOpen) return;
        
        isOpen = false;
        
        // Resume the game
        Time.timeScale = previousTimeScale;
        
        // Hide overlay
        if (overlayPanel != null)
        {
            overlayPanel.SetActive(false);
        }
        
        Debug.Log("[PlaneOverviewUI] Overview closed - game resumed");
    }

    /// <summary>
    /// Generates the full plane status overview text.
    /// </summary>
    private void GenerateOverview()
    {
        string titleColorHex = ColorToHex(titleColor);
        
        // Generate each column separately
        if (crewText != null)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"<size={titleSize}><b><color={titleColorHex}>CREW</color></b></size>");
            sb.AppendLine("────────────");
            GenerateCrewSection(sb);
            crewText.text = sb.ToString();
        }
        
        if (sectionsText != null)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"<size={titleSize}><b><color={titleColorHex}>SECTIONS</color></b></size>");
            sb.AppendLine("────────────");
            GenerateSectionSection(sb);
            sectionsText.text = sb.ToString();
        }
        
        if (systemsText != null)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"<size={titleSize}><b><color={titleColorHex}>SYSTEMS</color></b></size>");
            sb.AppendLine("────────────");
            GenerateSystemSection(sb);
            sb.AppendLine();
            GenerateEngineSection(sb);
            systemsText.text = sb.ToString();
        }
        
        if (generalText != null)
        {
            var sb = new StringBuilder();
            GenerateGeneralSection(sb);
            sb.AppendLine("\n<size=10><i><color=#FFFF88>Click to close</color></i></size>");
            generalText.text = sb.ToString();
        }
    }

    private void GenerateCrewSection(StringBuilder sb)
    {
        if (CrewManager.Instance == null || CrewManager.Instance.AllCrew.Count == 0)
        {
            sb.AppendLine("No crew");
            return;
        }
        
        foreach (var crew in CrewManager.Instance.AllCrew)
        {
            string statusColor = GetCrewHealthColor(crew.Status);
            string nameColor = GetCrewHealthColor(crew.Status);
            
            // Ultra-compact status
            string statusShort = crew.Status switch
            {
                CrewStatus.Healthy => "OK",
                CrewStatus.Light => "LT",
                CrewStatus.Serious => "SR",
                CrewStatus.Critical => "CR",
                CrewStatus.Dead => "XX",
                _ => "??"
            };
            
            // Compact action info
            string actionInfo = "—";
            if (crew.CurrentAction != null)
            {
                string actionShort = crew.CurrentAction.Type switch
                {
                    ActionType.Move => "→",
                    ActionType.Repair => "🔧",
                    ActionType.TreatInjury => "⚕",
                    ActionType.ExtinguishFire => "🔥",
                    ActionType.OccupyStation => "⚑",
                    ActionType.ManStation => "⚑",
                    ActionType.FeatherEngine => "F",
                    ActionType.RestartEngine => "R",
                    _ => "?"
                };
                
                string phaseShort = crew.CurrentAction.Phase switch
                {
                    ActionPhase.MoveToTarget => "→",
                    ActionPhase.Performing => "●",
                    ActionPhase.Returning => "←",
                    _ => ""
                };
                
                actionInfo = $"{phaseShort}{actionShort}";
            }
            
            sb.AppendLine($"<color={nameColor}>{crew.Name}</color> <color={statusColor}>{statusShort}</color> {actionInfo}");
        }
    }

    private void GenerateSectionSection(StringBuilder sb)
    {
        if (PlaneManager.Instance == null)
        {
            sb.AppendLine("No data");
            return;
        }
        
        var sections = PlaneManager.Instance.Sections;
        if (sections == null || sections.Count == 0)
        {
            sb.AppendLine("No sections");
            return;
        }
        
        foreach (var section in sections)
        {
            string integrityColor = GetIntegrityColor(section.Integrity);
            string nameColor = GetIntegrityColor(section.Integrity);
            string fireStatus = section.OnFire ? "🔥" : "";
            string destroyedStatus = section.Integrity <= PlaneManager.Instance.destroyedIntegrityThreshold ? "✗" : "";
            
            sb.AppendLine($"<color={nameColor}>{section.Id}</color> <color={integrityColor}>{section.Integrity}%</color>{fireStatus}{destroyedStatus}");
        }
    }

    private void GenerateSystemSection(StringBuilder sb)
    {
        if (PlaneManager.Instance == null)
        {
            sb.AppendLine("No data");
            return;
        }
        
        var systems = PlaneManager.Instance.Systems;
        if (systems == null || systems.Count == 0)
        {
            sb.AppendLine("No systems");
            return;
        }
        
        // Filter out engines (they have their own section)
        var nonEngineSystems = systems.Where(s => s.Type != SystemType.Engine).ToList();
        if (nonEngineSystems.Count == 0)
        {
            sb.AppendLine("No systems");
            return;
        }
        
        foreach (var system in nonEngineSystems)
        {
            string integrityColor = GetIntegrityColor(system.Integrity);
            string nameColor = GetIntegrityColor(system.Integrity);
            string statusIcon = system.Status == SystemStatus.Operational ? "●" : "○";
            string statusColor = system.Status == SystemStatus.Operational ? "#44FF44" : "#FF4444";
            
            sb.AppendLine($"<color={nameColor}>{system.Id}</color> <color={statusColor}>{statusIcon}</color><color={integrityColor}>{system.Integrity}%</color>");
        }
    }

    private void GenerateEngineSection(StringBuilder sb)
    {
        if (PlaneManager.Instance == null)
        {
            sb.AppendLine("No data");
            return;
        }
        
        var systems = PlaneManager.Instance.Systems;
        if (systems == null || systems.Count == 0)
        {
            sb.AppendLine("No engines");
            return;
        }
        
        // Filter for engines only
        var engines = systems.Where(s => s.Type == SystemType.Engine).ToList();
        if (engines.Count == 0)
        {
            sb.AppendLine("No engines");
            return;
        }
        
        foreach (var engine in engines)
        {
            string integrityColor = GetIntegrityColor(engine.Integrity);
            string nameColor = GetIntegrityColor(engine.Integrity);
            string statusIcon = engine.Status == SystemStatus.Operational ? "●" : "○";
            string statusColor = engine.Status == SystemStatus.Operational ? "#44FF44" : "#FF4444";
            string featheredStatus = engine.IsFeathered ? "F" : "";
            string fireStatus = engine.OnFire ? "🔥" : "";
            
            sb.AppendLine($"<color={nameColor}>{engine.Id}</color> <color={statusColor}>{statusIcon}</color><color={integrityColor}>{engine.Integrity}%</color>{featheredStatus}{fireStatus}");
        }
    }

    private void GenerateGeneralSection(StringBuilder sb)
    {
        if (PlaneManager.Instance == null)
        {
            sb.AppendLine("No data");
            return;
        }
        
        // Horizontal layout: Fuel, Speed, Alt on one line; Supplies on next line
        sb.AppendLine($"Fuel: {PlaneManager.Instance.FuelRemaining:F0} | Speed: {PlaneManager.Instance.CurrentCruiseSpeedMph:F0}mph | Alt: {PlaneManager.Instance.currentAltitudeFeet:F0}ft");
        
        if (SupplyManager.Instance != null)
        {
            sb.AppendLine($"Medkits: {SupplyManager.Instance.Inventory.GetCount(SupplyType.MedKit)} | RepairKits: {SupplyManager.Instance.Inventory.GetCount(SupplyType.RepairKit)} | FireExt: {SupplyManager.Instance.Inventory.GetCount(SupplyType.FireExtinguisher)}");
        }
    }

    /// <summary>
    /// Returns a hex color string based on crew health status.
    /// </summary>
    private string GetCrewHealthColor(CrewStatus status)
    {
        switch (status)
        {
            case CrewStatus.Healthy:
                return ColorToHex(excellentColor);
            case CrewStatus.Light:
                return ColorToHex(fairColor);
            case CrewStatus.Serious:
                return ColorToHex(poorColor);
            case CrewStatus.Critical:
                return ColorToHex(criticalColor);
            case CrewStatus.Dead:
                return ColorToHex(destroyedColor);
            default:
                return "#FFFFFF";
        }
    }

    /// <summary>
    /// Returns a hex color string based on integrity percentage (0-100).
    /// </summary>
    private string GetIntegrityColor(int integrity)
    {
        if (integrity >= 90)
            return ColorToHex(excellentColor);
        else if (integrity >= 70)
            return ColorToHex(goodColor);
        else if (integrity >= 50)
            return ColorToHex(fairColor);
        else if (integrity >= 25)
            return ColorToHex(poorColor);
        else if (integrity > 0)
            return ColorToHex(criticalColor);
        else
            return ColorToHex(destroyedColor);
    }

    /// <summary>
    /// Converts a Unity Color to a hex string for rich text.
    /// </summary>
    private string ColorToHex(Color color)
    {
        int r = Mathf.RoundToInt(color.r * 255);
        int g = Mathf.RoundToInt(color.g * 255);
        int b = Mathf.RoundToInt(color.b * 255);
        return $"#{r:X2}{g:X2}{b:X2}";
    }
}
