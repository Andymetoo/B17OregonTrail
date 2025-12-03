using UnityEngine;

/// <summary>
/// Animates a background image's Y position based on plane altitude.
/// As altitude decreases, the background moves down to simulate approaching ground.
/// </summary>
public class AltitudeBackground : MonoBehaviour
{
    [Header("Altitude Mapping")]
    [Tooltip("Altitude at which background is at high Y position (feet).")]
    public float maxAltitude = 25000f;
    
    [Tooltip("Altitude at which background is at low Y position (feet).")]
    public float minAltitude = 0f;
    
    [Header("Y Position Mapping")]
    [Tooltip("Y position when at max altitude (high in sky).")]
    public float maxAltitudeYPosition = 0f;
    
    [Tooltip("Y position when at min altitude (near ground).")]
    public float minAltitudeYPosition = -500f;
    
    [Header("Animation")]
    [Tooltip("Smoothing speed for position changes (0 = instant, higher = slower).")]
    public float smoothSpeed = 2f;
    
    private RectTransform rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogError("[AltitudeBackground] No RectTransform found! This script requires a UI element.");
        }
    }

    void Update()
    {
        if (rectTransform == null || PlaneManager.Instance == null) return;

        // Get current altitude from PlaneManager
        float currentAltitude = PlaneManager.Instance.currentAltitudeFeet;
        
        // Normalize altitude to 0-1 range
        float altitudeFraction = Mathf.InverseLerp(minAltitude, maxAltitude, currentAltitude);
        
        // Calculate target Y position based on altitude
        float targetY = Mathf.Lerp(minAltitudeYPosition, maxAltitudeYPosition, altitudeFraction);
        
        // Smoothly move to target position
        Vector2 currentPos = rectTransform.anchoredPosition;
        float newY;
        if (smoothSpeed > 0f)
        {
            newY = Mathf.Lerp(currentPos.y, targetY, Time.deltaTime * smoothSpeed);
        }
        else
        {
            newY = targetY;
        }
        
        rectTransform.anchoredPosition = new Vector2(currentPos.x, newY);
    }
}
