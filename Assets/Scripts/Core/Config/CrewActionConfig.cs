using UnityEngine;

/// <summary>
/// Configuration for crew action durations, success chances, and repair amounts.
/// Base actions have failure chances; consumables guarantee success and are faster.
/// </summary>
[CreateAssetMenu(menuName = "B17/Config/Crew Action Config", fileName = "CrewActionConfig", order = 100)]
public class CrewActionConfig : ScriptableObject
{
    public static CrewActionConfig Instance { get; private set; }

    [Header("Base Medical Treatment")]
    [Tooltip("Duration in seconds for base medical treatment action.")]
    public float baseMedicalDuration = 10f;
    
    [Tooltip("Chance of success for base medical treatment (0-1). Failure means no healing occurs.")]
    [Range(0f, 1f)]
    public float baseMedicalSuccessChance = 0.65f;
    
    [Header("Consumable: Medical Kit")]
    [Tooltip("Duration in seconds when using a medical kit.")]
    public float medkitDuration = 3f;
    
    [Tooltip("Medical kits always succeed (guaranteed healing).")]
    [Range(0f, 1f)]
    public float medkitSuccessChance = 1.0f;
    
    [Header("Base Repair")]
    [Tooltip("Duration in seconds for base repair action.")]
    public float baseRepairDuration = 10f;
    
    [Tooltip("Chance of success for base repair (0-1). Failure means no repair occurs.")]
    [Range(0f, 1f)]
    public float baseRepairSuccessChance = 0.70f;
    
    [Tooltip("Minimum integrity points restored by base repair (if successful).")]
    public int baseRepairAmountMin = 10;
    
    [Tooltip("Maximum integrity points restored by base repair (if successful).")]
    public int baseRepairAmountMax = 30;
    
    [Header("Consumable: Repair Kit")]
    [Tooltip("Duration in seconds when using a repair kit.")]
    public float repairKitDuration = 4f;
    
    [Tooltip("Repair kits always succeed (guaranteed repair).")]
    [Range(0f, 1f)]
    public float repairKitSuccessChance = 1.0f;
    
    [Tooltip("Minimum integrity points restored by repair kit.")]
    public int repairKitAmountMin = 30;
    
    [Tooltip("Maximum integrity points restored by repair kit.")]
    public int repairKitAmountMax = 50;
    
    [Header("Base Fire Extinguishing")]
    [Tooltip("Duration in seconds for base fire extinguishing action.")]
    public float baseExtinguishDuration = 8f;
    
    [Tooltip("Chance of success for base fire extinguishing (0-1). Failure means fire continues burning.")]
    [Range(0f, 1f)]
    public float baseExtinguishSuccessChance = 0.75f;
    
    [Header("Consumable: Fire Extinguisher")]
    [Tooltip("Duration in seconds when using a fire extinguisher.")]
    public float fireExtinguisherDuration = 2f;
    
    [Tooltip("Fire extinguishers always succeed (guaranteed extinguish).")]
    [Range(0f, 1f)]
    public float fireExtinguisherSuccessChance = 1.0f;

    private void OnEnable()
    {
        Instance = this;
    }

    /// <summary>
    /// Sample a random repair amount using a bell curve (normal distribution approximation).
    /// Uses Box-Muller transform for Gaussian distribution centered between min/max.
    /// </summary>
    public int SampleRepairAmount(int min, int max)
    {
        if (min >= max) return min;
        
        float mean = (min + max) / 2f;
        float stdDev = (max - min) / 6f; // ~99.7% of values fall within min-max (3 sigma rule)
        
        // Box-Muller transform for normal distribution
        float u1 = Random.value;
        float u2 = Random.value;
        float randStdNormal = Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Sin(2f * Mathf.PI * u2);
        float randNormal = mean + stdDev * randStdNormal;
        
        // Clamp to [min, max] and round
        return Mathf.Clamp(Mathf.RoundToInt(randNormal), min, max);
    }
}
