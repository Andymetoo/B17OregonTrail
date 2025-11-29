using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Visualizes a crew member on screen with sprite representation.
/// Handles movement animation and sprite swapping based on crew state.
/// </summary>
[RequireComponent(typeof(Image))]
public class CrewVisualizer : MonoBehaviour
{
    [Header("Crew Reference")]
    [SerializeField] private string crewId;
    
    [Header("Sprite States")]
    [SerializeField] private Sprite idleSprite;      // Standing at station
    [SerializeField] private Sprite movingSprite;    // Walking/moving
    [SerializeField] private Sprite workingSprite;   // Performing action
    
    [Header("Movement Settings")]
    [SerializeField] private float smoothing = 8f;   // How smoothly crew moves (higher = snappier)
    
    private Image image;
    private RectTransform rectTransform;
    private CrewMember crew;
    
    private void Awake()
    {
        image = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
    }
    
    private void Start()
    {
        // Delay initialization to ensure CrewManager.Start() has run first
        StartCoroutine(InitializeAfterCrewManager());
    }
    
    private System.Collections.IEnumerator InitializeAfterCrewManager()
    {
        // Wait one frame to ensure CrewManager.Start() has initialized positions
        yield return null;
        
        if (CrewManager.Instance != null)
        {
            crew = CrewManager.Instance.GetCrewById(crewId);
            if (crew != null)
            {
                // Crew position should already be initialized by CrewManager.Start()
                // Just sync our visual transform to match
                rectTransform.anchoredPosition = crew.CurrentPosition;
                UpdateSprite();
                Debug.Log($"[CrewVisualizer] {crew.Name} visual initialized at {crew.CurrentPosition} (rectTransform.anchoredPosition = {rectTransform.anchoredPosition})");
                Debug.Log($"[CrewVisualizer] {crew.Name} parent: {rectTransform.parent.name}, anchors: min={rectTransform.anchorMin}, max={rectTransform.anchorMax}");
            }
            else
            {
                Debug.LogError($"[CrewVisualizer] Crew '{crewId}' not found in CrewManager!");
            }
        }
        else
        {
            Debug.LogError("[CrewVisualizer] CrewManager.Instance is null!");
        }
    }
    
    private void Update()
    {
        if (crew == null) return;
        
        // Smoothly move crew toward their current target position
        Vector2 targetPos = crew.CurrentPosition;
        Vector2 currentPos = rectTransform.anchoredPosition;
        
        float distance = Vector2.Distance(currentPos, targetPos);
        
        if (distance > 0.5f)
        {
            Vector2 newPos = Vector2.Lerp(currentPos, targetPos, Time.deltaTime * smoothing);
            rectTransform.anchoredPosition = newPos;
            
            // Debug visualization
            if (distance > 50f) // Suspiciously far
            {
                Debug.LogWarning($"[CrewVisualizer] {crew.Name} moving large distance: from {currentPos} to {targetPos} (distance: {distance:F1})");
            }
        }
        else
        {
            rectTransform.anchoredPosition = targetPos;
        }
        
        UpdateSprite();
    }
    
    private void UpdateSprite()
    {
        if (crew == null || image == null) return;
        
        Sprite newSprite = crew.VisualState switch
        {
            CrewVisualState.IdleAtStation => idleSprite,
            CrewVisualState.Moving => movingSprite,
            CrewVisualState.Working => workingSprite,
            _ => idleSprite
        };
        
        if (newSprite != null && image.sprite != newSprite)
        {
            image.sprite = newSprite;
        }
        
        // Optional: Fade out if dead/unconscious
        if (crew.Status == CrewStatus.Dead || crew.Status == CrewStatus.Unconscious)
        {
            image.color = new Color(1f, 1f, 1f, 0.3f);
        }
        else
        {
            image.color = Color.white;
        }
    }
    
    /// <summary>
    /// Set the crew ID this visualizer represents (for dynamic setup)
    /// </summary>
    public void SetCrewId(string id)
    {
        crewId = id;
        if (CrewManager.Instance != null)
        {
            crew = CrewManager.Instance.GetCrewById(id);
            if (crew != null)
            {
                crew.CurrentPosition = crew.HomePosition;
                rectTransform.anchoredPosition = crew.HomePosition;
                UpdateSprite();
            }
        }
    }
}
