using UnityEngine;
using System.Linq;

/// <summary>
/// Centralized debug tools for testing game systems.
/// Provides hotkeys and methods for simulating damage, fires, altitude changes, etc.
/// </summary>
public class DebugManager : MonoBehaviour
{
    public static DebugManager Instance { get; private set; }

    [Header("Debug Hotkeys")]
    [Tooltip("Enable debug hotkeys (disable in production builds).")]
    public bool enableDebugHotkeys = true;
    
    [Header("Random Fire")]
    [Tooltip("Hotkey: P - Start a random fire in a random section.")]
    public KeyCode randomFireHotkey = KeyCode.P;
    
    [Header("Engine Damage")]
    [Tooltip("Hotkey: O - Damage a random engine.")]
    public KeyCode damageEngineHotkey = KeyCode.O;
    [Tooltip("Min damage to apply to engine (integrity points).")]
    public int minEngineDamage = 15;
    [Tooltip("Max damage to apply to engine (integrity points).")]
    public int maxEngineDamage = 40;
    [Tooltip("Chance for engine to catch fire when damaged (0-1).")]
    [Range(0f, 1f)]
    public float engineFireChanceOnDamage = 0.3f;
    
    [Header("Altitude Control")]
    [Tooltip("Hotkey: I - Increase altitude by 1000 ft.")]
    public KeyCode increaseAltitudeHotkey = KeyCode.I;
    [Tooltip("Hotkey: K - Decrease altitude by 1000 ft.")]
    public KeyCode decreaseAltitudeHotkey = KeyCode.K;
    [Tooltip("Amount to adjust altitude per keypress (feet).")]
    public float altitudeAdjustmentAmount = 1000f;
    
    [Header("Fuel Control")]
    [Tooltip("Hotkey: U - Add 100 gallons of fuel.")]
    public KeyCode addFuelHotkey = KeyCode.U;
    [Tooltip("Hotkey: J - Remove 100 gallons of fuel.")]
    public KeyCode removeFuelHotkey = KeyCode.J;
    [Tooltip("Amount to adjust fuel per keypress (gallons).")]
    public float fuelAdjustmentAmount = 100f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        if (!enableDebugHotkeys) return;

        // Random fire
        if (Input.GetKeyDown(randomFireHotkey))
        {
            StartRandomFire();
        }
        
        // Engine damage
        if (Input.GetKeyDown(damageEngineHotkey))
        {
            DamageRandomEngine();
        }
        
        // Altitude control
        if (Input.GetKeyDown(increaseAltitudeHotkey))
        {
            AdjustAltitude(altitudeAdjustmentAmount);
        }
        if (Input.GetKeyDown(decreaseAltitudeHotkey))
        {
            AdjustAltitude(-altitudeAdjustmentAmount);
        }
        
