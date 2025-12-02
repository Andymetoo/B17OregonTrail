using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Visual status indicator for crew members - shows injury state and grays out buttons when incapacitated.
/// Attach this to each crew button to show health status.
/// </summary>
public class CrewStatusIndicator : MonoBehaviour
{
    [Header("Crew Configuration")]
    public string crewId;
    
    [Header("UI References")]
    public Button crewButton;
    public Image statusIndicator; // Single image that shows different sprites (injury/dead/action icons)
    public Image borderImage; // Optional colored border showing health status
    public Image stationIcon; // Shows which station this crew is assigned to
    
    [Header("Status Sprites")]
    public Sprite injurySprite; // Any injury state (light/serious/critical)
    public Sprite deadSprite; // Dead/unconscious
    
    [Header("Action Sprites")]
    public Sprite repairSprite; // When repairing
    public Sprite medicalSprite; // When treating injury
    public Sprite fireSprite; // When fighting fire
    public Sprite moveSprite; // When moving (optional)
    
    [Header("Station Icons (B-17F)")]
    public Sprite pilotIcon; // Pilot/CoPilot
    public Sprite gunIcon; // All gun stations (TopTurret, BallTurret, Waist, Tail, Navigator, Bombardier)
    public Sprite navigatorIcon; // Navigator (optional - can use gunIcon since they man nose gun)
    public Sprite bombardierIcon; // Bombardier (optional - can use gunIcon)
    public Sprite radioIcon; // Radio Operator
    
    [Header("Status Colors (for button feedback)")]
    public float incapacitatedAlpha = 0.5f;
    
    [Header("Border Colors")]
    public Color healthyColor = Color.green;
    public Color lightInjuryColor = Color.yellow;
    public Color seriousInjuryColor = new Color(1f, 0.5f, 0f); // Orange
    public Color criticalColor = Color.red;
    public Color deadColor = Color.black;
    
    private CrewMember trackedCrew;
    private Image buttonImage;
    private Color originalButtonColor;
    
    private void Start()
    {
        if (crewButton != null)
        {
            buttonImage = crewButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                originalButtonColor = buttonImage.color;
            }
        }
        
