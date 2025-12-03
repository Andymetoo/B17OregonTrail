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
    
    [Header("Danger & Intervals")]
    [SerializeField] private bool enableChaos = true;
    [Tooltip("Event interval when danger=0 (seconds) within hazard phases.")]
    [SerializeField] private float eventIntervalAtSafe = 20f;
    [Tooltip("Event interval when danger=1 (seconds) within hazard phases.")]
    [SerializeField] private float eventIntervalAtDanger = 6f;
    [Tooltip("Fallback danger when no leg is active (0-1).")]
    [Range(0f,1f)] [SerializeField] private float defaultDanger = 0.3f;

    [Header("Timing Randomness")]
    [Tooltip("If true, use an exponential (Poisson) process for event timing; otherwise apply uniform jitter.")]
    [SerializeField] private bool useExponentialTiming = true;
    [Tooltip("Uniform jitter fraction around the mean interval when not using exponential timing (e.g., 0.35 => ±35%).")]
    [Range(0f,1f)] [SerializeField] private float hazardIntervalJitter = 0.35f;
    [Range(0f,1f)] [SerializeField] private float injuryIntervalJitter = 0.35f;

    [Header("Crew Injury Intervals")]
    [Tooltip("Crew injury interval when danger=0 (seconds).")]
    [SerializeField] private float injuryIntervalAtSafe = 40f;
    [Tooltip("Crew injury interval when danger=1 (seconds).")]
    [SerializeField] private float injuryIntervalAtDanger = 12f;
    
    [Header("Event Weights (0-1 ranges, scaled by Danger)")]
    [Range(0f,1f)] [SerializeField] private float planeDamageWeightMin = 0.2f;
    [Range(0f,1f)] [SerializeField] private float planeDamageWeightMax = 0.9f;
    [Range(0f,1f)] [SerializeField] private float engineDamageWeightMin = 0.1f;
    [Range(0f,1f)] [SerializeField] private float engineDamageWeightMax = 0.4f;
    [Range(0f,1f)] [SerializeField] private float fireWeightMin = 0.1f;
    [Range(0f,1f)] [SerializeField] private float fireWeightMax = 0.7f;
    [Range(0f,1f)] [SerializeField] private float crewInjuryWeightMin = 0.05f;
    [Range(0f,1f)] [SerializeField] private float crewInjuryWeightMax = 0.4f;

    [Header("Plane Damage Amount (scaled by Danger)")]
    [SerializeField] private int minDamage = 5;
    [SerializeField] private int maxDamage = 25;
    
    [Header("Engine Damage Amount")]
    [SerializeField] private int minEngineDamage = 10;
    [SerializeField] private int maxEngineDamage = 35;
    [Tooltip("Chance for engine hit to start a fire (0-1).")]
    [Range(0f, 1f)] [SerializeField] private float engineFireChance = 0.3f;

    [Header("Injury Severity Weights (0-1 ranges)")]
    [Range(0f,1f)] [SerializeField] private float lightSeverityWeightMin = 0.7f;
    [Range(0f,1f)] [SerializeField] private float lightSeverityWeightMax = 0.3f;
    [Range(0f,1f)] [SerializeField] private float seriousSeverityWeightMin = 0.25f;
    [Range(0f,1f)] [SerializeField] private float seriousSeverityWeightMax = 0.5f;
    [Range(0f,1f)] [SerializeField] private float criticalSeverityWeightMin = 0.05f;
    [Range(0f,1f)] [SerializeField] private float criticalSeverityWeightMax = 0.2f;
    
    // Phase scheduling
    public enum HazardPhase { Cruise, Flak, Fighters }
    [Header("Phase Duration Ranges (seconds)")]
    [Tooltip("Minimum duration for Cruise phases.")]
    [SerializeField] private float cruiseMinDuration = 8f;
    [Tooltip("Maximum duration for Cruise phases.")]
    [SerializeField] private float cruiseMaxDuration = 20f;
    [Tooltip("Minimum duration for Flak phases.")]
    [SerializeField] private float flakMinDuration = 6f;
    [Tooltip("Maximum duration for Flak phases.")]
    [SerializeField] private float flakMaxDuration = 14f;
    [Tooltip("Minimum duration for Fighter phases.")]
    [SerializeField] private float fightersMinDuration = 6f;
    [Tooltip("Maximum duration for Fighter phases.")]
    [SerializeField] private float fightersMaxDuration = 14f;
    [Tooltip("Fallback cruise bias when no leg weights are configured (0-1).")]
    [SerializeField] private float cruiseBias = 0.6f;

    private HazardPhase _currentPhase = HazardPhase.Cruise;
    private float _phaseTimer;
    private float _phaseDuration;
    private bool _legConfigured;

    // Warnings/guardrails
    private bool _warnedTickWithoutLeg;

    // Leg-configured values
    private float _legStartDanger = 0f;
    private float _legEndDanger = 0.6f;
    private LegPhaseWeights _legPhaseWeights = new LegPhaseWeights();

    public bool IsInHazardPhase => _currentPhase == HazardPhase.Flak || _currentPhase == HazardPhase.Fighters;
    
    // Public accessors for UI/debugging
    public HazardPhase CurrentPhase => _currentPhase;
    public float CurrentDanger { get; private set; }
    public float PhaseProgress => _phaseDuration > 0f ? _phaseTimer / _phaseDuration : 0f;
    public float PhaseTimeRemaining => Mathf.Max(0f, _phaseDuration - _phaseTimer);

    private float timeSinceLastHazard;
    private float _nextHazardInterval;
    private float timeSinceLastInjury;
    private float _nextInjuryInterval;
    
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
        // Update leg-driven danger if travelling
        if (MissionManager.Instance != null && MissionManager.Instance.IsTravelling)
        {
            if (!_legConfigured || _phaseDuration <= 0f)
            {
                // Auto-bootstrap a leg if config was missed to avoid disabling hazards entirely
                // Use existing stored values or safe defaults
                Debug.LogWarning("[Chaos] Auto-configuring leg due to missing ConfigureLeg; applying grace period and stored/default values.");
                ConfigureLeg(_legStartDanger, _legEndDanger, _legPhaseWeights);
            }

            var t = MissionManager.Instance.SegmentProgress01;
            float danger = Mathf.Lerp(_legStartDanger, _legEndDanger, t);
            CurrentDanger = danger; // Store for external monitoring
            // Per-phase event interval shortens with higher danger
            float hazardIntervalMean = Mathf.Lerp(eventIntervalAtSafe, eventIntervalAtDanger, danger);
            float injuryIntervalMean = Mathf.Lerp(injuryIntervalAtSafe, injuryIntervalAtDanger, CurrentCrewInjuryWeight(danger));

            // Advance current phase timer and maybe transition
            _phaseTimer += deltaTime;
            
            // Log phase status every second (throttled)
            if (Time.frameCount % 60 == 0)
            {
                Debug.Log($"[Chaos] Phase: {_currentPhase}, Timer: {_phaseTimer:F1}/{_phaseDuration:F1}s, Danger: {danger:F2}, NextHazard: {_nextHazardInterval:F1}s");
            }
            
            if (_phaseTimer >= _phaseDuration)
            {
                ChooseNextPhase();
                // Re-sample hazard timing on phase change for extra variety
                _nextHazardInterval = SampleInterval(hazardIntervalMean, hazardIntervalJitter, useExponentialTiming);
            }

            // Within hazard phases, roll hazard events
            timeSinceLastHazard += deltaTime;
            if (IsInHazardPhase)
            {
                // Initialize next interval if needed
                if (_nextHazardInterval <= 0f)
                {
                    _nextHazardInterval = SampleInterval(hazardIntervalMean, hazardIntervalJitter, useExponentialTiming);
                    Debug.Log($"[Chaos] Initialized hazard interval: {_nextHazardInterval:F1}s for {_currentPhase} phase");
                }
                if (timeSinceLastHazard >= _nextHazardInterval)
                {
                    timeSinceLastHazard = 0f;
                    _nextHazardInterval = SampleInterval(hazardIntervalMean, hazardIntervalJitter, useExponentialTiming);
                    Debug.Log($"[Chaos] Generating hazard event for {_currentPhase}, next in {_nextHazardInterval:F1}s");
                    if (_currentPhase == HazardPhase.Flak)
                    {
                        GenerateFlakEvent(danger);
                    }
                    else if (_currentPhase == HazardPhase.Fighters)
                    {
                        GenerateFighterEvent(danger);
                    }
                }
            }

            // Crew injuries ONLY occur during hazard phases (Flak/Fighters), not during Cruise
            if (IsInHazardPhase)
            {
                timeSinceLastInjury += deltaTime;
                // Initialize next interval if needed
                if (_nextInjuryInterval <= 0f)
                {
                    _nextInjuryInterval = SampleInterval(injuryIntervalMean, injuryIntervalJitter, useExponentialTiming);
                }
                if (timeSinceLastInjury >= _nextInjuryInterval)
                {
                    timeSinceLastInjury = 0f;
                    _nextInjuryInterval = SampleInterval(injuryIntervalMean, injuryIntervalJitter, useExponentialTiming);
                    if (Random.value < CurrentCrewInjuryWeight(danger)) GenerateCrewInjuryEvent(danger);
                }
            }
        }
    }
    public void ConfigureLeg(float startDanger, float endDanger, LegPhaseWeights weights)
    {
        _legStartDanger = Mathf.Clamp01(startDanger);
        _legEndDanger = Mathf.Clamp01(endDanger);
        _legPhaseWeights = weights ?? new LegPhaseWeights();
        // Start in cruise at leg begin with grace period (3-5 seconds of peace)
        _currentPhase = HazardPhase.Cruise;
        _phaseTimer = 0f;
        _phaseDuration = Random.Range(3f, 5f); // Initial grace period
        _legConfigured = true;
        _warnedTickWithoutLeg = false;
        timeSinceLastHazard = 0f;
        timeSinceLastInjury = 0f;
        // Initialize randomized intervals
        float danger = _legStartDanger;
        _nextHazardInterval = SampleInterval(Mathf.Lerp(eventIntervalAtSafe, eventIntervalAtDanger, danger), hazardIntervalJitter, useExponentialTiming);
        _nextInjuryInterval = SampleInterval(Mathf.Lerp(injuryIntervalAtSafe, injuryIntervalAtDanger, CurrentCrewInjuryWeight(danger)), injuryIntervalJitter, useExponentialTiming);
    }

    private void ChooseNextPhase()
    {
        var previousPhase = _currentPhase;
        _phaseTimer = 0f;
        
        // Calculate current danger for event interval
        float danger = _legStartDanger;
        if (MissionManager.Instance != null && MissionManager.Instance.IsTravelling)
        {
            var t = MissionManager.Instance.SegmentProgress01;
            danger = Mathf.Lerp(_legStartDanger, _legEndDanger, t);
        }

        // Build normalized weights
        float wCruise = Mathf.Max(0f, _legPhaseWeights.Cruise);
        float wFlak = Mathf.Max(0f, _legPhaseWeights.Flak);
        float wFighters = Mathf.Max(0f, _legPhaseWeights.Fighters);
        float sum = wCruise + wFlak + wFighters;
        if (sum <= 0.0001f)
        {
            wCruise = cruiseBias; wFlak = (1f - cruiseBias) * 0.5f; wFighters = (1f - cruiseBias) * 0.5f;
            sum = wCruise + wFlak + wFighters;
        }
        wCruise /= sum; wFlak /= sum; wFighters /= sum;

        float r = Random.value;
        if (r < wCruise)
        {
            _currentPhase = HazardPhase.Cruise;
            _phaseDuration = Random.Range(cruiseMinDuration, cruiseMaxDuration);
            Debug.Log($"[Chaos] Phase transition: {previousPhase} → Cruise (duration: {_phaseDuration:F1}s, weights: C={wCruise:F2} F={wFlak:F2} Fi={wFighters:F2}, roll={r:F2})");
        }
        else if (r < wCruise + wFlak)
        {
            _currentPhase = HazardPhase.Flak;
            _phaseDuration = Random.Range(flakMinDuration, flakMaxDuration);
            Debug.Log($"[Chaos] Phase transition: {previousPhase} → Flak (duration: {_phaseDuration:F1}s, weights: C={wCruise:F2} F={wFlak:F2} Fi={wFighters:F2}, roll={r:F2})");
            EventLogUI.Instance?.Log("Flak bursts ahead!", Color.red);
            // Pause for phase announcement - major transition
            EventPopupUI.Instance?.Show("Flak bursts ahead!", Color.red, pause:true);
            // Start with partial timer so first event fires quickly (1-3 seconds into phase)
            float hazardIntervalMean = Mathf.Lerp(eventIntervalAtSafe, eventIntervalAtDanger, danger);
            _nextHazardInterval = Random.Range(1f, 3f);
            timeSinceLastHazard = 0f;
            Debug.Log($"[Chaos] First flak event in {_nextHazardInterval:F1}s (mean interval: {hazardIntervalMean:F1}s)");
        }
        else
        {
            _currentPhase = HazardPhase.Fighters;
            _phaseDuration = Random.Range(fightersMinDuration, fightersMaxDuration);
            Debug.Log($"[Chaos] Phase transition: {previousPhase} → Fighters (duration: {_phaseDuration:F1}s, weights: C={wCruise:F2} F={wFlak:F2} Fi={wFighters:F2}, roll={r:F2})");
            EventLogUI.Instance?.Log("Enemy fighters spotted!", Color.red);
            // Pause for phase announcement - major transition
            EventPopupUI.Instance?.Show("Enemy fighters spotted!", Color.red, pause:true);
            // Start with partial timer so first event fires quickly (1-3 seconds into phase)
            float hazardIntervalMean = Mathf.Lerp(eventIntervalAtSafe, eventIntervalAtDanger, danger);
            _nextHazardInterval = Random.Range(1f, 3f);
            timeSinceLastHazard = 0f;
            Debug.Log($"[Chaos] First fighter attack in {_nextHazardInterval:F1}s (mean interval: {hazardIntervalMean:F1}s)");
        }
    }
    
    private void GenerateFlakEvent(float danger)
    {
        if (PlaneManager.Instance == null) return;
        
        // Decide between section hit or engine hit based on engine weight
        float engineWeight = CurrentEngineDamageWeight(danger);
        bool hitEngine = Random.value < engineWeight;
        
        if (hitEngine)
        {
            // Hit an engine
            var engines = PlaneManager.Instance.Systems.Where(s => s.Type == SystemType.Engine && s.Integrity > 0).ToList();
            if (engines.Count > 0)
            {
                var engine = engines[Random.Range(0, engines.Count)];
                int damage = Random.Range(minEngineDamage, maxEngineDamage + 1);
                float fireChance = engineFireChance * Mathf.Lerp(0.5f, 1.5f, danger);
                
                PlaneManager.Instance.ApplyEngineHit(engine.Id, damage, fireChance);
                OnChaosEvent?.Invoke($"Flak hit {engine.Id}");
                Debug.Log($"[Chaos] Flak hits {engine.Id} for {damage} damage");
            }
        }
        else
        {
            // Hit a section
            if (PlaneManager.Instance.Sections == null || PlaneManager.Instance.Sections.Count == 0) return;
            var healthySections = PlaneManager.Instance.Sections.Where(s => s.Integrity > 0).ToList();
            if (healthySections.Count == 0) return;

            var section = healthySections[Random.Range(0, healthySections.Count)];
            int oldIntegrity = section.Integrity;
            
            float dmgT = CurrentPlaneDamageWeight(danger);
            int dmgMin = Mathf.RoundToInt(Mathf.Lerp(minDamage, maxDamage, Mathf.Clamp01(dmgT * 0.5f)));
            int dmgMax = Mathf.RoundToInt(Mathf.Lerp(minDamage, maxDamage, Mathf.Clamp01(0.5f + dmgT * 0.5f)));
            int damage = Random.Range(dmgMin, Mathf.Max(dmgMin+1, dmgMax));

            float fireWeight = CurrentFireWeight(danger);
            float fireChance = Mathf.Lerp(0.05f, 0.6f, fireWeight);
            PlaneManager.Instance.ApplyHitToSection(section.Id, damage, true, fireChance);
            
            // Destruction notification only - regular damage goes through DamageLogUI
            if (section.Integrity <= 0 && oldIntegrity > 0)
            {
                EventPopupUI.Instance?.Show($"{section.Id} destroyed!", Color.red, pause:false);
            }
            
            OnChaosEvent?.Invoke($"Flak hit {section.Id}");
            Debug.Log($"[Chaos] Flak hits {section.Id} for {damage}, integrity={section.Integrity}, fire={section.OnFire}");
        }
    }
    
    private void GenerateFighterEvent(float danger)
    {
        if (PlaneManager.Instance == null) return;

        // Fighters strafe: multiple lighter hits, can hit sections OR engines
        float dmgW = CurrentPlaneDamageWeight(danger);
        float engineW = CurrentEngineDamageWeight(danger);
        int passes = Random.Range(1, Random.value < dmgW ? 3 : 2);
        
        List<string> hitTargets = new List<string>();
        int totalDamage = 0;
        bool anyFires = false;
        
        for (int i = 0; i < passes; i++)
        {
            // 50/50 chance between section and engine hit (scaled by engine weight)
            bool hitEngine = Random.value < (engineW * 1.5f); // Fighters more likely to hit engines
            
            if (hitEngine)
            {
                var engines = PlaneManager.Instance.Systems.Where(s => s.Type == SystemType.Engine && s.Integrity > 0).ToList();
                if (engines.Count > 0)
                {
                    var engine = engines[Random.Range(0, engines.Count)];
                    int damage = Random.Range(minEngineDamage / 2, maxEngineDamage / 2 + 1); // Lighter than flak
                    float fireChance = engineFireChance * 0.6f; // Lower fire chance than flak
                    
                    PlaneManager.Instance.ApplyEngineHit(engine.Id, damage, fireChance);
                    if (!hitTargets.Contains(engine.Id)) hitTargets.Add(engine.Id);
                    totalDamage += damage;
                    if (engine.OnFire) anyFires = true;
                }
            }
            else
            {
                // Hit section
                if (PlaneManager.Instance.Sections == null || PlaneManager.Instance.Sections.Count == 0) continue;
                var viable = PlaneManager.Instance.Sections.Where(s => s.Integrity > 0).ToList();
                if (viable.Count == 0) break;
                var section = viable[Random.Range(0, viable.Count)];
                
                int dmgMin = Mathf.RoundToInt(Mathf.Lerp(2, minDamage, 0.5f));
                int dmgMax = Mathf.RoundToInt(Mathf.Lerp(minDamage, maxDamage, dmgW));
                int damage = Random.Range(dmgMin, Mathf.Max(dmgMin+1, dmgMax));
                float fireChance = Mathf.Lerp(0.05f, 0.25f, CurrentFireWeight(danger) * 0.7f);
                PlaneManager.Instance.ApplyHitToSection(section.Id, damage, true, fireChance);
                
                if (!hitTargets.Contains(section.Id)) hitTargets.Add(section.Id);
                totalDamage += damage;
                if (section.OnFire) anyFires = true;
            }
        }
        
        OnChaosEvent?.Invoke($"Fighter strafe {passes} passes");
        Debug.Log($"[Chaos] Fighters strafe: {passes} passes, {totalDamage} damage to {string.Join(", ", hitTargets)}");
    }
    
    private void GenerateCrewInjuryEvent(float danger)
    {
        if (CrewManager.Instance?.AllCrew == null || CrewManager.Instance.AllCrew.Count == 0) return;
        
        // Pick a random healthy crew member
        var healthyCrew = CrewManager.Instance.AllCrew.Where(c => c.Status == CrewStatus.Healthy).ToList();
        if (healthyCrew.Count == 0) return;
        
        var crewMember = healthyCrew[Random.Range(0, healthyCrew.Count)];
        
        // Determine injury severity using danger-scaled weights
        float wLight = Mathf.Max(0.0001f, Mathf.Lerp(lightSeverityWeightMin, lightSeverityWeightMax, danger));
        float wSerious = Mathf.Max(0.0001f, Mathf.Lerp(seriousSeverityWeightMin, seriousSeverityWeightMax, danger));
        float wCritical = Mathf.Max(0.0001f, Mathf.Lerp(criticalSeverityWeightMin, criticalSeverityWeightMax, danger));
        float sum = wLight + wSerious + wCritical;
        wLight /= sum; wSerious /= sum; wCritical /= sum;

        float r = Random.value;
        CrewStatus newStatus = r < wCritical ? CrewStatus.Critical : (r < wCritical + wSerious ? CrewStatus.Serious : CrewStatus.Light);
        
        // Apply injury through CrewManager (which will trigger the event)
        CrewManager.Instance.ApplyInjury(crewMember.Id, newStatus);
        
        string severity = newStatus switch
        {
            CrewStatus.Light => "lightly wounded",
            CrewStatus.Serious => "seriously wounded",
            CrewStatus.Critical => "CRITICALLY injured",
            _ => "injured"
        };
        
        Color outcomeColor = newStatus == CrewStatus.Critical ? Color.red : 
                            newStatus == CrewStatus.Serious ? new Color(1f, 0.5f, 0f) : 
                            Color.yellow;
        
        // Direct injury notification - no flavor text during combat
        EventPopupUI.Instance?.Show($"{crewMember.Name} ({crewMember.Role}) is {severity}!", outcomeColor, pause:false);
        Debug.Log($"[Chaos] {crewMember.Name} ({crewMember.Role}) is {severity}!");
    }
    
    /// <summary>
    /// For testing - force chaos events manually
    /// </summary>
    [ContextMenu("Force Flak Event")]
    public void ForceFlakEvent() => GenerateFlakEvent(0.5f);
    
    [ContextMenu("Force Fighter Event")]
    public void ForceFighterEvent() => GenerateFighterEvent(0.5f);
    
    [ContextMenu("Force Crew Injury")]
    public void ForceCrewInjury() => GenerateCrewInjuryEvent(0.5f);
    
    /// <summary>
    /// Toggle chaos on/off for peaceful testing
    /// </summary>
    public void SetChaosEnabled(bool enabled)
    {
        enableChaos = enabled;
        Debug.Log($"[Chaos] Chaos simulator {(enabled ? "enabled" : "disabled")}");
    }

    // ---------------------------
    // Helpers: weight curves
    // ---------------------------
    private float CurrentPlaneDamageWeight(float danger) => Mathf.Lerp(planeDamageWeightMin, planeDamageWeightMax, Mathf.Clamp01(danger));
    private float CurrentEngineDamageWeight(float danger) => Mathf.Lerp(engineDamageWeightMin, engineDamageWeightMax, Mathf.Clamp01(danger));
    private float CurrentFireWeight(float danger) => Mathf.Lerp(fireWeightMin, fireWeightMax, Mathf.Clamp01(danger));
    private float CurrentCrewInjuryWeight(float danger) => Mathf.Lerp(crewInjuryWeightMin, crewInjuryWeightMax, Mathf.Clamp01(danger));

    // Sample next interval from mean with jitter/exponential randomness
    private float SampleInterval(float mean, float jitter, bool exponential)
    {
        mean = Mathf.Max(0.001f, mean);
        if (exponential)
        {
            // Exponential with mean: -ln(U) * mean (U in (0,1))
            float u = Mathf.Clamp01(Random.value);
            u = Mathf.Max(1e-6f, u);
            return -Mathf.Log(u) * mean;
        }
        else
        {
            float minMul = Mathf.Max(0f, 1f - jitter);
            float maxMul = 1f + jitter;
            return mean * Random.Range(minMul, maxMul);
        }
    }
}