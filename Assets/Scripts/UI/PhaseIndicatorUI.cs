using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Visual indicator showing the current hazard phase (Flak/Fighters).
/// Shows different images based on ChaosSimulator.CurrentPhase.
/// Also flashes an attack indicator whenever a hazard event fires.
/// </summary>
public class PhaseIndicatorUI : MonoBehaviour
{
    [Header("Phase Images")]
    [Tooltip("Image shown during Flak phase")]
    public Sprite flakSprite;
    [Tooltip("Image shown during Fighters phase")]
    public Sprite fightersSprite;
    
    [Header("UI References")]
    [Tooltip("The Image component that will display phase sprites")]
    public Image phaseImage;
    [Tooltip("Separate Image that flashes briefly on each attack (optional)")]
    public Image attackFlashImage;
    
    [Header("Display Settings")]
    [Tooltip("If true, hide the image during Cruise phase; if false, show empty/transparent")]
    public bool hideOnCruise = true;
    [Tooltip("Color tint applied to the image (useful for debugging or styling)")]
    public Color imageTint = Color.white;
    
    [Header("Attack Flash Settings")]
    [Tooltip("Sprite shown during attack flash")]
    public Sprite attackFlashSprite;
    [Tooltip("How long the attack flash is visible (seconds)")]
    public float flashDuration = 0.5f;
    [Tooltip("Color tint for attack flash")]
    public Color flashTint = Color.red;
    
    private float _flashTimer = 0f;
    
    private void Start()
    {
        if (phaseImage != null)
        {
            phaseImage.color = imageTint;
        }
        
        if (attackFlashImage != null)
        {
            attackFlashImage.color = flashTint;
            attackFlashImage.enabled = false;
        }
        
        // Subscribe to hazard events
        if (ChaosSimulator.Instance != null)
        {
            ChaosSimulator.Instance.OnChaosEvent += HandleChaosEvent;
        }
    }
    
    private void OnDestroy()
    {
        if (ChaosSimulator.Instance != null)
        {
            ChaosSimulator.Instance.OnChaosEvent -= HandleChaosEvent;
        }
    }
    
    private void HandleChaosEvent(string eventMessage)
    {
        // Trigger flash whenever a hazard event occurs
        if (attackFlashImage != null)
        {
            _flashTimer = flashDuration;
            attackFlashImage.enabled = true;
        }
    }
    
    private void Update()
    {
        if (ChaosSimulator.Instance == null || phaseImage == null)
            return;
        
        // Update phase indicator
        var currentPhase = ChaosSimulator.Instance.CurrentPhase;
        
        switch (currentPhase)
        {
            case ChaosSimulator.HazardPhase.Cruise:
                if (hideOnCruise)
                {
                    phaseImage.enabled = false;
                }
                else
                {
                    phaseImage.enabled = true;
                    phaseImage.sprite = null;
                }
                break;
                
            case ChaosSimulator.HazardPhase.Flak:
                phaseImage.enabled = true;
                phaseImage.sprite = flakSprite;
                break;
                
            case ChaosSimulator.HazardPhase.Fighters:
                phaseImage.enabled = true;
                phaseImage.sprite = fightersSprite;
                break;
        }
        
        // Update attack flash timer
        if (_flashTimer > 0f)
        {
            _flashTimer -= Time.deltaTime;
            if (_flashTimer <= 0f && attackFlashImage != null)
            {
                attackFlashImage.enabled = false;
            }
        }
    }
}
