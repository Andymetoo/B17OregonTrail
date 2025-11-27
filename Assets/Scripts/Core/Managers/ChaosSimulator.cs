using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Sandbox mode chaos generator - randomly damages sections, injures crew, and starts fires.
/// Perfect for testing crew management systems without needing mission nodes.
/// </summary>
public class ChaosSimulator : MonoBehaviour
{
    public static ChaosSimulator Instance { get; private set; }
    
    [Header("Chaos Settings")]
    [SerializeField] private bool enableChaos = true;
    [SerializeField] private float chaosInterval = 15f; // Every 15 seconds something bad happens
    [SerializeField] private float damageEventChance = 0.4f; // 40% chance
    [SerializeField] private float fireEventChance = 0.3f; // 30% chance
    [SerializeField] private float crewInjuryChance = 0.3f; // 30% chance
    
    [Header("Damage Settings")]
    [SerializeField] private int minDamage = 5;
    [SerializeField] private int maxDamage = 25;
    
    [Header("Crew Injury Settings")]
    [SerializeField] private float lightInjuryChance = 0.6f; // Most injuries are light
    [SerializeField] private float seriousInjuryChance = 0.3f;
    [SerializeField] private float criticalInjuryChance = 0.1f;
    
    private float timeSinceLastChaos;
    
    // Events for UI notifications
    public System.Action<string> OnChaosEvent; // For Oregon Trail style messages
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    public void Tick(float deltaTime)
    {
        if (!enableChaos) return;
        
        timeSinceLastChaos += deltaTime;
        
        if (timeSinceLastChaos >= chaosInterval)
        {
            GenerateChaosEvent();
            timeSinceLastChaos = 0f;
        }
    }
    
    private void GenerateChaosEvent()
    {
        float roll = Random.value;
        
        if (roll < damageEventChance)
        {
            GenerateDamageEvent();
        }
        else if (roll < damageEventChance + fireEventChance)
        {
            GenerateFireEvent();
        }
        else if (roll < damageEventChance + fireEventChance + crewInjuryChance)
        {
            GenerateCrewInjuryEvent();
        }
        
        // Reset timer with slight randomness to avoid predictable timing
        chaosInterval = Random.Range(10f, 20f);
    }
    
    private void GenerateDamageEvent()
    {
        if (PlaneManager.Instance?.Sections == null || PlaneManager.Instance.Sections.Count == 0) return;
        
        // Pick a random section that's not already destroyed
        var healthySections = PlaneManager.Instance.Sections.Where(s => s.Integrity > 0).ToList();
        if (healthySections.Count == 0) return;
        
        var section = healthySections[Random.Range(0, healthySections.Count)];
        int damage = Random.Range(minDamage, maxDamage);
        
        PlaneManager.Instance.ApplyHitToSection(section.Id, damage, false, 0f); // No fire for pure damage events
        
        Debug.Log($"[Chaos] The {section.Id} takes {damage} damage!");
    }
    
    private void GenerateFireEvent()
    {
        if (PlaneManager.Instance?.Sections == null || PlaneManager.Instance.Sections.Count == 0) return;
        
        // Pick a random section that's not already on fire
        var nonBurningSections = PlaneManager.Instance.Sections.Where(s => !s.OnFire && s.Integrity > 0).ToList();
        if (nonBurningSections.Count == 0) return;
        
        var section = nonBurningSections[Random.Range(0, nonBurningSections.Count)];
        
        PlaneManager.Instance.ApplyHitToSection(section.Id, 0, true, 1f); // Pure fire event, guaranteed fire
        
        Debug.Log($"[Chaos] Fire event triggered in {section.Id}");
    }
    
    private void GenerateCrewInjuryEvent()
    {
        if (CrewManager.Instance?.AllCrew == null || CrewManager.Instance.AllCrew.Count == 0) return;
        
        // Pick a random healthy crew member
        var healthyCrew = CrewManager.Instance.AllCrew.Where(c => c.Status == CrewStatus.Healthy).ToList();
        if (healthyCrew.Count == 0) return;
        
        var crewMember = healthyCrew[Random.Range(0, healthyCrew.Count)];
        
        // Determine injury severity
        CrewStatus newStatus;
        float injuryRoll = Random.value;
        
        if (injuryRoll < criticalInjuryChance)
        {
            newStatus = CrewStatus.Critical;
        }
        else if (injuryRoll < criticalInjuryChance + seriousInjuryChance)
        {
            newStatus = CrewStatus.Serious;
        }
        else
        {
            newStatus = CrewStatus.Light;
        }
        
        // Apply injury through CrewManager (which will trigger the event)
        CrewManager.Instance.ApplyInjury(crewMember.Id, newStatus);
        
        string severity = newStatus switch
        {
            CrewStatus.Light => "lightly injured",
            CrewStatus.Serious => "seriously wounded",
            CrewStatus.Critical => "critically injured",
            _ => "injured"
        };
        
        Debug.Log($"[Chaos] {crewMember.Name} ({crewMember.Role}) is {severity}!");
    }
    
    /// <summary>
    /// For testing - force chaos events manually
    /// </summary>
    [ContextMenu("Force Damage Event")]
    public void ForceDamageEvent() => GenerateDamageEvent();
    
    [ContextMenu("Force Fire Event")]
    public void ForceFireEvent() => GenerateFireEvent();
    
    [ContextMenu("Force Crew Injury")]
    public void ForceCrewInjury() => GenerateCrewInjuryEvent();
    
    /// <summary>
    /// Toggle chaos on/off for peaceful testing
    /// </summary>
    public void SetChaosEnabled(bool enabled)
    {
        enableChaos = enabled;
        Debug.Log($"[Chaos] Chaos simulator {(enabled ? "enabled" : "disabled")}");
    }
}