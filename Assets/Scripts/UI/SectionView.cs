using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Provides visual feedback for section state by tinting button colors.
/// Attach to section buttons alongside TargetClick component.
/// </summary>
public class SectionView : MonoBehaviour
{
    [Header("Section Configuration")]
    public string sectionId;
    public Image image;
    
    [Header("Fire Graphics")]
    [Tooltip("UI GameObject showing fire graphic - will be enabled/disabled based on section fire state.")]
    public GameObject fireGraphic;

    [Header("Visual States")]
    public Color healthyColor = new Color(0f, 0.8f, 0f);      // Green - full integrity
    public Color criticalColor = new Color(0.9f, 0f, 0f);     // Red - near destroyed
    public Color fireColor = new Color(1f, 0.3f, 0f);         // Bright orange - on fire
    [Tooltip("Section integrity at full health (usually 100)")]
    public int maxIntegrity = 100;
    [Tooltip("Section integrity when destroyed (usually 0)")]
    public int minIntegrity = 0;
    
    [Header("Damage Blink Effect")]
    public float blinkDuration = 0.5f; // How long to blink when hit
    public Color blinkColor = Color.white; // Flash color
    private float blinkTimer = 0f;
    private int lastKnownIntegrity = -1;

    void Update()
    {
        if (PlaneManager.Instance == null || image == null) return;

        var section = PlaneManager.Instance.GetSection(sectionId);
        if (section == null) return;
        
        // Detect damage (integrity decreased)
        if (lastKnownIntegrity > 0 && section.Integrity < lastKnownIntegrity)
        {
            blinkTimer = blinkDuration; // Start blink
        }
        lastKnownIntegrity = section.Integrity;
        
        // Update blink timer
        if (blinkTimer > 0f)
        {
            blinkTimer -= Time.deltaTime;
        }
        
        // Update fire graphic visibility
        if (fireGraphic != null)
        {
            fireGraphic.SetActive(section.OnFire);
        }

        // Priority: Blink > Fire > Gradient damage tint
        if (blinkTimer > 0f)
        {
            // Flash white when damaged
            image.color = blinkColor;
        }
        else if (section.OnFire)
        {
            image.color = fireColor;
        }
        else
        {
            // Gradient from critical (red) at 0 to healthy (green) at max
            // integrityFraction = 0.0 when integrity is 0 (critical)
            // integrityFraction = 1.0 when integrity is 100 (healthy)
            float integrityFraction = Mathf.InverseLerp(minIntegrity, maxIntegrity, section.Integrity);
            image.color = Color.Lerp(criticalColor, healthyColor, integrityFraction);
        }
    }
}