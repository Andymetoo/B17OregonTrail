using System;
using System.Collections.Generic;
using UnityEngine;

public class PlaneManager : MonoBehaviour
{
    public static PlaneManager Instance { get; private set; }

    /// <summary>
    /// All structural sections of the plane (nose, cockpit, wings, tail, etc.)
    /// </summary>
    public List<PlaneSectionState> Sections = new List<PlaneSectionState>();

    /// <summary>
    /// All systems (engines, oxygen, turrets, etc.) that live in sections.
    /// </summary>
    public List<PlaneSystemState> Systems = new List<PlaneSystemState>();

    // Add this to an existing manager like GameStateManager or create a new bootstrap object:
    [Header("Consumables Scriptable Object")]
    [SerializeField] private CrewActionConfig crewActionConfig;

    [Header("Cruise Speed")]
    [Tooltip("Base cruise speed (all engines operational). Miles per hour.")]
    public float baseCruiseSpeedMph = 180f;
    [Tooltip("Minimum cruise speed when all engines are destroyed.")]
    public float minCruiseSpeedMph = 60f;

    // Simple fire damage tuning
    [Header("Fire Settings")]
    [Tooltip("Damage per second to section integrity while on fire.")]
    public float fireDamagePerSecond = 2f;
    
    [Tooltip("Chance per second for fire damage to be applied (0-1). Set to 0.33 for 33% chance per second.")]
    [Range(0f, 1f)]
    public float fireDamageChancePerSecond = 1f;

    [Tooltip("Chance per second for fire to spread to adjacent sections (0-1).")]
    [Range(0f, 1f)]
    public float fireSpreadChancePerSecond = 0.05f;

    [Tooltip("Minimum integrity before a section is considered destroyed.")]
    public int destroyedIntegrityThreshold = 0;

    // Events for other systems / UI
    public event Action<PlaneSectionState> OnSectionDamaged;
    public event Action<PlaneSectionState> OnSectionDestroyed;
    public event Action<PlaneSectionState> OnFireStarted;
    public event Action<PlaneSectionState> OnFireExtinguished;

    public event Action<PlaneSystemState> OnSystemStatusChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// Current dynamic cruise speed based on operational engines.
    /// </summary>
    public float CurrentCruiseSpeedMph
    {
        get
        {
            int engineCount = 0; int operational = 0;
            foreach (var sys in Systems)
            {
                if (sys.Type == SystemType.Engine)
                {
                    engineCount++;
                    if (sys.Status == SystemStatus.Operational) operational++;
                }
            }
            if (engineCount == 0) return baseCruiseSpeedMph;
            float fraction = (float)operational / engineCount;
            return Mathf.Lerp(minCruiseSpeedMph, baseCruiseSpeedMph, fraction);
        }
    }

    /// <summary>
    /// Called by GameStateManager once per simulation tick.
    /// Handles fire damage and fire spread mechanics.
    /// </summary>
    public void Tick(float deltaTime)
    {
        TickFires(deltaTime);
    }