        // Find the crew member we're tracking
        FindTrackedCrew();
        UpdateVisuals();
    }
    
    private void Update()
    {
        // Refresh crew reference if needed
        if (trackedCrew == null)
        {
            FindTrackedCrew();
        }
        
        UpdateVisuals();
    }
    
    private void FindTrackedCrew()
    {
        if (string.IsNullOrEmpty(crewId) || CrewManager.Instance == null) return;
        
        trackedCrew = CrewManager.Instance.GetCrewById(crewId);
    }
    
    private void UpdateVisuals()
    {
        if (trackedCrew == null)
        {
            // No crew found - disable button
            SetButtonInteractable(false);
            HideStatusIndicator();
            return;
        }
        
        // Update button interactability based on crew status
        bool isUsable = IsCrewUsable(trackedCrew.Status);
        // Keep buttons enabled so injured crew can be clicked as medical targets
        SetButtonInteractable(true);
        
        // Update button transparency for unusable crew (visual feedback only)
        if (buttonImage != null)
        {
            Color buttonColor = originalButtonColor;
            if (!isUsable)
            {
                // Make it slightly transparent but still clickable
                buttonColor.a = incapacitatedAlpha;
            }
            buttonImage.color = buttonColor;
        }
        
        // Update the status indicator sprite based on current state
        UpdateStatusIndicator(trackedCrew.Status, trackedCrew.CurrentAction);
        
        // Update border color based on health status
        UpdateBorderColor(trackedCrew.Status);
        
        // Update station icon based on current assignment
        UpdateStationIcon(trackedCrew.CurrentStation);
    }
    
    private bool IsCrewUsable(CrewStatus status)
    {
        // Any injury or incapacitation makes crew unable to perform actions
        return status == CrewStatus.Healthy;
    }
    
    private void UpdateStatusIndicator(CrewStatus status, CrewAction currentAction)
    {
        if (statusIndicator == null) return;
        
        // Priority order:
        // 1. Dead/Unconscious (highest priority)
        // 2. Current action being performed
        // 3. Injury state
        // 4. Hide if healthy and idle
        
        Sprite spriteToShow = null;
        
        // Check if dead/unconscious first
        if (status == CrewStatus.Dead || status == CrewStatus.Unconscious)
        {
            spriteToShow = deadSprite;
        }
        // Check if performing an action
        else if (currentAction != null && currentAction.Type != ActionType.Idle)
        {
            spriteToShow = currentAction.Type switch
            {
                ActionType.Repair => repairSprite,
                ActionType.TreatInjury => medicalSprite,
                ActionType.ExtinguishFire => fireSprite,
                ActionType.Move => moveSprite,
                _ => null
            };
        }
        // Check injury state
        else if (status == CrewStatus.Light || status == CrewStatus.Serious || status == CrewStatus.Critical)
        {
            spriteToShow = injurySprite;
        }
        
        // Show or hide the indicator
        if (spriteToShow != null)
        {
            statusIndicator.enabled = true;
            statusIndicator.sprite = spriteToShow;
        }
        else
        {
            // Healthy and idle - hide the indicator
            statusIndicator.enabled = false;
        }
    }
    
    private void HideStatusIndicator()
    {
        if (statusIndicator != null)
        {
            statusIndicator.enabled = false;
        }
    }
    
    private void UpdateBorderColor(CrewStatus status)
    {
        if (borderImage == null) return;
        
        Color borderColor = status switch
        {
            CrewStatus.Healthy => healthyColor,
            CrewStatus.Light => lightInjuryColor,
            CrewStatus.Serious => seriousInjuryColor,
            CrewStatus.Critical => criticalColor,
            CrewStatus.Dead => deadColor,
            CrewStatus.Unconscious => deadColor,
            _ => Color.gray
        };
        
        borderImage.color = borderColor;
    }
    
    private void UpdateStationIcon(StationType station)
    {
        if (stationIcon == null) return;
        
        // If no station, hide the icon
        if (station == StationType.None)
        {
            stationIcon.enabled = false;
            return;
        }
        
        // Select appropriate sprite based on station type
        Sprite iconSprite = GetIconForStation(station);
        
        if (iconSprite != null)
        {
            stationIcon.enabled = true;
            stationIcon.sprite = iconSprite;
        }
        else
        {
            // No sprite configured for this station - hide icon
            stationIcon.enabled = false;
        }
    }
    
    private Sprite GetIconForStation(StationType type)
    {
        // Check for specific icons first (if you want unique icons per role)
        switch (type)
        {
            case StationType.Pilot:
            case StationType.CoPilot:
                return pilotIcon;
            
            case StationType.Navigator:
                // Use specific navigator icon if set, otherwise fall back to gun icon
                return navigatorIcon != null ? navigatorIcon : gunIcon;
            
            case StationType.Bombardier:
                // Use specific bombardier icon if set, otherwise fall back to gun icon
                return bombardierIcon != null ? bombardierIcon : gunIcon;
            
            case StationType.RadioOperator:
                return radioIcon;
            
            // All gun stations use the same gun icon
            case StationType.TopTurret:
            case StationType.BallTurret:
            case StationType.LeftWaistGun:
            case StationType.RightWaistGun:
            case StationType.TailGun:
                return gunIcon;
            
            default:
                return null;
        }
    }
    
    private void SetButtonInteractable(bool interactable)
    {
        if (crewButton != null)
        {
            crewButton.interactable = interactable;
        }
    }
    
    /// <summary>
    /// Get the tracked crew member for external use
    /// </summary>
    public CrewMember GetTrackedCrew() => trackedCrew;
    
    /// <summary>
    /// Manual refresh for when crew data changes dramatically
    /// </summary>
    public void RefreshVisuals()
    {
        FindTrackedCrew();
        UpdateVisuals();
    }
}