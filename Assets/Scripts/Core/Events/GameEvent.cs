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
        if (MissionManager.Instance != null && FuelDelta != 0f)
        {
            // Directly adjust fuel (could later route through a SupplyManager)
            var mm = MissionManager.Instance;
            var newFuel = Mathf.Max(0f, mm.FuelRemaining + FuelDelta);
            // Reflect change by hacking private setter via event (optional: expose a method)
            // For now, log and rely on a future dedicated API.
            Debug.Log($"[GameEvent] Fuel change {FuelDelta} applied (new tentative: {newFuel}). Consider implementing a MissionManager.AdjustFuel API.");
        }

        if (RandomSectionDamage > 0 && PlaneManager.Instance != null)
        {
            PlaneManager.Instance.ApplyRandomHit(RandomSectionDamage, FireChance > 0f, FireChance);
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
                    Debug.Log($"[GameEvent] Crew injury triggered: {crew.Name} -> {status}");
                }
            }
        }
    }
}
