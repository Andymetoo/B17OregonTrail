using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Provides visual feedback for system state by tinting button colors.
/// Shows integrity level, damaged/destroyed states.
/// Attach to system buttons (guns, radio, navigator station, bombsight).
/// </summary>
public class SystemView : MonoBehaviour, IPointerClickHandler
{
    [Header("System Configuration")]
    public string systemId; // "TopTurret", "Radio", "NavigatorStation", etc.
    public Image image;
    
    [Header("State Graphics")]
    [Tooltip("Sprite to display when system is operational (optional - uses default if not set).")]
    public Sprite operationalSprite;
    [Tooltip("Sprite to display when system is damaged (replaces button image).")]
    public Sprite damagedSprite;
    [Tooltip("Sprite to display when system is destroyed (replaces button image, button grayed out).")]
    public Sprite destroyedSprite;

    [Header("Visual States")]
    public Color operationalColor = new Color(0f, 0.8f, 0f);      // Green - full integrity
    public Color damagedColor = new Color(1f, 0.7f, 0f);          // Orange - damaged
    public Color criticalColor = new Color(0.9f, 0f, 0f);         // Red - near destroyed
    public Color destroyedColor = new Color(0.3f, 0.3f, 0.3f);    // Gray - destroyed
    
    [Tooltip("System integrity at full health (100)")]
    public int maxIntegrity = 100;
    [Tooltip("System integrity threshold for damaged status (75)")]
    public int damagedThreshold = 75;
    
    [Header("Damage Blink Effect")]
    public float blinkDuration = 0.5f; // How long to blink when hit
    public Color blinkColor = Color.white; // Flash color
    private float blinkTimer = 0f;
    private int lastKnownIntegrity = -1;
    private SystemStatus lastKnownStatus = SystemStatus.Operational;

    void Start()
    {
        // Set initial sprite based on current system status
        if (PlaneManager.Instance != null && image != null)
        {
            var system = PlaneManager.Instance.GetSystem(systemId);
            if (system != null)
            {
                UpdateSprite(system.Status);
                lastKnownStatus = system.Status;
                lastKnownIntegrity = system.Integrity;
            }
        }
    }

    void Update()
    {
        if (PlaneManager.Instance == null || image == null) return;

        var system = PlaneManager.Instance.GetSystem(systemId);
        if (system == null) return;
        
        // Detect damage (integrity decreased)
        if (lastKnownIntegrity > 0 && system.Integrity < lastKnownIntegrity)
        {
            blinkTimer = blinkDuration; // Start blink
        }
        lastKnownIntegrity = system.Integrity;
        
        // Update sprite when status changes
        if (system.Status != lastKnownStatus)
        {
            UpdateSprite(system.Status);
            lastKnownStatus = system.Status;
        }
        
        // Update blink timer
        if (blinkTimer > 0f)
        {
            blinkTimer -= Time.deltaTime;
        }

        // Priority: Blink > Status-based coloring
        if (blinkTimer > 0f)
        {
            // Flash white when damaged
            image.color = blinkColor;
        }
        else
        {
            // Color based on status and integrity
            if (system.Status == SystemStatus.Destroyed || system.Integrity <= 0)
            {
                image.color = destroyedColor; // Gray
            }
            else if (system.Integrity < damagedThreshold)
            {
                // Gradient from critical (red) at 0 to damaged (orange) at threshold
                float fraction = Mathf.InverseLerp(0, damagedThreshold, system.Integrity);
                image.color = Color.Lerp(criticalColor, damagedColor, fraction);
            }
            else
            {
                // Gradient from damaged (orange) at threshold to operational (green) at max
                float fraction = Mathf.InverseLerp(damagedThreshold, maxIntegrity, system.Integrity);
                image.color = Color.Lerp(damagedColor, operationalColor, fraction);
            }
        }
    }
    
    /// <summary>
    /// Update the sprite based on system status.
    /// </summary>
    private void UpdateSprite(SystemStatus status)
    {
        if (image == null) return;
        
        switch (status)
        {
            case SystemStatus.Operational:
                if (operationalSprite != null)
                    image.sprite = operationalSprite;
                break;
            case SystemStatus.Damaged:
                if (damagedSprite != null)
                    image.sprite = damagedSprite;
                break;
            case SystemStatus.Destroyed:
                if (destroyedSprite != null)
                    image.sprite = destroyedSprite;
                break;
        }
    }
    
    /// <summary>
    /// Called when system button is clicked. Notify OrdersUIController.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (OrdersUIController.Instance != null)
        {
            OrdersUIController.Instance.OnSystemClicked(systemId);
        }
    }
}
