using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Draws a visual line from a crew member's UI button to their sprite on the plane.
/// Helps players quickly identify which crew member they have selected.
/// Uses UI Image component instead of LineRenderer for proper Canvas rendering.
/// </summary>
public class CrewSelectionLine : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The OrdersUIController to track which crew is selected")]
    public OrdersUIController ordersController;
    
    [Tooltip("Parent transform of all crew buttons (to find button positions)")]
    public RectTransform crewButtonsParent;
    
    [Tooltip("Parent transform of all crew sprites (to find sprite positions)")]
    public RectTransform crewSpritesParent;
    
    [Header("Line Settings")]
    [Tooltip("Color of the selection line")]
    public Color lineColor = new Color(0f, 1f, 1f, 0.8f); // Cyan with transparency
    
    [Tooltip("Width of the line in pixels")]
    public float lineWidth = 3f;
    
    [Header("Animation (Optional)")]
    [Tooltip("Animate the line pulsing/fading")]
    public bool animateLine = true;
    
    [Tooltip("Speed of the pulse animation")]
    public float pulseSpeed = 2f;
    
    [Tooltip("Min/max alpha for pulse")]
    public float minAlpha = 0.4f;
    public float maxAlpha = 1f;
    
    private Image lineImage;
    private RectTransform lineRect;
    private Canvas canvas;
    private float pulseTimer = 0f;
    
    // Mapping from crew IDs to button names (if they differ)
    private System.Collections.Generic.Dictionary<string, string> crewIdToButtonName = new System.Collections.Generic.Dictionary<string, string>()
    {
        // Map full crew IDs to shortened button names
        { "BallTurretGunner", "BallGunner" },
        { "LeftWaistGunner", "LWGunner" },
        { "RightWaistGunner", "RWGunner" },
        { "RadioOperator", "RadioOp" },
        { "RadioOp", "RadioOp" },
        { "RadioOP", "RadioOp" }, // The actual crew ID is RadioOP (all caps)
        // Add more mappings as needed - buttons that match exactly don't need entries
    };
    
    // Mapping from crew IDs to sprite names (if they differ)
    private System.Collections.Generic.Dictionary<string, string> crewIdToSpriteName = new System.Collections.Generic.Dictionary<string, string>()
    {
        // Map full crew IDs to sprite GameObject names
        { "TailGunner", "Tail_Gunner_Sprite" },
        { "RightWaistGunner", "RWG_Sprite" },
        { "LeftWaistGunner", "LWG_Sprite" },
        { "BallTurretGunner", "Ball_Sprite" },
        { "RadioOperator", "RadioOP_Sprite" },
        { "RadioOp", "RadioOP_Sprite" },
        { "RadioOP", "RadioOP_Sprite" },
        { "Engineer", "Engineer_Sprite" },
        { "Pilot", "Pilot_Sprite" },
        { "Copilot", "Copilot_Sprite" },
        { "CoPilot", "Copilot_Sprite" },
        { "Navigator", "Nav_Sprite" },
        { "Bombardier", "Bombardier_Sprite" },
    };
    
    private void Awake()
    {
        // Find or create the Image component for the line
        lineImage = GetComponent<Image>();
        if (lineImage == null)
        {
            lineImage = gameObject.AddComponent<Image>();
        }
        
        lineRect = GetComponent<RectTransform>();
        if (lineRect == null)
        {
            lineRect = gameObject.AddComponent<RectTransform>();
        }
        
        // Set up the image as a simple colored rectangle
        lineImage.color = lineColor;
        lineImage.raycastTarget = false; // Don't block clicks
        
        // Find canvas
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[CrewSelectionLine] Could not find Canvas in parent hierarchy!");
        }
        else
        {
            Debug.Log($"[CrewSelectionLine] Found Canvas with RenderMode: {canvas.renderMode}");
        }
        
        // Initially hide the line
        lineImage.enabled = false;
        
        Debug.Log("[CrewSelectionLine] Component initialized successfully");
    }
    
    private void Update()
    {
        if (ordersController == null)
        {
            lineImage.enabled = false;
            return;
        }
        
        string selectedCrewId = ordersController.SelectedCrewId;
        
        // No crew selected - hide line
        if (string.IsNullOrEmpty(selectedCrewId))
        {
            lineImage.enabled = false;
            return;
        }
        
        // Find the button and sprite for this crew
        RectTransform buttonRect = FindCrewButton(selectedCrewId) as RectTransform;
        RectTransform spriteRect = FindCrewSprite(selectedCrewId) as RectTransform;
        
        if (buttonRect == null)
        {
            if (selectedCrewId.Contains("Radio"))
            {
                Debug.LogWarning($"[CrewSelectionLine] RadioOp debug - Could not find button for crew: '{selectedCrewId}'");
            }
            lineImage.enabled = false;
            return;
        }
        
        if (spriteRect == null)
        {
            if (selectedCrewId.Contains("Radio"))
            {
                Debug.LogWarning($"[CrewSelectionLine] RadioOp debug - Could not find sprite for crew: '{selectedCrewId}'");
            }
            lineImage.enabled = false;
            return;
        }
        
        // Get positions - always convert to common canvas space
        Vector2 buttonPos = GetCanvasPosition(buttonRect);
        Vector2 spritePos = GetCanvasPosition(spriteRect);
        
        // Calculate line parameters
        Vector2 direction = spritePos - buttonPos;
        float distance = direction.magnitude;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        // Position the line at button position, stretch it to sprite (with 2x distance to reach all the way)
        lineRect.anchoredPosition = buttonPos;
        lineRect.sizeDelta = new Vector2(distance * 2f, lineWidth);
        lineRect.rotation = Quaternion.Euler(0, 0, angle);
        lineRect.pivot = new Vector2(0f, 0.5f); // Pivot at start (left edge)
        
        // Enable the line
        lineImage.enabled = true;
        
        // Optional: Animate the line
        if (animateLine)
        {
            pulseTimer += Time.deltaTime * pulseSpeed;
            float alpha = Mathf.Lerp(minAlpha, maxAlpha, (Mathf.Sin(pulseTimer) + 1f) / 2f);
            
            Color currentColor = lineColor;
            currentColor.a = alpha;
            lineImage.color = currentColor;
        }
    }
    
    /// <summary>
    /// Get the position of a RectTransform in canvas space.
    /// </summary>
    private Vector2 GetCanvasPosition(RectTransform rect)
    {
        if (canvas == null) return rect.anchoredPosition;
        
        // Convert to canvas space
        Vector2 canvasPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, rect.position),
            canvas.worldCamera,
            out canvasPos
        );
        
        return canvasPos;
    }
    
    /// <summary>
    /// Find the UI button for a specific crew member by ID.
    /// Uses mapping dictionary to handle mismatched names.
    /// </summary>
    private Transform FindCrewButton(string crewId)
    {
        if (crewButtonsParent == null)
        {
            Debug.LogWarning("[CrewSelectionLine] crewButtonsParent is null!");
            return null;
        }
        
        // Check if there's a mapped button name for this crew ID
        string buttonNameToFind = crewId;
        if (crewIdToButtonName.ContainsKey(crewId))
        {
            buttonNameToFind = crewIdToButtonName[crewId];
        }
        
        // First try CrewButton components (if they exist)
        CrewButton[] buttons = crewButtonsParent.GetComponentsInChildren<CrewButton>();
        foreach (var button in buttons)
        {
            if (button.crewId == crewId)
            {
                return button.transform;
            }
        }
        
        // Fallback: try CrewStatusIndicator components
        CrewStatusIndicator[] indicators = crewButtonsParent.GetComponentsInChildren<CrewStatusIndicator>();
        foreach (var indicator in indicators)
        {
            if (indicator.crewId == crewId)
            {
                return indicator.transform;
            }
        }
        
        // Search by GameObject name (use mapped name)
        Transform[] allChildren = crewButtonsParent.GetComponentsInChildren<Transform>();
        foreach (var child in allChildren)
        {
            if (child.name == buttonNameToFind)
            {
                return child;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Find the sprite view for a specific crew member by ID.
    /// Uses mapping dictionary to handle mismatched names.
    /// </summary>
    private Transform FindCrewSprite(string crewId)
    {
        if (crewSpritesParent == null) return null;
        
        // Check if there's a mapped sprite name for this crew ID
        string spriteNameToFind = crewId;
        if (crewIdToSpriteName.ContainsKey(crewId))
        {
            spriteNameToFind = crewIdToSpriteName[crewId];
        }
        
        // Look for CrewSpriteView components on children
        CrewSpriteView[] spriteViews = crewSpritesParent.GetComponentsInChildren<CrewSpriteView>();
        foreach (var spriteView in spriteViews)
        {
            if (spriteView.crewId == crewId)
            {
                return spriteView.transform;
            }
        }
        
        // Search by GameObject name (use mapped name)
        Transform[] allChildren = crewSpritesParent.GetComponentsInChildren<Transform>();
        foreach (var child in allChildren)
        {
            if (child.name == spriteNameToFind)
            {
                return child;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Manually enable/disable the line (useful for debugging or manual control).
    /// </summary>
    public void SetLineEnabled(bool enabled)
    {
        if (lineImage != null)
        {
            lineImage.enabled = enabled;
        }
    }
}
