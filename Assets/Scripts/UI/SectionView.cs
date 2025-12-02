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

    [Header("Visual States")]
    public Color healthyColor = new Color(0f, 0.8f, 0f);      // Green - full integrity
    public Color criticalColor = new Color(0.9f, 0f, 0f);     // Red - near destroyed
    public Color fireColor = new Color(1f, 0.3f, 0f);         // Bright orange - on fire
    [Tooltip("Section integrity at full health (usually 100)")]
    public int maxIntegrity = 100;
    [Tooltip("Section integrity when destroyed (usually 0)")]
    public int minIntegrity = 0;

    void Update()
    {
        if (PlaneManager.Instance == null || image == null) return;

        var section = PlaneManager.Instance.GetSection(sectionId);
        if (section == null) return;

        // Priority: Fire overrides everything, then gradient damage tint
        if (section.OnFire)
        {
            image.color = fireColor;
        }
        else
        {
            // Gradient from healthy (green) at max integrity to critical (red) at min integrity
            float integrityFraction = Mathf.InverseLerp(minIntegrity, maxIntegrity, section.Integrity);
            image.color = Color.Lerp(criticalColor, healthyColor, integrityFraction);
        }
    }
}