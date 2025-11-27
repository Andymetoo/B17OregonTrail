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
    public Sprite standingSprite; // Default/idle sprite
    public Sprite inStationSprite; // When manning a station (optional)
    
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
        
        // Check if crew is at a station
        bool isAtStation = !string.IsNullOrEmpty(trackedCrew.CurrentStationId);
        
        if (isAtStation && inStationSprite != null)
        {
            crewSprite.sprite = inStationSprite;
        }
        else if (standingSprite != null)
        {
            crewSprite.sprite = standingSprite;
        }
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
            CrewStatus.Light => lightInjuryTint,
            CrewStatus.Serious => seriousInjuryTint,
            CrewStatus.Critical => criticalTint,
            CrewStatus.Dead => Color.gray,
            CrewStatus.Unconscious => Color.gray,
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