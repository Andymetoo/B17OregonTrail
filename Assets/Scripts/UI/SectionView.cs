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
    public Color healthyColor = new Color(0f, 0.8f, 0f);      // Green - section is fine
    public Color damagedColor = new Color(0.9f, 0.8f, 0f);    // Yellow - section is damaged
    public Color fireColor = new Color(0.9f, 0.2f, 0f);       // Red - section is on fire

    void Update()
    {
        if (PlaneManager.Instance == null || image == null) return;

        var section = PlaneManager.Instance.GetSection(sectionId);
        if (section == null) return;

        // Priority: Fire > Damage > Healthy
        if (section.OnFire)
            image.color = fireColor;
        else if (section.Integrity < 100)
            image.color = damagedColor;
        else
            image.color = healthyColor;
    }
}