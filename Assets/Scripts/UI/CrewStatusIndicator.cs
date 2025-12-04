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
    public Image selectionIndicator; // Visual indicator when this crew is selected (e.g., highlight border, checkmark, etc.)
    
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
    
    [Header("Action Icon Blink Settings")]
    [Tooltip("Blink the action icon when crew is actively performing an action")]
    public bool blinkActionIcon = true;
    [Tooltip("Speed of the blink animation")]
    public float blinkSpeed = 2f;
    [Tooltip("Minimum alpha during blink")]
    public float blinkMinAlpha = 0.3f;
    
    [Header("Selection Highlight")]
    [Tooltip("Color tint to apply to button when this crew is selected")]
    public Color selectedTint = new Color(1f, 1f, 0.7f, 1f); // Slight yellow tint
    [Tooltip("Show selection indicator image when selected")]
    public bool useSelectionIndicator = true;
    
    private CrewMember trackedCrew;
    private Image buttonImage;
    private Color originalButtonColor;
    private float blinkTimer = 0f;
    
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
        
        // Hide selection indicator initially
        if (selectionIndicator != null)
        {
            selectionIndicator.enabled = false;
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
        
        // Check if this crew is currently selected
        bool isSelected = false;
        if (OrdersUIController.Instance != null)
        {
            isSelected = (OrdersUIController.Instance.SelectedCrewId == crewId);
        }
        
        // Show/hide selection indicator
        if (selectionIndicator != null && useSelectionIndicator)
        {
            selectionIndicator.enabled = isSelected;
        }
        
        // Update button interactability based on crew status
        bool isUsable = IsCrewUsable(trackedCrew.Status);
        // Keep buttons enabled so injured crew can be clicked as medical targets
        SetButtonInteractable(true);
        
        // Update button appearance based on usability (no longer using selectedTint on button itself)
        if (buttonImage != null)
        {
            Color buttonColor = originalButtonColor;
            
            // Apply transparency for incapacitated crew
            if (!isUsable)
            {
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
        // Healthy and Light injured crew can perform actions (with penalties)
        // Serious, Critical, Unconscious, and Dead cannot
        return status == CrewStatus.Healthy || status == CrewStatus.Light;
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
        bool isActivelyPerformingAction = false;
        
        // Check if dead/unconscious first
        if (status == CrewStatus.Dead || status == CrewStatus.Unconscious)
        {
            spriteToShow = deadSprite;
        }
        // Check if performing an action
        else if (currentAction != null && currentAction.Type != ActionType.Idle)
        {
            // Check if they're in the Performing phase (actively working, not moving)
            isActivelyPerformingAction = (currentAction.Phase == ActionPhase.Performing);
            
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
            
            // Apply blinking effect if actively performing an action
            if (isActivelyPerformingAction && blinkActionIcon)
            {
                blinkTimer += Time.deltaTime * blinkSpeed;
                float alpha = Mathf.Lerp(blinkMinAlpha, 1f, (Mathf.Sin(blinkTimer) + 1f) / 2f);
                
                Color iconColor = statusIndicator.color;
                iconColor.a = alpha;
                statusIndicator.color = iconColor;
            }
            else
            {
                // Reset alpha to full when not performing
                Color iconColor = statusIndicator.color;
                iconColor.a = 1f;
                statusIndicator.color = iconColor;
            }
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