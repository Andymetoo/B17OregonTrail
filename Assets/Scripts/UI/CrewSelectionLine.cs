using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Draws a visual line from a crew member's UI button to their sprite on the plane.
/// Helps players quickly identify which crew member they have selected.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
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
    
    [Tooltip("Width of the line")]
    public float lineWidth = 3f;
    
    [Tooltip("Material for the line (use a UI material)")]
    public Material lineMaterial;
    
    [Tooltip("Sorting order for the line (should be above plane but below UI)")]
    public int sortingOrder = 100;
    
    [Header("Animation (Optional)")]
    [Tooltip("Animate the line pulsing/fading")]
    public bool animateLine = true;
    
    [Tooltip("Speed of the pulse animation")]
    public float pulseSpeed = 2f;
    
    [Tooltip("Min/max alpha for pulse")]
    public float minAlpha = 0.4f;
    public float maxAlpha = 1f;
    
    private LineRenderer lineRenderer;
    private Canvas canvas;
    private Camera uiCamera;
    private float pulseTimer = 0f;
    
    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        
        // Configure LineRenderer for UI usage
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.useWorldSpace = true;
        
        if (lineMaterial != null)
        {
            lineRenderer.material = lineMaterial;
        }
        
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;
        
        // Set sorting for UI rendering
        lineRenderer.sortingLayerName = "UI"; // Make sure you have a "UI" sorting layer or change this
        lineRenderer.sortingOrder = sortingOrder;
        
        // Initially hide the line
        lineRenderer.enabled = false;
        
        // Find canvas and camera
        canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            uiCamera = canvas.worldCamera;
        }
        else if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = null; // Overlay mode uses screen space directly
        }
        else
        {
            uiCamera = Camera.main;
        }
    }
    
    private void Update()
    {
        if (ordersController == null)
        {
            lineRenderer.enabled = false;
            return;
        }
        
        string selectedCrewId = ordersController.SelectedCrewId;
        
        // No crew selected - hide line
        if (string.IsNullOrEmpty(selectedCrewId))
        {
            lineRenderer.enabled = false;
            return;
        }
        
        // Find the button and sprite for this crew
        Transform buttonTransform = FindCrewButton(selectedCrewId);
        Transform spriteTransform = FindCrewSprite(selectedCrewId);
        
        if (buttonTransform == null || spriteTransform == null)
        {
            lineRenderer.enabled = false;
            return;
        }
        
        // Get world positions
        Vector3 buttonWorldPos = GetWorldPosition(buttonTransform);
        Vector3 spriteWorldPos = GetWorldPosition(spriteTransform);
        
        // Update line positions
        lineRenderer.SetPosition(0, buttonWorldPos);
        lineRenderer.SetPosition(1, spriteWorldPos);
        
        // Enable the line
        lineRenderer.enabled = true;
        
        // Optional: Animate the line
        if (animateLine)
        {
            pulseTimer += Time.deltaTime * pulseSpeed;
            float alpha = Mathf.Lerp(minAlpha, maxAlpha, (Mathf.Sin(pulseTimer) + 1f) / 2f);
            
            Color currentColor = lineColor;
            currentColor.a = alpha;
            lineRenderer.startColor = currentColor;
            lineRenderer.endColor = currentColor;
        }
    }
    
    /// <summary>
    /// Find the UI button for a specific crew member by ID.
    /// </summary>
    private Transform FindCrewButton(string crewId)
    {
        if (crewButtonsParent == null) return null;
        
        // Look for CrewStatusIndicator components on children
        CrewStatusIndicator[] indicators = crewButtonsParent.GetComponentsInChildren<CrewStatusIndicator>();
        foreach (var indicator in indicators)
        {
            if (indicator.crewId == crewId)
            {
                return indicator.transform;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Find the sprite view for a specific crew member by ID.
    /// </summary>
    private Transform FindCrewSprite(string crewId)
    {
        if (crewSpritesParent == null) return null;
        
        // Look for CrewSpriteView components on children
        CrewSpriteView[] spriteViews = crewSpritesParent.GetComponentsInChildren<CrewSpriteView>();
        foreach (var spriteView in spriteViews)
        {
            if (spriteView.crewId == crewId)
            {
                return spriteView.transform;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Convert a RectTransform position to world space for LineRenderer.
    /// </summary>
    private Vector3 GetWorldPosition(Transform target)
    {
        if (target == null) return Vector3.zero;
        
        RectTransform rectTransform = target as RectTransform;
        
        if (rectTransform != null)
        {
            // For UI elements, convert from canvas space to world space
            Vector3 worldPos;
            
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                // In overlay mode, convert screen position to world
                Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(null, rectTransform.position);
                worldPos = rectTransform.position;
            }
            else if (canvas.renderMode == RenderMode.ScreenSpaceCamera && uiCamera != null)
            {
                // In camera mode, the position is already in world space relative to camera
                worldPos = rectTransform.position;
            }
            else
            {
                // World space canvas
                worldPos = rectTransform.position;
            }
            
            return worldPos;
        }
        
        // Non-UI transform, just use world position
        return target.position;
    }
    
    /// <summary>
    /// Manually enable/disable the line (useful for debugging or manual control).
    /// </summary>
    public void SetLineEnabled(bool enabled)
    {
        if (lineRenderer != null)
        {
            lineRenderer.enabled = enabled;
        }
    }
}
