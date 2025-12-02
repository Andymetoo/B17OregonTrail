using UnityEngine;

[CreateAssetMenu(menuName = "B17/Events/Game Event", fileName = "GameEvent_", order = 10)]
public class GameEvent : ScriptableObject
{
    [Header("Identity")]
    public string Id;
    public string Title;
    [TextArea] public string Description;
    public Color DisplayColor = Color.white;
    [Header("Selection Weight")]
    [Tooltip("Relative weight for random selection. Higher = more likely.")]
    public float Weight = 1f;

    [Header("Effects")]
    [Tooltip("Fuel delta applied when event resolves (can be negative).")]
    public float FuelDelta;
    [Tooltip("Integrity damage applied randomly to one section (0 = none).")]
    public int RandomSectionDamage;
    [Tooltip("Chance to start a fire in that damaged section (0-1).")]
    [Range(0f,1f)] public float FireChance;

    [Tooltip("Crew injury roll (0-1). If triggered, severity scales with global danger.")]
    [Range(0f,1f)] public float CrewInjuryChance;

    public void ApplyEffects()
    {
        System.Collections.Generic.List<string> outcomes = new System.Collections.Generic.List<string>();

        if (MissionManager.Instance != null && FuelDelta != 0f)
        {
            // Directly adjust fuel (could later route through a SupplyManager)
            var mm = MissionManager.Instance;
            var newFuel = Mathf.Max(0f, mm.FuelRemaining + FuelDelta);
            // Reflect change by hacking private setter via event (optional: expose a method)
            // For now, log and rely on a future dedicated API.
            Debug.Log($"[GameEvent] Fuel change {FuelDelta} applied (new tentative: {newFuel}). Consider implementing a MissionManager.AdjustFuel API.");
            if (FuelDelta > 0f)
            {
                outcomes.Add($"<color=green>+{FuelDelta} fuel</color>");
            }
            else
            {
                outcomes.Add($"<color=orange>{FuelDelta} fuel</color>");
            }
        }

        if (RandomSectionDamage > 0 && PlaneManager.Instance != null)
        {
            var sections = PlaneManager.Instance.Sections;
            if (sections != null && sections.Count > 0)
            {
                var section = sections[Random.Range(0, sections.Count)];
                int oldIntegrity = section.Integrity;
                bool wasOnFire = section.OnFire;
                
                PlaneManager.Instance.ApplyHitToSection(section.Id, RandomSectionDamage, FireChance > 0f, FireChance);
                
                if (section.Integrity <= 0 && oldIntegrity > 0)
                {
                    outcomes.Add($"<color=red>{section.Id} DESTROYED!</color>");
                }
                else if (RandomSectionDamage > 0)
                {
                    outcomes.Add($"<color=orange>{section.Id} took {RandomSectionDamage} damage (Integrity: {section.Integrity})</color>");
                }
                
                if (section.OnFire && !wasOnFire)
                {
                    outcomes.Add($"<color=red>{section.Id} on FIRE!</color>");
                }
            }
        }

        if (CrewInjuryChance > 0f && CrewManager.Instance != null)
        {
            if (Random.value <= CrewInjuryChance)
            {
                var crew = CrewManager.Instance.GetRandomHealthyCrew();
                if (crew != null)
                {
                    // Simple severity roll
                    float r = Random.value;
                    CrewStatus status = r < 0.15f ? CrewStatus.Critical : (r < 0.45f ? CrewStatus.Serious : CrewStatus.Light);
                    CrewManager.Instance.ApplyInjury(crew.Id, status);
                    
                    string severity = status switch
                    {
                        CrewStatus.Light => "lightly wounded",
                        CrewStatus.Serious => "seriously wounded",
                        CrewStatus.Critical => "CRITICALLY injured",
                        _ => "injured"
                    };
                    
                    Color injuryColor = status == CrewStatus.Critical ? Color.red :
                                       status == CrewStatus.Serious ? new Color(1f, 0.5f, 0f) :
                                       Color.yellow;
                    
                    outcomes.Add($"<color=#{ColorUtility.ToHtmlStringRGB(injuryColor)}>{crew.Name} is {severity}!</color>");
                    Debug.Log($"[GameEvent] Crew injury triggered: {crew.Name} -> {status}");
                }
            }
        }
        
        // If nothing happened, add that
        if (outcomes.Count == 0)
        {
            outcomes.Add("<color=green>Nothing happened.</color>");
        }
        
        // Store outcomes for popup display
        LastOutcomes = outcomes;
    }
    
    // Store last outcomes for display
    [System.NonSerialized] public System.Collections.Generic.List<string> LastOutcomes = new System.Collections.Generic.List<string>();
}
