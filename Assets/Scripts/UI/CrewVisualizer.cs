using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Visualizes a crew member sprite and smoothly moves it between positions
/// </summary>
[RequireComponent(typeof(Image))]
public class CrewVisualizer : MonoBehaviour
{
    [Header("Crew Reference")]
    [SerializeField] private string crewId;
    
    [Header("Sprite States")]
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite movingSprite;
    [SerializeField] private Sprite workingSprite;
    
    [Header("Movement")]
    [SerializeField] private float smoothing = 8f;
    
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
        if (CrewManager.Instance != null)
        {
            crew = CrewManager.Instance.GetCrewById(crewId);
            if (crew != null)
            {
                rectTransform.anchoredPosition = crew.CurrentPosition;
                UpdateSprite();
            }
        }
    }
    
    private void Update()
    {
        if (crew == null) return;
        
        // Smoothly lerp toward crew's current position
        Vector2 currentPos = rectTransform.anchoredPosition;
        Vector2 targetPos = crew.CurrentPosition;
        
        if (Vector2.Distance(currentPos, targetPos) > 0.5f)
        {
            rectTransform.anchoredPosition = Vector2.Lerp(currentPos, targetPos, Time.deltaTime * smoothing);
            // Optional verbose logging using CrewManager toggle
            if (CrewManager.Instance != null && CrewManager.Instance.verboseLogging)
            {
                Debug.Log($"[CrewVisualizer] {crew.Id} lerp pos={rectTransform.anchoredPosition} target={targetPos} state={crew.VisualState}");
            }
        }
        else
        {
            rectTransform.anchoredPosition = targetPos;
            if (CrewManager.Instance != null && CrewManager.Instance.verboseLogging)
            {
                Debug.Log($"[CrewVisualizer] {crew.Id} snap target={targetPos} state={crew.VisualState}");
            }
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
        
        // Fade if dead/unconscious
        image.color = (crew.Status == CrewStatus.Dead || crew.Status == CrewStatus.Unconscious) 
            ? new Color(1f, 1f, 1f, 0.3f) 
            : Color.white;
    }
    
    public void SetCrewId(string id)
    {
        crewId = id;
        if (CrewManager.Instance != null)
        {
            crew = CrewManager.Instance.GetCrewById(id);
            if (crew != null)
            {
                rectTransform.anchoredPosition = crew.CurrentPosition;
                UpdateSprite();
            }
        }
    }
}
