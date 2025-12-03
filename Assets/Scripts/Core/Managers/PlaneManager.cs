using System;
using System.Collections.Generic;
using System.Linq;
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
    
    [Header("Altitude & Descent")]
    [Tooltip("Current altitude in feet.")]
    public float currentAltitudeFeet = 25000f;
    [Tooltip("Altitude lost per second when >2 engines destroyed (feet/sec).")]
    public float descentRateFeetPerSecond = 50f;
    [Tooltip("Minimum safe altitude - below this triggers critical warnings.")]
    public float minimumSafeAltitudeFeet = 5000f;

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
    public event Action<PlaneSystemState> OnEngineFireStarted;      // Engine-specific fire events
    public event Action<PlaneSystemState> OnEngineFireExtinguished;
    public event Action<PlaneSystemState> OnEngineDamaged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        
        // Initialize B-17 engines if not already set up
        InitializeEngines();
    }
    
    /// <summary>
    /// Initialize the four engines of the B-17.
    /// Called during Awake to ensure engines exist.
    /// </summary>
    private void InitializeEngines()
    {
        // Only initialize if no engines exist
        if (Systems.Any(s => s.Type == SystemType.Engine)) return;
        
        // B-17 has 4 engines: 2 on each wing (outer left, inner left, inner right, outer right)
        string[] engineIds = { "Engine1", "Engine2", "Engine3", "Engine4" };
        string[] engineSections = { "LeftWing", "LeftWing", "RightWing", "RightWing" };
        
        for (int i = 0; i < 4; i++)
        {
            var engine = new PlaneSystemState
            {
                Id = engineIds[i],
                Type = SystemType.Engine,
                Status = SystemStatus.Operational,
                Special = SpecialState.None,
                SectionId = engineSections[i],
                Integrity = 100,
                OnFire = false,
                IsFeathered = false
            };
            Systems.Add(engine);
            Debug.Log($"[PlaneManager] Initialized {engine.Id} in {engine.SectionId}");
        }
    }

    /// <summary>
    /// Current dynamic cruise speed based on operational engines.
    /// Accounts for: Operational (100%), Damaged (75%), Destroyed/Feathered (0%).
    /// </summary>
    public float CurrentCruiseSpeedMph
    {
        get
        {
            int engineCount = 0;
            float totalPower = 0f;
            
            foreach (var sys in Systems)
            {
                if (sys.Type == SystemType.Engine)
                {
                    engineCount++;
                    
                    // Feathered or destroyed engines provide no power
                    if (sys.IsFeathered || sys.Status == SystemStatus.Destroyed || sys.Integrity <= 0)
                    {
                        // 0% power
                    }
                    // Damaged engines provide reduced power
                    else if (sys.Status == SystemStatus.Damaged)
                    {
                        totalPower += 0.75f; // 75% power
                    }
                    // Operational engines provide full power
                    else if (sys.Status == SystemStatus.Operational)
                    {
                        totalPower += 1.0f; // 100% power
                    }
                }
            }
            
            if (engineCount == 0) return baseCruiseSpeedMph;
            
            float fraction = totalPower / engineCount;
            return Mathf.Lerp(minCruiseSpeedMph, baseCruiseSpeedMph, fraction);
        }
    }

    /// <summary>
    /// Called by GameStateManager once per simulation tick.
    /// Handles fire damage and fire spread mechanics for both sections and engines.
    /// Also handles altitude descent when engines are lost.
    /// </summary>
    public void Tick(float deltaTime)
    {
        TickFires(deltaTime);
        TickEngines(deltaTime);
        TickAltitude(deltaTime);
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
    /// <summary>
    /// Attempt to extinguish a fire on a section or engine.
    /// Checks both sections and engines by ID.
    /// </summary>
    public bool TryExtinguishFire(string targetId)
    {
        // Try section first
        var section = GetSection(targetId);
        if (section != null)
        {
            if (!section.OnFire) return false;
            section.OnFire = false;
            section.FireDamageAccumulator = 0f;
            OnFireExtinguished?.Invoke(section);
            return true;
        }
        
        // Try engine
        var engine = GetEngine(targetId);
        if (engine != null)
        {
            return TryExtinguishEngineFire(targetId);
        }
        
        return false;
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
    
    // ================================
    // ENGINE SYSTEMS
    // ================================
    
    /// <summary>
    /// Get an engine by ID.
    /// </summary>
    public PlaneSystemState GetEngine(string engineId)
    {
        return Systems.FirstOrDefault(s => s.Type == SystemType.Engine && s.Id == engineId);
    }
    
    /// <summary>
    /// Process engine fires (damage over time).
    /// </summary>
    private void TickEngines(float deltaTime)
    {
        foreach (var engine in Systems.Where(s => s.Type == SystemType.Engine))
        {
            if (!engine.OnFire) continue;
            
            int oldIntegrity = engine.Integrity;
            
            // Accumulate fire damage
            engine.FireDamageAccumulator += fireDamagePerSecond * deltaTime;
            
            // When accumulated >= 1, roll chance to apply
            if (engine.FireDamageAccumulator >= 1f)
            {
                if (UnityEngine.Random.value < fireDamageChancePerSecond)
                {
                    int damage = Mathf.FloorToInt(engine.FireDamageAccumulator);
                    engine.FireDamageAccumulator -= damage;
                    
                    engine.Integrity = Mathf.Max(0, engine.Integrity - damage);
                    
                    // Update status based on integrity
                    UpdateEngineStatus(engine);
                    
                    if (engine.Integrity != oldIntegrity)
                    {
                        // Calculate old and new 10-point thresholds
                        int oldThreshold = (oldIntegrity / 10) * 10;
                        int newThreshold = (engine.Integrity / 10) * 10;
                        
                        // Log when crossing DOWN through a 10-point threshold
                        bool crossedThreshold = oldThreshold > newThreshold;
                        bool isDestroyed = engine.Integrity <= 0;
                        
                        if (crossedThreshold || isDestroyed)
                        {
                            engine.LastFireDamageThreshold = newThreshold;
                            OnEngineDamaged?.Invoke(engine);
                            EventLogUI.Instance?.Log($"Fire damaged {engine.Id}: {engine.Integrity}%", Color.red);
                            Debug.Log($"[Plane] Fire damaged {engine.Id}: {oldIntegrity} -> {engine.Integrity}");
                        }
                        
                        if (isDestroyed)
                        {
                            EventLogUI.Instance?.Log($"{engine.Id} destroyed!", new Color(0.8f, 0f, 0f));
                        }
                    }
                }
                else
                {
                    // Chance failed, reset accumulator
                    engine.FireDamageAccumulator = 0f;
                }
            }
        }
    }
    
    /// <summary>
    /// Update engine status based on integrity level.
    /// 100-75: Operational, 74-1: Damaged, 0: Destroyed
    /// </summary>
    private void UpdateEngineStatus(PlaneSystemState engine)
    {
        SystemStatus oldStatus = engine.Status;
        
        if (engine.Integrity <= 0)
        {
            engine.Status = SystemStatus.Destroyed;
        }
        else if (engine.Integrity < 75)
        {
            engine.Status = SystemStatus.Damaged;
        }
        else
        {
            engine.Status = SystemStatus.Operational;
        }
        
        if (oldStatus != engine.Status)
        {
            OnSystemStatusChanged?.Invoke(engine);
            Debug.Log($"[Plane] {engine.Id} status changed: {oldStatus} -> {engine.Status}");
        }
    }
    
    /// <summary>
    /// Apply damage to an engine (from combat hits).
    /// </summary>
    public void ApplyEngineHit(string engineId, int damage, float fireChance = 0f)
    {
        var engine = GetEngine(engineId);
        if (engine == null)
        {
            Debug.LogWarning($"[Plane] ApplyEngineHit: Engine {engineId} not found");
            return;
        }
        
        int oldIntegrity = engine.Integrity;
        engine.Integrity = Mathf.Max(0, engine.Integrity - damage);
        
        UpdateEngineStatus(engine);
        
        if (engine.Integrity != oldIntegrity)
        {
            OnEngineDamaged?.Invoke(engine);
            EventLogUI.Instance?.Log($"{engine.Id} hit! Integrity: {engine.Integrity}%", new Color(1f, 0.4f, 0f));
            Debug.Log($"[Plane] {engine.Id} hit for {damage} damage: {oldIntegrity} -> {engine.Integrity}");
        }
        
        // Roll for fire
        if (!engine.OnFire && fireChance > 0f && UnityEngine.Random.value < fireChance)
        {
            StartEngineFire(engine);
        }
    }
    
    /// <summary>
    /// Start a fire on an engine.
    /// </summary>
    public void StartEngineFire(PlaneSystemState engine)
    {
        if (engine.OnFire) return; // Already on fire
        
        engine.OnFire = true;
        engine.FireDamageAccumulator = 0f;
        engine.LastFireDamageThreshold = (engine.Integrity / 10) * 10;
        
        OnEngineFireStarted?.Invoke(engine);
        EventLogUI.Instance?.Log($"{engine.Id} is on fire!", new Color(1f, 0.3f, 0f));
        Debug.Log($"[Plane] Fire started on {engine.Id}");
    }
    
    /// <summary>
    /// Attempt to extinguish an engine fire.
    /// </summary>
    public bool TryExtinguishEngineFire(string engineId)
    {
        var engine = GetEngine(engineId);
        if (engine == null)
        {
            Debug.LogWarning($"[Plane] TryExtinguishEngineFire: Engine {engineId} not found");
            return false;
        }
        
        if (!engine.OnFire)
        {
            Debug.Log($"[Plane] {engineId} is not on fire");
            return false;
        }
        
        engine.OnFire = false;
        engine.FireDamageAccumulator = 0f;
        
        OnEngineFireExtinguished?.Invoke(engine);
        EventLogUI.Instance?.Log($"{engine.Id} fire extinguished.", Color.cyan);
        Debug.Log($"[Plane] Fire extinguished on {engine.Id}");
        
        return true;
    }
    
    /// <summary>
    /// Feather an engine (stop it to prevent drag and further damage).
    /// </summary>
    public void FeatherEngine(string engineId)
    {
        var engine = GetEngine(engineId);
        if (engine == null)
        {
            Debug.LogWarning($"[Plane] FeatherEngine: Engine {engineId} not found");
            return;
        }
        
        if (engine.IsFeathered)
        {
            Debug.Log($"[Plane] {engineId} is already feathered");
            return;
        }
        
        engine.IsFeathered = true;
        OnSystemStatusChanged?.Invoke(engine);
        EventLogUI.Instance?.Log($"{engine.Id} feathered.", Color.yellow);
        Debug.Log($"[Plane] {engine.Id} feathered");
    }
    
    /// <summary>
    /// Handle altitude descent when too many engines are destroyed.
    /// Plane descends when >2 engines are destroyed or feathered.
    /// </summary>
    private void TickAltitude(float deltaTime)
    {
        // Count operational engines (not destroyed, not feathered)
        int operationalEngines = 0;
        foreach (var engine in Systems.Where(s => s.Type == SystemType.Engine))
        {
            if (!engine.IsFeathered && engine.Status != SystemStatus.Destroyed && engine.Integrity > 0)
            {
                operationalEngines++;
            }
        }
        
        // Descent when 2 or fewer operational engines (need at least 3 to maintain altitude)
        if (operationalEngines <= 2)
        {
            float oldAltitude = currentAltitudeFeet;
            currentAltitudeFeet -= descentRateFeetPerSecond * deltaTime;
            currentAltitudeFeet = Mathf.Max(0f, currentAltitudeFeet);
            
            // Log altitude warnings at key thresholds
            if (oldAltitude > minimumSafeAltitudeFeet && currentAltitudeFeet <= minimumSafeAltitudeFeet)
            {
                EventLogUI.Instance?.Log($"WARNING: Altitude critical! {Mathf.RoundToInt(currentAltitudeFeet)} feet", new Color(1f, 0.5f, 0f));
                Debug.LogWarning($"[Plane] Altitude critical: {currentAltitudeFeet:F0} feet, {operationalEngines} operational engines");
            }
            
            if (currentAltitudeFeet <= 0f)
            {
                EventLogUI.Instance?.Log("CRASH: Aircraft has hit the ground!", Color.red);
                Debug.LogError("[Plane] Aircraft crashed - altitude reached 0");
                // TODO: Trigger crash/mission failure
            }
        }
    }

}