        // Fuel control
        if (Input.GetKeyDown(addFuelHotkey))
        {
            AdjustFuel(fuelAdjustmentAmount);
        }
        if (Input.GetKeyDown(removeFuelHotkey))
        {
            AdjustFuel(-fuelAdjustmentAmount);
        }
    }

    /// <summary>
    /// Start a fire in a random section that isn't already burning.
    /// </summary>
    public void StartRandomFire()
    {
        if (PlaneManager.Instance == null || PlaneManager.Instance.Sections == null || PlaneManager.Instance.Sections.Count == 0)
        {
            Debug.LogWarning("[DebugManager] Cannot start fire - no sections available");
            return;
        }
        
        // Get all sections that aren't already on fire and aren't destroyed
        var validSections = PlaneManager.Instance.Sections
            .Where(s => !s.OnFire && s.Integrity > 0)
            .ToList();
            
        if (validSections.Count == 0)
        {
            Debug.LogWarning("[DebugManager] Cannot start fire - all sections are already burning or destroyed!");
            EventLogUI.Instance?.Log("All sections are burning or destroyed!", Color.yellow);
            return;
        }
        
        // Pick random section
        var section = validSections[Random.Range(0, validSections.Count)];
        
        // Start fire using PlaneManager helper (properly triggers event)
        PlaneManager.Instance.StartFire(section.Id);
        
        EventLogUI.Instance?.Log($"[DEBUG] Started fire in {section.Id}", new Color(1f, 0.5f, 0f));
        Debug.Log($"[DebugManager] Started fire in {section.Id}");
    }

    /// <summary>
    /// Apply damage to a random operational engine.
    /// Optionally causes engine fire.
    /// </summary>
    public void DamageRandomEngine()
    {
        if (PlaneManager.Instance == null || PlaneManager.Instance.Systems == null)
        {
            Debug.LogWarning("[DebugManager] Cannot damage engine - PlaneManager not available");
            return;
        }
        
        // Get all engines that aren't already destroyed
        var validEngines = PlaneManager.Instance.Systems
            .Where(s => s.Type == SystemType.Engine && s.Integrity > 0)
            .ToList();
            
        if (validEngines.Count == 0)
        {
            Debug.LogWarning("[DebugManager] Cannot damage engine - all engines already destroyed!");
            EventLogUI.Instance?.Log("All engines are destroyed!", Color.red);
            return;
        }
        
        // Pick random engine
        var engine = validEngines[Random.Range(0, validEngines.Count)];
        
        // Apply damage
        int damage = Random.Range(minEngineDamage, maxEngineDamage + 1);
        float fireChance = engineFireChanceOnDamage;
        
        PlaneManager.Instance.ApplyEngineHit(engine.Id, damage, fireChance);
        
        string fireText = engine.OnFire ? " and caught FIRE!" : "";
        EventLogUI.Instance?.Log($"[DEBUG] {engine.Id} took {damage} damage{fireText}", new Color(1f, 0.3f, 0f));
        Debug.Log($"[DebugManager] Damaged {engine.Id}: {damage} points, fire: {engine.OnFire}");
    }

    /// <summary>
    /// Adjust plane altitude by specified amount.
    /// </summary>
    public void AdjustAltitude(float deltaFeet)
    {
        if (PlaneManager.Instance == null)
        {
            Debug.LogWarning("[DebugManager] Cannot adjust altitude - PlaneManager not available");
            return;
        }
        
        float oldAltitude = PlaneManager.Instance.currentAltitudeFeet;
        PlaneManager.Instance.currentAltitudeFeet = Mathf.Max(0f, oldAltitude + deltaFeet);
        float newAltitude = PlaneManager.Instance.currentAltitudeFeet;
        
        string direction = deltaFeet > 0 ? "increased" : "decreased";
        EventLogUI.Instance?.Log($"[DEBUG] Altitude {direction}: {newAltitude:F0} ft", Color.cyan);
        Debug.Log($"[DebugManager] Altitude adjusted: {oldAltitude:F0} -> {newAltitude:F0} ft");
    }

    /// <summary>
    /// Adjust fuel by specified amount.
    /// </summary>
    public void AdjustFuel(float deltaGallons)
    {
        if (PlaneManager.Instance == null)
        {
            Debug.LogWarning("[DebugManager] Cannot adjust fuel - PlaneManager not available");
            return;
        }
        
        PlaneManager.Instance.AdjustFuel(deltaGallons);
        Debug.Log($"[DebugManager] Fuel adjusted by {deltaGallons:F0} gallons");
    }

    /// <summary>
    /// Destroy a random engine completely.
    /// </summary>
    public void DestroyRandomEngine()
    {
        if (PlaneManager.Instance == null || PlaneManager.Instance.Systems == null)
        {
            Debug.LogWarning("[DebugManager] Cannot destroy engine - PlaneManager not available");
            return;
        }
        
        var validEngines = PlaneManager.Instance.Systems
            .Where(s => s.Type == SystemType.Engine && s.Status != SystemStatus.Destroyed)
            .ToList();
            
        if (validEngines.Count == 0)
        {
            Debug.LogWarning("[DebugManager] All engines already destroyed!");
            return;
        }
        
        var engine = validEngines[Random.Range(0, validEngines.Count)];
        PlaneManager.Instance.ApplyEngineHit(engine.Id, 999, 0f); // Massive damage, no fire
        
        EventLogUI.Instance?.Log($"[DEBUG] {engine.Id} DESTROYED!", Color.red);
        Debug.Log($"[DebugManager] Destroyed {engine.Id}");
    }

    /// <summary>
    /// Heal all engines to full integrity.
    /// </summary>
    public void RepairAllEngines()
    {
        if (PlaneManager.Instance == null || PlaneManager.Instance.Systems == null)
        {
            Debug.LogWarning("[DebugManager] Cannot repair engines - PlaneManager not available");
            return;
        }
        
        int repairedCount = 0;
        foreach (var engine in PlaneManager.Instance.Systems.Where(s => s.Type == SystemType.Engine))
        {
            if (engine.Integrity < 100)
            {
                engine.Integrity = 100;
                engine.Status = SystemStatus.Operational;
                engine.OnFire = false;
                engine.IsFeathered = false;
                repairedCount++;
            }
        }
        
        EventLogUI.Instance?.Log($"[DEBUG] Repaired {repairedCount} engines to full health", Color.green);
        Debug.Log($"[DebugManager] Repaired {repairedCount} engines");
    }

    /// <summary>
    /// Extinguish all fires on sections and engines.
    /// </summary>
    public void ExtinguishAllFires()
    {
        if (PlaneManager.Instance == null)
        {
            Debug.LogWarning("[DebugManager] Cannot extinguish fires - PlaneManager not available");
            return;
        }
        
        int fireCount = 0;
        
        // Extinguish section fires
        foreach (var section in PlaneManager.Instance.Sections)
        {
            if (section.OnFire)
            {
                section.OnFire = false;
                fireCount++;
            }
        }
        
        // Extinguish engine fires
        foreach (var engine in PlaneManager.Instance.Systems.Where(s => s.Type == SystemType.Engine))
        {
            if (engine.OnFire)
            {
                engine.OnFire = false;
                fireCount++;
            }
        }
        
        EventLogUI.Instance?.Log($"[DEBUG] Extinguished {fireCount} fires", Color.cyan);
        Debug.Log($"[DebugManager] Extinguished {fireCount} fires");
    }
}
