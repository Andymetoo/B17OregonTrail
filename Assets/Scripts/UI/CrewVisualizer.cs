using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Visualizes a crew member sprite and smoothly moves it between positions
/// </summary>
[RequireComponent(typeof(Image))]
public class CrewVisualizer : MonoBehaviour
{
    [Header("Crew Reference")]
    [SerializeField] private string crewId;
    
    [Header("Sprite States")]
    [SerializeField] private Sprite idleSprite; // Default idle (no station)
    [SerializeField] private Sprite movingSprite;
    [SerializeField] private Sprite workingSprite;
    [SerializeField] private Sprite incapacitatedSprite; // used for Unconscious/Dead
    
    [Header("Station-Specific Idle Sprites (B-17F)")]
    [SerializeField] private Sprite pilotIdleSprite;
    [SerializeField] private Sprite copilotIdleSprite;
    [SerializeField] private Sprite navigatorIdleSprite;
    [SerializeField] private Sprite bombardierIdleSprite;
    [SerializeField] private Sprite radioOperatorIdleSprite;
    [SerializeField] private Sprite topTurretIdleSprite;
    [SerializeField] private Sprite ballTurretIdleSprite;
    [SerializeField] private Sprite leftWaistGunIdleSprite;
    [SerializeField] private Sprite rightWaistGunIdleSprite;
    [SerializeField] private Sprite tailGunIdleSprite;
    
    [Header("Action Progress Display")]
    [SerializeField] private GameObject actionProgressUI; // Container for progress bar + icon (above crew head)
    [SerializeField] private Image progressFill; // Progress bar fill
    [SerializeField] private Image actionIcon; // Icon showing what action (repair/medical/fire)
    [SerializeField] private Sprite repairIconSprite;
    [SerializeField] private Sprite medicalIconSprite;
    [SerializeField] private Sprite fireIconSprite;
    
    [Header("Movement")]
    [SerializeField] private float smoothing = 8f;

    [Header("Tinting")]
    [SerializeField] private Color baseColor = Color.white;
    [SerializeField] private Color workingTint = new Color(1f, 0.9f, 0.2f, 0.4f); // subtle warm tint
    [SerializeField] private Color lightInjuryTint = new Color(1f, 0.5f, 0f, 0.35f); // orange
    [SerializeField] private Color seriousInjuryTint = new Color(1f, 0.25f, 0f, 0.4f); // deeper orange
    [SerializeField] private Color criticalInjuryTint = new Color(1f, 0f, 0f, 0.45f); // red
    [SerializeField] private Color deadTint = new Color(0.3f, 0.3f, 0.3f, 0.5f); // gray
    [SerializeField] private bool additiveTints = true; // additive vs multiply
    [SerializeField] private bool dimWhenIncapacitated = true;
    [SerializeField] [Range(0f, 1f)] private float incapacitatedAlpha = 0.3f;
    
    private Image image;
    private RectTransform rectTransform;
    private CrewMember crew;
    private bool snappedToStart = false;
    private RectTransform actionProgressUIRect; // Cache the progress UI rect for counter-flipping
    
    private void Awake()
    {
        image = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
        
        // Cache action progress UI rect if it exists
        if (actionProgressUI != null)
        {
            actionProgressUIRect = actionProgressUI.GetComponent<RectTransform>();
        }
    }
    
    private void Start()
    {
        if (CrewManager.Instance != null)
        {
            crew = CrewManager.Instance.GetCrewById(crewId);
            // Do not set anchoredPosition here to avoid snapping to (0,0) before positions init
        }
    }
    
    private void Update()
    {
        if (crew == null) return;
        
        // Wait until CrewManager has initialized positions, then snap once to avoid (0,0)
        if (!snappedToStart)
        {
            if (CrewManager.Instance != null && CrewManager.Instance.PositionsInitialized)
            {
                rectTransform.anchoredPosition = crew.CurrentPosition;
                snappedToStart = true;
                UpdateSprite();
            }
            return; // Skip lerp until we've snapped to the correct start
        }

        // Smoothly lerp toward crew's current position thereafter
        Vector2 currentPos = rectTransform.anchoredPosition;
        Vector2 targetPos = crew.CurrentPosition;

        if (Vector2.Distance(currentPos, targetPos) > 0.5f)
        {
            rectTransform.anchoredPosition = Vector2.Lerp(currentPos, targetPos, Time.deltaTime * smoothing);
        }
        else
        {
            rectTransform.anchoredPosition = targetPos;
        }
        
        // Flip horizontally based on travel direction (keep last facing when nearly stationary)
        float dx = targetPos.x - currentPos.x;
        if (Mathf.Abs(dx) > 0.01f)
        {
            var scale = rectTransform.localScale;
            scale.x = dx >= 0 ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            rectTransform.localScale = scale;
            
            // Counter-flip the action progress UI so it stays upright
            if (actionProgressUIRect != null)
            {
                var uiScale = actionProgressUIRect.localScale;
                uiScale.x = dx >= 0 ? Mathf.Abs(uiScale.x) : -Mathf.Abs(uiScale.x);
                actionProgressUIRect.localScale = uiScale;
            }
        }

        UpdateSprite();
        UpdateActionProgress();
    }
    