    /// <summary>
    /// Process fire damage and spread.
    /// Fire spread chance controlled by fireSpreadChancePerSecond parameter (see Inspector).
    /// </summary>
    private void TickFires(float deltaTime)
    {
        foreach (var section in Sections)
        {
            if (!section.OnFire) continue;

            // Accumulate fire damage over time (always accumulates, chance applied when dealing damage)
            int oldIntegrity = section.Integrity;
            
            section.FireDamageAccumulator += fireDamagePerSecond * deltaTime;
            
            // When accumulated damage >= 1, roll chance to apply it
            if (section.FireDamageAccumulator >= 1f)
            {
                // Roll chance once per second (approximately)
                if (UnityEngine.Random.value < fireDamageChancePerSecond)
                {
                    int damage = Mathf.FloorToInt(section.FireDamageAccumulator);
                    section.FireDamageAccumulator -= damage; // Keep fractional remainder
                    
                    section.Integrity = Mathf.Max(destroyedIntegrityThreshold, section.Integrity - damage);

                    if (section.Integrity != oldIntegrity)
                    {
                        // Calculate old and new 10-point thresholds
                        int oldThreshold = (oldIntegrity / 10) * 10;
                        int newThreshold = (section.Integrity / 10) * 10;
                        
                        // Log when crossing DOWN through a 10-point threshold (e.g., 91->89 crosses 90)
                        bool crossedThreshold = oldThreshold > newThreshold;
                        bool isDestroyed = section.Integrity <= destroyedIntegrityThreshold;
                        
                        if (crossedThreshold || isDestroyed)
                        {
                            section.LastFireDamageThreshold = newThreshold;
                            OnSectionDamaged?.Invoke(section);
                            Debug.Log($"[Plane] Fire damaged {section.Id}: {oldIntegrity} -> {section.Integrity} (crossed threshold: {newThreshold})");
                        }

                        if (isDestroyed)
                        {
                            OnSectionDestroyed?.Invoke(section);
                        }
                    }
                }
                else
                {
                    // Chance failed, reset accumulator to try again next second
                    section.FireDamageAccumulator = 0f;
                }
            }

            // Fire spread to adjacent sections
            if (fireSpreadChancePerSecond > 0f && UnityEngine.Random.value < fireSpreadChancePerSecond * deltaTime)
            {
                var adjacentIds = GetAdjacentSections(section.Id);
                foreach (var adjId in adjacentIds)
                {
                    var adjSection = GetSection(adjId);
                    if (adjSection != null && !adjSection.OnFire && adjSection.Integrity > destroyedIntegrityThreshold)
                    {
                        // Fire spreads!
                        adjSection.OnFire = true;
                        adjSection.LastFireDamageThreshold = (adjSection.Integrity / 10) * 10; // Initialize threshold
                        OnFireStarted?.Invoke(adjSection);
                        Debug.Log($"[Plane] Fire spread from {section.Id} to {adjId}!");
                        break; // Only spread to one section per tick
                    }
                }
            }
        }
    }

    // ------------------------------------------------------------------
    // DAMAGE / FIRE APPLICATION
    // ------------------------------------------------------------------

    /// <summary>
    /// Apply a hit to a specific section, optionally starting a fire.
    /// </summary>
    public void ApplyHitToSection(string sectionId, int damageAmount, bool canStartFire, float fireStartChance = 0.25f)
    {
        var section = GetSection(sectionId);
        if (section == null) return;

        int oldIntegrity = section.Integrity;

        section.Integrity = Mathf.Max(destroyedIntegrityThreshold, section.Integrity - damageAmount);

        if (section.Integrity != oldIntegrity)
        {
            OnSectionDamaged?.Invoke(section);

            if (section.Integrity <= destroyedIntegrityThreshold)
            {
                OnSectionDestroyed?.Invoke(section);
            }
        }

        if (canStartFire && !section.OnFire)
        {
            if (UnityEngine.Random.value < fireStartChance)
            {
                section.OnFire = true;
                section.LastFireDamageThreshold = (section.Integrity / 10) * 10; // Initialize threshold
                OnFireStarted?.Invoke(section);
            }
        }
    }

    /// <summary>
    /// Apply a hit that may pick a random section, useful for flak.
    /// </summary>
    public void ApplyRandomHit(int damageAmount, bool canStartFire, float fireStartChance = 0.25f)
    {
        if (Sections == null || Sections.Count == 0) return;

        int index = UnityEngine.Random.Range(0, Sections.Count);
        var section = Sections[index];

        ApplyHitToSection(section.Id, damageAmount, canStartFire, fireStartChance);
    }

    /// <summary>
    /// Extinguish fire in a section (called by crew actions later).
    /// </summary>
    public bool TryExtinguishFire(string sectionId)
    {
        var section = GetSection(sectionId);
        if (section == null) return false;
        if (!section.OnFire) return false;

        section.OnFire = false;
        OnFireExtinguished?.Invoke(section);
        return true;
    }

    // ------------------------------------------------------------------
    // SYSTEM STATUS / REPAIRS
    // ------------------------------------------------------------------

