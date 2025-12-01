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
    [SerializeField] private Sprite incapacitatedSprite; // used for Unconscious/Dead
    
    [Header("Movement")]
    [SerializeField] private float smoothing = 8f;

    [Header("Tinting")]
    [SerializeField] private Color baseColor = Color.white;
    [SerializeField] private Color workingTint = new Color(1f, 0.9f, 0.2f, 0.4f); // subtle warm tint
    [SerializeField] private Color lightInjuryTint = new Color(1f, 0.5f, 0f, 0.35f); // orange
    [SerializeField] private Color seriousInjuryTint = new Color(1f, 0.25f, 0f, 0.4f); // deeper orange
    [SerializeField] private Color criticalInjuryTint = new Color(1f, 0f, 0f, 0.45f); // red
    [SerializeField] private Color deadTint = new Color(0.3f, 0.3f, 0.3f, 0.5f); // gray
    [SerializeField] private bool additiveTints = true; // additive vs multiply
    [SerializeField] private bool dimWhenIncapacitated = true;
    [SerializeField] [Range(0f, 1f)] private float incapacitatedAlpha = 0.3f;
    
    private Image image;
    private RectTransform rectTransform;
    private CrewMember crew;
    private bool snappedToStart = false;
    
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
            // Do not set anchoredPosition here to avoid snapping to (0,0) before positions init
        }
    }
    
    private void Update()
    {
        if (crew == null) return;
        
        // Wait until CrewManager has initialized positions, then snap once to avoid (0,0)
        if (!snappedToStart)
        {
            if (CrewManager.Instance != null && CrewManager.Instance.PositionsInitialized)
            {
                rectTransform.anchoredPosition = crew.CurrentPosition;
                snappedToStart = true;
                UpdateSprite();
            }
            return; // Skip lerp until we've snapped to the correct start
        }

        // Smoothly lerp toward crew's current position thereafter
        Vector2 currentPos = rectTransform.anchoredPosition;
        Vector2 targetPos = crew.CurrentPosition;

        if (Vector2.Distance(currentPos, targetPos) > 0.5f)
        {
            rectTransform.anchoredPosition = Vector2.Lerp(currentPos, targetPos, Time.deltaTime * smoothing);
        }
        else
        {
            rectTransform.anchoredPosition = targetPos;
        }
        
        // Flip horizontally based on travel direction (keep last facing when nearly stationary)
        float dx = targetPos.x - currentPos.x;
        if (Mathf.Abs(dx) > 0.01f)
        {
            var scale = rectTransform.localScale;
            scale.x = dx >= 0 ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            rectTransform.localScale = scale;
        }

        UpdateSprite();
    }
    
    private void UpdateSprite()
    {
        if (crew == null || image == null) return;

        // Treat any non-healthy status as incapacitated for visuals
        bool incapacitated = crew.Status != CrewStatus.Healthy;

        // Choose sprite
        Sprite newSprite;
        if (incapacitated && incapacitatedSprite != null)
        {
            newSprite = incapacitatedSprite;
        }
        else
        {
            newSprite = crew.VisualState switch
            {
                CrewVisualState.IdleAtStation => idleSprite,
                CrewVisualState.Moving => movingSprite,
                CrewVisualState.Working => workingSprite,
                _ => idleSprite
            };
        }
        if (newSprite != null && image.sprite != newSprite)
        {
            image.sprite = newSprite;
        }

        // Compose tint
        Color c = baseColor;
        if (crew.VisualState == CrewVisualState.Working)
            c = Blend(c, workingTint, additiveTints);

        switch (crew.Status)
        {
            case CrewStatus.Light:
                c = Blend(c, lightInjuryTint, additiveTints);
                break;
            case CrewStatus.Serious:
                c = Blend(c, seriousInjuryTint, additiveTints);
                break;
            case CrewStatus.Critical:
                c = Blend(c, criticalInjuryTint, additiveTints);
                break;
            case CrewStatus.Unconscious:
            case CrewStatus.Dead:
                c = Blend(c, deadTint, additiveTints);
                break;
        }

        if (incapacitated && dimWhenIncapacitated)
        {
            c.a = incapacitatedAlpha;
        }
        image.color = c;
    }

    private static Color Blend(Color baseC, Color tint, bool additive)
    {
        if (tint.a <= 0f) return baseC;
        if (additive)
        {
            // Additive using tint alpha as intensity; do not increase overall alpha here
            float r = Mathf.Clamp01(baseC.r + tint.r * tint.a);
            float g = Mathf.Clamp01(baseC.g + tint.g * tint.a);
            float b = Mathf.Clamp01(baseC.b + tint.b * tint.a);
            return new Color(r, g, b, baseC.a);
        }
        else
        {
            // Multiplicative blend (modulate)
            float r = baseC.r * Mathf.Lerp(1f, tint.r, tint.a);
            float g = baseC.g * Mathf.Lerp(1f, tint.g, tint.a);
            float b = baseC.b * Mathf.Lerp(1f, tint.b, tint.a);
            return new Color(r, g, b, baseC.a);
        }
    }
    
    public void SetCrewId(string id)
    {
        crewId = id;
        if (CrewManager.Instance != null)
        {
            crew = CrewManager.Instance.GetCrewById(id);
            snappedToStart = false; // force a fresh snap when positions are ready
            if (crew != null && CrewManager.Instance.PositionsInitialized)
            {
                rectTransform.anchoredPosition = crew.CurrentPosition;
                snappedToStart = true;
                UpdateSprite();
            }
        }
    }
}
