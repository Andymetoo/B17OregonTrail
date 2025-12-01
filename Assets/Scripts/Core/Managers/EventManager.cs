using System;
using UnityEngine;

public class EventManager : MonoBehaviour
{
    public static EventManager Instance { get; private set; }

    [Header("Check Intervals (seconds)")]
    [Tooltip("Safe interval between flak checks (danger=0).")]
    public float flakIntervalSafe = 12f;
    [Tooltip("Interval between flak checks at max danger (danger=1).")]
    public float flakIntervalDanger = 4f;
    [Tooltip("Safe interval between fighter checks (danger=0).")]
    public float fighterIntervalSafe = 25f;
    [Tooltip("Interval between fighter checks at max danger (danger=1).")]
    public float fighterIntervalDanger = 8f;
    [Tooltip("Safe interval between incident checks (danger=0).")]
    public float incidentIntervalSafe = 40f;
    [Tooltip("Interval between incident checks at max danger (danger=1).")]
    public float incidentIntervalDanger = 15f;

    [Header("Danger Scaling 0-1")] 
    [Range(0f,1f)] public float danger01 = 0f;
    [Tooltip("If true, danger value auto-lerps from start to end of segment using progress.")] public bool autoDangerByProgress = true;
    [Range(0f,1f)] public float minSegmentDanger = 0f;
    [Range(0f,1f)] public float maxSegmentDanger = 0.6f;

    [Header("Flak Settings")]
    [Tooltip("Base damage from a single flak hit.")]
    public int flakDamage = 10;

    [Tooltip("Chance that a flak hit will start a fire in the impacted section.")]
    [Range(0f, 1f)]
    public float flakFireStartChance = 0.3f;

    private float _flakTimer;
    private float _fighterTimer;
    private float _incidentTimer;

    // Events other systems/UI can subscribe to
    public event Action OnFlakEvent;              // Fired when flak actually hits
    public event Action OnFighterEncounter;       // Fired when fighters spawn
    public event Action OnIncidentEvent;          // Generic non-combat incident

    private void Start()
    {
        // Reset interval timers whenever a new segment starts
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.OnSegmentStarted += (_, __) => ResetTimers();
        }
    }

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
    /// Called once per simulation tick by GameStateManager.
    /// </summary>
    public void Tick(float deltaTime)
    {
        // Need mission and game state information to do anything.
        if (MissionManager.Instance == null || GameStateManager.Instance == null)
            return;

        // Only roll events while traveling in Cruise phase.
        if (!MissionManager.Instance.IsTravelling)
            return;

        if (GameStateManager.Instance.CurrentPhase != GamePhase.Cruise)
            return;

        var nextNodeId = MissionManager.Instance.NextNodeId;
        if (string.IsNullOrEmpty(nextNodeId))
            return;

        var nextNode = MissionManager.Instance.GetNode(nextNodeId);
        if (nextNode == null || nextNode.Threats == null)
            return;

        var threats = nextNode.Threats;

        // Auto danger progression (optional)
        if (autoDangerByProgress)
        {
            danger01 = Mathf.Lerp(minSegmentDanger, maxSegmentDanger, MissionManager.Instance.SegmentProgress01);
        }

        // Compute dynamic intervals based on danger (higher danger = shorter intervals)
        float flakCheckInterval = Mathf.Lerp(flakIntervalSafe, flakIntervalDanger, danger01);
        float fighterCheckInterval = Mathf.Lerp(fighterIntervalSafe, fighterIntervalDanger, danger01);
        float incidentCheckInterval = Mathf.Lerp(incidentIntervalSafe, incidentIntervalDanger, danger01);

        // Advance internal timers
        _flakTimer += deltaTime;
        _fighterTimer += deltaTime;
        _incidentTimer += deltaTime;

        // Roll for flak
        if (_flakTimer >= flakCheckInterval)
        {
            _flakTimer = 0f;
            TryTriggerFlak(threats);
        }

        // Roll for fighters
        if (_fighterTimer >= fighterCheckInterval)
        {
            _fighterTimer = 0f;
            TryTriggerFighters(threats);
        }

        // Roll for generic incidents (stubbed for later)
        if (_incidentTimer >= incidentCheckInterval)
        {
            _incidentTimer = 0f;
            TryTriggerIncident(threats);
        }
    }

    // ------------------------------------------------------------------
    // INDIVIDUAL EVENT ROLL METHODS
    // ------------------------------------------------------------------

    private void TryTriggerFlak(ThreatProfile threats)
    {
        // FighterChance, FlakChance, IncidentChance are all 0–1 probabilities
        // We can treat FlakChance as probability per interval.
        float roll = UnityEngine.Random.value;
        if (roll <= threats.FlakChance)
        {
            // Apply a random hit to the plane.
            if (PlaneManager.Instance != null)
            {
                PlaneManager.Instance.ApplyRandomHit(flakDamage, canStartFire: true, fireStartChance: flakFireStartChance);
            }

            EventLogUI.Instance?.Log("Flak bursts ahead!", Color.red);
            OnFlakEvent?.Invoke();
            // Pause flow and show a modal popup like Oregon Trail (if available)
            EventPopupUI.Instance?.Show("Flak bursts ahead!", Color.red, pause:true);
            // Slight danger bump on flak event (manual escalation)
            danger01 = Mathf.Clamp01(danger01 + 0.05f);
        }
    }

    private void TryTriggerFighters(ThreatProfile threats)
    {
        float roll = UnityEngine.Random.value;
        if (roll <= threats.FighterChance)
        {
            // Notify systems that we've entered a fighter encounter.
            EventLogUI.Instance?.Log("Enemy fighters spotted!", Color.red);
            // Pause and notify player, switch to FighterCombat after continue
            EventPopupUI.Instance?.Show("Enemy fighters spotted!", Color.red, pause:true, onContinueAction: () => {
                OnFighterEncounter?.Invoke();
                GameStateManager.Instance?.EnterFighterCombat();
            });
            danger01 = Mathf.Clamp01(danger01 + 0.1f);

            // In a real implementation, you might also notify a CombatManager
            // to set up specific fighter waves based on threat severity, etc.
        }
    }

    private void TryTriggerIncident(ThreatProfile threats)
    {
        float roll = UnityEngine.Random.value;
        if (roll <= threats.IncidentChance)
        {
            // For now, just fire an event.
            // Later, you can implement actual incident logic (crew injury,
            // mechanical failure, navigation error, etc.)
            EventLogUI.Instance?.Log("Radio chatter: Allied crew hails you.", Color.cyan);
            OnIncidentEvent?.Invoke();
            EventPopupUI.Instance?.Show("Radio chatter: Allied crew hails you.", Color.cyan, pause:true);

            // Example stub:
            // - random crew member gets Light injury
            // - or random system gets Damaged
            // You can hook those into CrewManager / PlaneManager later.
            danger01 = Mathf.Clamp01(danger01 + 0.02f);
        }
    }

    // Optional: helpers to reset timers, e.g., when a new segment starts.
    public void ResetTimers()
    {
        _flakTimer = 0f;
        _fighterTimer = 0f;
        _incidentTimer = 0f;
        // Optionally reset danger on new leg start
        if (autoDangerByProgress) danger01 = minSegmentDanger;
    }

    /// <summary>
    /// External systems/UI can set danger manually (0-1).
    /// </summary>
    public void SetDanger(float value)
    {
        danger01 = Mathf.Clamp01(value);
    }
}