    public PlaneSystemState GetSystem(string systemId)
    {
        return Systems.Find(s => s.Id == systemId);
    }

    public PlaneSectionState GetSection(string sectionId)
    {
        return Sections.Find(s => s.Id == sectionId);
    }

    /// <summary>
    /// Mark a system as damaged or destroyed.
    /// </summary>
    public void SetSystemStatus(string systemId, SystemStatus newStatus, SpecialState special = SpecialState.None)
    {
        var system = GetSystem(systemId);
        if (system == null) return;

        system.Status = newStatus;
        system.Special = special;

        OnSystemStatusChanged?.Invoke(system);
    }

    /// <summary>
    /// Example repair method. You can tune this later.
    /// </summary>
    public bool TryRepairSystem(string systemId, int repairAmount = 20)
    {
        var system = GetSystem(systemId);
        if (system == null) return false;
        var section = GetSection(system.SectionId);
        if (section == null) return false;

        // Cannot repair while section is on fire
        if (section.OnFire) return false;

        if (system.Status == SystemStatus.Destroyed)
        {
            // Maybe destroyed systems can't be repaired at all
            return false;
        }

        // Simple example: any repair puts it back to Operational
        system.Status = SystemStatus.Operational;
        system.Special = SpecialState.None;
        OnSystemStatusChanged?.Invoke(system);

        // Also repair the section that contains this system
        TryRepairSection(system.SectionId, repairAmount);

        return true;
    }

    /// <summary>
    /// Repair a section's structural integrity.
    /// </summary>
    public bool TryRepairSection(string sectionId, int repairAmount = 20)
    {
        var section = GetSection(sectionId);
        if (section == null) return false;

        // Can't repair if destroyed
        if (section.Integrity <= destroyedIntegrityThreshold) return false;
        // Can't repair if already at max integrity
        if (section.Integrity >= 100) return false;
        // Can't repair while on fire
        if (section.OnFire) return false;

        // Restore integrity using the provided amount (from CrewActionConfig bell curve)
        int oldIntegrity = section.Integrity;
        section.Integrity = Mathf.Min(100, section.Integrity + repairAmount);

        if (section.Integrity != oldIntegrity)
        {
            OnSectionDamaged?.Invoke(section); // Reuse this event for repairs too
        }

        return true;
    }

    /// <summary>
    /// Is a station "usable" - i.e. its section isn't destroyed and the system is operational?
    /// </summary>
    public bool IsStationFunctional(string stationSystemId)
    {
        var system = GetSystem(stationSystemId);
        if (system == null) return false;

        var section = GetSection(system.SectionId);
        if (section == null) return false;

        bool sectionAlive = section.Integrity > destroyedIntegrityThreshold;
        bool systemOk = system.Status == SystemStatus.Operational;

        return sectionAlive && systemOk;
    }

    // ------------------------------------------------------------------
    // FIRE SAFETY RULES
    // ------------------------------------------------------------------

    /// <summary>
    /// Get sections adjacent to the given section based on CrewPositionRegistry ordering.
    /// Returns sections immediately before and after in the linear order.
    /// </summary>
    public List<string> GetAdjacentSections(string sectionId)
    {
        var adjacent = new List<string>();
        if (CrewPositionRegistry.Instance == null) return adjacent;

        var orderedSections = CrewPositionRegistry.Instance.GetOrderedSectionIds();
        int index = orderedSections.IndexOf(sectionId);
        
        if (index == -1)
        {
            Debug.LogWarning($"[PlaneManager] GetAdjacentSections: '{sectionId}' not found in ordered sections!");
            return adjacent;
        }

        // Add previous section (toward nose)
        if (index > 0)
        {
            adjacent.Add(orderedSections[index - 1]);
        }

        // Add next section (toward tail)
        if (index < orderedSections.Count - 1)
        {
            adjacent.Add(orderedSections[index + 1]);
        }
        
        Debug.Log($"[PlaneManager] Adjacent to '{sectionId}': {string.Join(", ", adjacent)}");
        return adjacent;
    }

