using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Provides visual feedback for engine state by tinting button colors.
/// Shows integrity level, fire state, and feathered status.
/// Attach to engine buttons.
/// </summary>
public class EngineView : MonoBehaviour, IPointerClickHandler
{
    [Header("Engine Configuration")]
    public string engineId; // "Engine1", "Engine2", "Engine3", "Engine4"
    public Image image;
    
    [Header("Fire Graphics")]
    [Tooltip("UI GameObject showing fire graphic - will be enabled/disabled based on engine fire state.")]
    public GameObject fireGraphic;
    
    [Header("Feathered Indicator")]
    [Tooltip("UI GameObject showing feathered indicator - will be enabled/disabled based on feathered state.")]
    public GameObject featheredIndicator;

    [Header("Visual States")]
    public Color operationalColor = new Color(0f, 0.8f, 0f);      // Green - full integrity
    public Color damagedColor = new Color(1f, 0.7f, 0f);          // Orange - damaged
    public Color criticalColor = new Color(0.9f, 0f, 0f);         // Red - near destroyed
    public Color destroyedColor = new Color(0.3f, 0.3f, 0.3f);    // Gray - destroyed
    public Color fireColor = new Color(1f, 0.3f, 0f);             // Bright orange - on fire
    public Color featheredColor = new Color(0.4f, 0.4f, 0.6f);    // Blue-gray - feathered
    [Tooltip("Engine integrity at full health (100)")]
    public int maxIntegrity = 100;
    [Tooltip("Engine integrity threshold for damaged status (75)")]
    public int damagedThreshold = 75;
    
    [Header("Damage Blink Effect")]
    public float blinkDuration = 0.5f; // How long to blink when hit
    public Color blinkColor = Color.white; // Flash color
    private float blinkTimer = 0f;
    private int lastKnownIntegrity = -1;

    void Update()
    {
        if (PlaneManager.Instance == null || image == null) return;

        var engine = PlaneManager.Instance.GetEngine(engineId);
        if (engine == null) return;
        
        // Detect damage (integrity decreased)
        if (lastKnownIntegrity > 0 && engine.Integrity < lastKnownIntegrity)
        {
            blinkTimer = blinkDuration; // Start blink
        }
        lastKnownIntegrity = engine.Integrity;
        
        // Update blink timer
        if (blinkTimer > 0f)
        {
            blinkTimer -= Time.deltaTime;
        }
        
        // Update fire graphic visibility
        if (fireGraphic != null)
        {
            fireGraphic.SetActive(engine.OnFire);
        }
        
        // Update feathered indicator visibility
        if (featheredIndicator != null)
        {
            featheredIndicator.SetActive(engine.IsFeathered);
        }

        // Priority: Blink > Fire > Feathered > Status gradient
        if (blinkTimer > 0f)
        {
            // Flash white when damaged
            image.color = blinkColor;
        }
        else if (engine.OnFire)
        {
            image.color = fireColor;
        }
        else if (engine.IsFeathered)
        {
            image.color = featheredColor;
        }
        else
        {
            // Color based on status and integrity
            if (engine.Integrity <= 0)
            {
                image.color = destroyedColor; // Gray
            }
            else if (engine.Integrity < damagedThreshold)
            {
                // Gradient from critical (red) at 0 to damaged (orange) at threshold
                float fraction = Mathf.InverseLerp(0, damagedThreshold, engine.Integrity);
                image.color = Color.Lerp(criticalColor, damagedColor, fraction);
            }
            else
            {
                // Gradient from damaged (orange) at threshold to operational (green) at max
                float fraction = Mathf.InverseLerp(damagedThreshold, maxIntegrity, engine.Integrity);
                image.color = Color.Lerp(damagedColor, operationalColor, fraction);
            }
        }
    }
    
    /// <summary>
    /// Called when engine button is clicked. Notify OrdersUIController.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (OrdersUIController.Instance != null)
        {
            OrdersUIController.Instance.OnEngineClicked(engineId);
        }
    }
}
