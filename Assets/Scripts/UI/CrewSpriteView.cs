using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays a crew member sprite on the plane visualization.
/// Shows their position, current status via color tinting, and what they're doing.
/// </summary>
public class CrewSpriteView : MonoBehaviour
{
    [Header("Crew Configuration")]
    public string crewId;
    
    [Header("Sprite References")]
    public Image crewSprite; // The main crew member sprite
    
    [Header("Base State Sprites")]
    public Sprite idleNoStationSprite; // Standing around with no station assignment
    public Sprite movingSprite; // Walking/moving between locations
    public Sprite workingSprite; // Performing an action (repair, medical, fire)
    public Sprite incapacitatedSprite; // Dead or unconscious
    
    [Header("Station-Specific Idle Sprites (B-17F)")]
    public Sprite pilotIdleSprite; // Sitting in pilot seat
    public Sprite copilotIdleSprite; // Sitting in copilot seat
    public Sprite navigatorIdleSprite; // At navigator station with nose gun
    public Sprite bombardierIdleSprite; // At bombardier station with nose gun
    public Sprite radioOperatorIdleSprite; // At radio station
    public Sprite topTurretIdleSprite; // In top turret
    public Sprite ballTurretIdleSprite; // In ball turret
    public Sprite waistGunIdleSprite; // At waist gun position (can use for both left/right)
    public Sprite tailGunIdleSprite; // At tail gun position
    
    [Header("Status Visuals")]
    public Image statusTintOverlay; // Optional overlay to tint the sprite
    public GameObject progressBar; // Shows when working on something
    public Image progressFill; // The fill image for the progress bar
    public Image actionIcon; // Shows what action they're performing
    
    [Header("Action Icons")]
    public Sprite repairIcon;
    public Sprite fireIcon;
    public Sprite medicalIcon;
    public Sprite moveIcon;
    
    [Header("Status Colors")]
    public Color healthyTint = Color.white;
    public Color lightInjuryTint = new Color(1f, 1f, 0.7f); // Slight yellow
    public Color seriousInjuryTint = new Color(1f, 0.7f, 0.5f); // Orange tint
    public Color criticalTint = new Color(1f, 0.5f, 0.5f); // Red tint
    
    private CrewMember trackedCrew;
    
    private void Start()
    {
        if (progressBar != null)
        {
            progressBar.SetActive(false);
        }
        FindTrackedCrew();
    }
    
    private void Update()
    {
        if (trackedCrew == null)
        {
            FindTrackedCrew();
            if (trackedCrew == null)
            {
                // Hide everything if no crew found
                if (crewSprite != null) crewSprite.enabled = false;
                return;
            }
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
        if (crewSprite != null)
        {
            crewSprite.enabled = true;
            
            // Apply status tint
            Color tint = GetStatusTint(trackedCrew.Status);
            crewSprite.color = tint;
            
            if (statusTintOverlay != null)
            {
                statusTintOverlay.color = tint;
            }
        }
        
        // Update sprite based on activity
        UpdateSprite();
        
        // Update action progress bar
        UpdateProgressBar();
        
        // Update action icon
        UpdateActionIcon();
    }
    
    private void UpdateSprite()
    {
        if (crewSprite == null) return;
        
        Sprite spriteToUse = null;
        
        // Priority 1: Incapacitated state (dead/unconscious)
        if (trackedCrew.Status == CrewStatus.Dead || trackedCrew.Status == CrewStatus.Unconscious)
        {
            spriteToUse = incapacitatedSprite;
        }
        // Priority 2: Check visual state (what they're doing)
        else if (trackedCrew.VisualState == CrewVisualState.Moving)
        {
            spriteToUse = movingSprite;
        }
        else if (trackedCrew.VisualState == CrewVisualState.Working)
        {
            spriteToUse = workingSprite;
        }
        // Priority 3: Idle at station - use station-specific sprite
        else if (trackedCrew.VisualState == CrewVisualState.IdleAtStation && trackedCrew.CurrentStation != StationType.None)
        {
            spriteToUse = GetStationIdleSprite(trackedCrew.CurrentStation);
        }
        // Priority 4: Idle with no station
        else
        {
            spriteToUse = idleNoStationSprite;
        }
        
        // Apply sprite (or keep current if null)
        if (spriteToUse != null)
        {
            crewSprite.sprite = spriteToUse;
        }
    }
    
    /// <summary>
    /// Get the appropriate idle sprite for a specific station.
    /// </summary>
    private Sprite GetStationIdleSprite(StationType station)
    {
        return station switch
        {
            StationType.Pilot => pilotIdleSprite,
            StationType.CoPilot => copilotIdleSprite,
            StationType.Navigator => navigatorIdleSprite,
            StationType.Bombardier => bombardierIdleSprite,
            StationType.RadioOperator => radioOperatorIdleSprite,
            StationType.TopTurret => topTurretIdleSprite,
            StationType.BallTurret => ballTurretIdleSprite,
            StationType.LeftWaistGun => waistGunIdleSprite,
            StationType.RightWaistGun => waistGunIdleSprite, // Same sprite for both waist guns
            StationType.TailGun => tailGunIdleSprite,
            _ => idleNoStationSprite // Fallback
        };
    }
    
    private void UpdateProgressBar()
    {
        bool hasAction = trackedCrew.CurrentAction != null && 
                        trackedCrew.CurrentAction.Type != ActionType.Idle;
        
        if (progressBar != null)
        {
            progressBar.SetActive(hasAction);
        }
        
        if (hasAction && progressFill != null)
        {
            float progress = trackedCrew.CurrentAction.Duration > 0f
                ? trackedCrew.CurrentAction.Elapsed / trackedCrew.CurrentAction.Duration
                : 0f;
            
            progressFill.fillAmount = Mathf.Clamp01(progress);
        }
    }
    
    private void UpdateActionIcon()
    {
        if (actionIcon == null) return;
        
        if (trackedCrew.CurrentAction == null || trackedCrew.CurrentAction.Type == ActionType.Idle)
        {
            actionIcon.enabled = false;
            return;
        }
        
        actionIcon.enabled = true;
        
        // Set the appropriate icon based on action type
        Sprite icon = trackedCrew.CurrentAction.Type switch
        {
            ActionType.Repair => repairIcon,
            ActionType.ExtinguishFire => fireIcon,
            ActionType.TreatInjury => medicalIcon,
            ActionType.Move => moveIcon,
            _ => null
        };
        
        if (icon != null)
        {
            actionIcon.sprite = icon;
        }
        else
        {
            actionIcon.enabled = false;
        }
    }
    
    private Color GetStatusTint(CrewStatus status)
    {
        return status switch
        {
            CrewStatus.Healthy => healthyTint,
            CrewStatus.Light => lightInjuryTint, // Slight tint but still functional
            CrewStatus.Serious => seriousInjuryTint,
            CrewStatus.Critical => criticalTint,
            CrewStatus.Dead => Color.gray, // Grayed out
            CrewStatus.Unconscious => Color.gray, // Grayed out
            _ => Color.white
        };
    }
    
    /// <summary>
    /// Move this sprite to a specific position on the plane (for movement animation later)
    /// </summary>
    public void SetPosition(Vector2 position)
    {
        transform.position = position;
    }
    
    /// <summary>
    /// Get the tracked crew member
    /// </summary>
    public CrewMember GetTrackedCrew() => trackedCrew;
}