    /// <summary>
    /// Check if path between two sections is blocked by fire.
    /// Returns true if any section along the path (exclusive of start, inclusive of end) is on fire.
    /// Exception: ExtinguishFire action is allowed to target fire sections.
    /// </summary>
    public bool IsPathBlockedByFire(string startSectionId, string endSectionId, bool isExtinguishAction = false)
    {
        if (CrewPositionRegistry.Instance == null) return false;

        var orderedSections = CrewPositionRegistry.Instance.GetOrderedSectionIds();
        int startIdx = orderedSections.IndexOf(startSectionId);
        int endIdx = orderedSections.IndexOf(endSectionId);

        if (startIdx == -1 || endIdx == -1) return false;

        // Determine direction
        int step = startIdx < endIdx ? 1 : -1;
        
        // Check each section along path
        for (int i = startIdx + step; step > 0 ? i <= endIdx : i >= endIdx; i += step)
        {
            var section = GetSection(orderedSections[i]);
            if (section != null && section.OnFire)
            {
                // Allow entering final section if this is extinguish action
                if (isExtinguishAction && i == endIdx)
                {
                    continue; // Extinguish can target fire section
                }
                return true; // Path is blocked
            }
        }

        return false;
    }

    /// <summary>
    /// Check if a crew member is trapped by fire with no escape route.
    /// Returns true if the section is surrounded by fire on all adjacent sides.
    /// </summary>
    public bool IsCrewTrappedByFire(string currentSectionId)
    {
        var section = GetSection(currentSectionId);
        if (section == null) return false;

        // If current section is on fire, crew is trapped
        if (section.OnFire) return true;

        var adjacent = GetAdjacentSections(currentSectionId);
        if (adjacent.Count == 0) return false; // Edge case: no adjacent sections

        // Check if ALL adjacent sections are on fire (trapped)
        foreach (var adjId in adjacent)
        {
            var adjSection = GetSection(adjId);
            if (adjSection != null && !adjSection.OnFire)
            {
                return false; // Found escape route
            }
        }

        return true; // All adjacent sections on fire = trapped
    }

    /// <summary>
    /// Get the nearest non-fire adjacent section for emergency evacuation.
    /// Returns null if no safe adjacent section exists.
    /// </summary>
    public string GetNearestSafeAdjacentSection(string currentSectionId)
    {
        var adjacent = GetAdjacentSections(currentSectionId);
        Debug.Log($"[PlaneManager] GetNearestSafeAdjacentSection for '{currentSectionId}' - checking {adjacent.Count} adjacent sections");
        
        foreach (var adjId in adjacent)
        {
            var adjSection = GetSection(adjId);
            if (adjSection != null && !adjSection.OnFire)
            {
                Debug.Log($"[PlaneManager] Found safe adjacent section: '{adjId}'");
                return adjId; // First safe adjacent section
            }
            else if (adjSection != null && adjSection.OnFire)
            {
                Debug.Log($"[PlaneManager] Adjacent section '{adjId}' is on fire, skipping");
            }
        }

        Debug.LogWarning($"[PlaneManager] No safe adjacent section found for '{currentSectionId}'!");
        return null; // No safe adjacent section
    }

    /// <summary>
    /// Helper method to start a fire and trigger the event (for debug/external use).
    /// </summary>
    public void StartFire(string sectionId)
    {
        var section = GetSection(sectionId);
        if (section == null || section.OnFire) return;
        
        section.OnFire = true;
        section.LastFireDamageThreshold = (section.Integrity / 10) * 10; // Initialize threshold
        OnFireStarted?.Invoke(section);
    }

    public void DebugDamageSection(string sectionId)
    {
        var section = GetSection(sectionId);
        if (section == null)
        {
            Debug.LogWarning($"[Plane] DebugDamageSection: no section with id {sectionId}");
            return;
        }

        section.Integrity = Mathf.Max(0, section.Integrity - 50);
        section.OnFire = true;

        Debug.Log($"[Plane] Debug damaged {sectionId}: integrity={section.Integrity}, onFire={section.OnFire}");

        // If you have an event, raise it here, e.g.:
        // OnSectionStateChanged?.Invoke(section);
    }

}
