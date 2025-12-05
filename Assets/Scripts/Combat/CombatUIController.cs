using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls the UI during combat mode (3D first-person gunner view).
/// Manages station switching buttons, return-to-main button, and combat HUD.
/// </summary>
public class CombatUIController : MonoBehaviour
{
    public static CombatUIController Instance { get; private set; }

    [Header("Station Info")]
    [Tooltip("Text showing current station name.")]
    [SerializeField] private TextMeshProUGUI stationNameText;
    [Tooltip("Text showing current crew member name at this station.")]
    [SerializeField] private TextMeshProUGUI crewNameText;

    [Header("Station Switching")]
    [Tooltip("Container for station switch buttons (will be auto-populated).")]
    [SerializeField] private Transform stationButtonContainer;
    [Tooltip("Button prefab for station switching.")]
    [SerializeField] private GameObject stationButtonPrefab;

    [Header("Navigation")]
    [Tooltip("Button to return to main UI mode.")]
    [SerializeField] private Button returnToMainButton;

    [Header("Combat HUD")]
    [Tooltip("Text showing ammo count (TODO).")]
    [SerializeField] private TextMeshProUGUI ammoText;
    [Tooltip("Text showing enemy count (TODO).")]
    [SerializeField] private TextMeshProUGUI enemyCountText;

    [Header("Event Log")]
    [Tooltip("Optional reference to event log for combat messages.")]
    [SerializeField] private TextMeshProUGUI combatLogText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Wire up return button
        if (returnToMainButton != null)
        {
            returnToMainButton.onClick.AddListener(OnReturnToMainClicked);
        }

        // Subscribe to mode changes
        if (GameModeManager.Instance != null)
        {
            GameModeManager.Instance.OnModeChanged += OnModeChanged;
        }

        // Populate station buttons
        PopulateStationButtons();
    }

    private void OnDestroy()
    {
        if (GameModeManager.Instance != null)
        {
            GameModeManager.Instance.OnModeChanged -= OnModeChanged;
        }
    }

    private void OnModeChanged(GameMode newMode)
    {
        if (newMode == GameMode.CombatMode)
        {
            RefreshUI();
        }
    }

    /// <summary>
    /// Refresh all UI elements based on current station and crew.
    /// </summary>
    public void RefreshUI()
    {
        if (GameModeManager.Instance == null) return;

        GunnerStation currentStation = GameModeManager.Instance.CurrentStation;
        
        // Update station name
        if (stationNameText != null)
        {
            stationNameText.text = GetStationDisplayName(currentStation);
        }

        // Update crew name
        if (crewNameText != null)
        {
            string crewName = GetCrewNameForStation(currentStation);
            crewNameText.text = string.IsNullOrEmpty(crewName) ? "No Crew" : crewName;
        }

        // Refresh station buttons availability
        RefreshStationButtons();

        // TODO: Update ammo, enemy count, etc.
    }

    private void PopulateStationButtons()
    {
        if (stationButtonContainer == null || stationButtonPrefab == null) return;

        // Clear existing buttons
        foreach (Transform child in stationButtonContainer)
        {
            Destroy(child.gameObject);
        }

        // Create button for each station
        foreach (GunnerStation station in System.Enum.GetValues(typeof(GunnerStation)))
        {
            if (station == GunnerStation.None) continue;

            GameObject buttonObj = Instantiate(stationButtonPrefab, stationButtonContainer);
            Button button = buttonObj.GetComponent<Button>();
            TextMeshProUGUI buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();

            if (buttonText != null)
            {
                buttonText.text = GetStationDisplayName(station);
            }

            if (button != null)
            {
                GunnerStation capturedStation = station; // Capture for closure
                button.onClick.AddListener(() => OnStationButtonClicked(capturedStation));
            }
        }
    }

    private void RefreshStationButtons()
    {
        if (stationButtonContainer == null) return;

        foreach (Transform child in stationButtonContainer)
        {
            Button button = child.GetComponent<Button>();
            TextMeshProUGUI buttonText = child.GetComponentInChildren<TextMeshProUGUI>();
            
            if (button == null || buttonText == null) continue;

            // Try to parse station name from button text
            GunnerStation station = ParseStationFromDisplayName(buttonText.text);
            
            if (station != GunnerStation.None && GameModeManager.Instance != null)
            {
                bool isAvailable = GameModeManager.Instance.IsStationAvailable(station);
                bool isCurrent = GameModeManager.Instance.CurrentStation == station;
                
                button.interactable = isAvailable && !isCurrent;
                
                // Visual feedback for current station
                if (isCurrent && buttonText != null)
                {
                    buttonText.color = Color.yellow;
                }
                else
                {
                    buttonText.color = isAvailable ? Color.white : Color.gray;
                }
            }
        }
    }

    private void OnStationButtonClicked(GunnerStation station)
    {
        if (GameModeManager.Instance != null)
        {
            GameModeManager.Instance.SwitchStation(station);
            RefreshUI();
        }
    }

    private void OnReturnToMainClicked()
    {
        if (GameModeManager.Instance != null)
        {
            GameModeManager.Instance.ExitCombatMode();
        }
    }

    private string GetStationDisplayName(GunnerStation station)
    {
        switch (station)
        {
            case GunnerStation.TopTurret: return "Top Turret";
            case GunnerStation.BallTurret: return "Ball Turret";
            case GunnerStation.LeftWaist: return "Left Waist";
            case GunnerStation.RightWaist: return "Right Waist";
            case GunnerStation.TailGunner: return "Tail Gunner";
            case GunnerStation.Nose: return "Nose";
            default: return "Unknown";
        }
    }

    private GunnerStation ParseStationFromDisplayName(string displayName)
    {
        switch (displayName)
        {
            case "Top Turret": return GunnerStation.TopTurret;
            case "Ball Turret": return GunnerStation.BallTurret;
            case "Left Waist": return GunnerStation.LeftWaist;
            case "Right Waist": return GunnerStation.RightWaist;
            case "Tail Gunner": return GunnerStation.TailGunner;
            case "Nose": return GunnerStation.Nose;
            default: return GunnerStation.None;
        }
    }

    private string GetCrewNameForStation(GunnerStation station)
    {
        if (CrewManager.Instance == null || GameModeManager.Instance == null) return null;

        // Get crew role for this station
        CrewRole? role = null;
        switch (station)
        {
            case GunnerStation.TopTurret: role = CrewRole.Gunner; break;
            case GunnerStation.BallTurret: role = CrewRole.Gunner; break;
            case GunnerStation.LeftWaist: role = CrewRole.Gunner; break;
            case GunnerStation.RightWaist: role = CrewRole.Gunner; break;
            case GunnerStation.TailGunner: role = CrewRole.Gunner; break;
            case GunnerStation.Nose: role = CrewRole.Bombardier; break;
        }

        if (!role.HasValue) return null;

        var crew = CrewManager.Instance.AllCrew.Find(c => c.Role == role.Value);
        return crew?.Name;
    }

    /// <summary>
    /// Add a message to the combat log (if assigned).
    /// </summary>
    public void LogMessage(string message)
    {
        if (combatLogText != null)
        {
            combatLogText.text += $"\n{message}";
        }
    }
}