    private void UpdateSprite()
    {
        if (crew == null || image == null) return;

        // Determine if incapacitated (can't perform actions and should show collapsed sprite)
        // Serious/Critical/Unconscious/Dead = incapacitated sprite
        // Light = injured but mobile (use normal sprites with tint)
        bool showIncapacitatedSprite = crew.Status == CrewStatus.Serious ||
                                       crew.Status == CrewStatus.Critical || 
                                       crew.Status == CrewStatus.Unconscious || 
                                       crew.Status == CrewStatus.Dead;

        // Choose sprite with priority: Incapacitated > Moving > Working > Station Idle > Default Idle
        Sprite newSprite = null;
        
        if (showIncapacitatedSprite && incapacitatedSprite != null)
        {
            newSprite = incapacitatedSprite;
        }
        else if (crew.VisualState == CrewVisualState.Moving && movingSprite != null)
        {
            newSprite = movingSprite;
        }
        else if (crew.VisualState == CrewVisualState.Working && workingSprite != null)
        {
            newSprite = workingSprite;
        }
        else if (crew.VisualState == CrewVisualState.IdleAtStation && crew.CurrentStation != StationType.None)
        {
            // Use station-specific idle sprite
            newSprite = GetStationIdleSprite(crew.CurrentStation);
        }
        
        // Fallback to default idle if no specific sprite found
        if (newSprite == null)
        {
            newSprite = idleSprite;
        }
        
        if (newSprite != null && image.sprite != newSprite)
        {
            image.sprite = newSprite;
        }

        // Compose tint
        Color c = baseColor;
        if (crew.VisualState == CrewVisualState.Working)
            c = Blend(c, workingTint, additiveTints);

        switch (crew.Status)
        {
            case CrewStatus.Light:
                c = Blend(c, lightInjuryTint, additiveTints);
                break;
            case CrewStatus.Serious:
                c = Blend(c, seriousInjuryTint, additiveTints);
                break;
            case CrewStatus.Critical:
                c = Blend(c, criticalInjuryTint, additiveTints);
                break;
            case CrewStatus.Unconscious:
            case CrewStatus.Dead:
                c = Blend(c, deadTint, additiveTints);
                break;
        }

        // Dim the sprite for incapacitated crew
        if (showIncapacitatedSprite && dimWhenIncapacitated)
        {
            c.a = incapacitatedAlpha;
        }
        image.color = c;
    }
    
    /// <summary>
    /// Get station-specific idle sprite based on current station assignment.
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
            StationType.LeftWaistGun => leftWaistGunIdleSprite,
            StationType.RightWaistGun => rightWaistGunIdleSprite,
            StationType.TailGun => tailGunIdleSprite,
            _ => idleSprite // Fallback to default
        };
    }
    
    /// <summary>
    /// Update the action progress bar and icon above the crew member's head.
    /// </summary>
    private void UpdateActionProgress()
    {
        if (actionProgressUI == null) return;
        
        // Show progress UI only when crew has an active action (not Idle, not Move)
        bool hasAction = crew.CurrentAction != null && 
                        crew.CurrentAction.Type != ActionType.Idle &&
                        crew.CurrentAction.Type != ActionType.Move;
        
        actionProgressUI.SetActive(hasAction);
        
        if (!hasAction) return;
        
        // Update progress bar fill
        if (progressFill != null)
        {
            float progress = crew.CurrentAction.Duration > 0f
                ? crew.CurrentAction.Elapsed / crew.CurrentAction.Duration
                : 0f;
            progressFill.fillAmount = Mathf.Clamp01(progress);
        }
        
        // Update action icon
        if (actionIcon != null)
        {
            Sprite icon = crew.CurrentAction.Type switch
            {
                ActionType.Repair => repairIconSprite,
                ActionType.TreatInjury => medicalIconSprite,
                ActionType.ExtinguishFire => fireIconSprite,
                _ => null
            };
            
            if (icon != null)
            {
                actionIcon.enabled = true;
                actionIcon.sprite = icon;
            }
            else
            {
                actionIcon.enabled = false;
            }
        }
    }

    private static Color Blend(Color baseC, Color tint, bool additive)
    {
        if (tint.a <= 0f) return baseC;
        if (additive)
        {
            // Additive using tint alpha as intensity; do not increase overall alpha here
            float r = Mathf.Clamp01(baseC.r + tint.r * tint.a);
            float g = Mathf.Clamp01(baseC.g + tint.g * tint.a);
            float b = Mathf.Clamp01(baseC.b + tint.b * tint.a);
            return new Color(r, g, b, baseC.a);
        }
        else
        {
            // Multiplicative blend (modulate)
            float r = baseC.r * Mathf.Lerp(1f, tint.r, tint.a);
            float g = baseC.g * Mathf.Lerp(1f, tint.g, tint.a);
            float b = baseC.b * Mathf.Lerp(1f, tint.b, tint.a);
            return new Color(r, g, b, baseC.a);
        }
    }
    
    public void SetCrewId(string id)
    {
        crewId = id;
        if (CrewManager.Instance != null)
        {
            crew = CrewManager.Instance.GetCrewById(id);
            snappedToStart = false; // force a fresh snap when positions are ready
            if (crew != null && CrewManager.Instance.PositionsInitialized)
            {
                rectTransform.anchoredPosition = crew.CurrentPosition;
                snappedToStart = true;
                UpdateSprite();
            }
        }
    }
}
