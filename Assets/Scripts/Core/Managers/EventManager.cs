using System;
using UnityEngine;

public class EventManager : MonoBehaviour
{
    public static EventManager Instance { get; private set; }

    [Header("Check Intervals (seconds)")]
    [Tooltip("How often to roll for flak while in a dangerous area.")]
    public float flakCheckInterval = 5f;

    [Tooltip("How often to roll for fighter attacks.")]
    public float fighterCheckInterval = 10f;

    [Tooltip("How often to roll for generic incidents (crew accidents, etc.).")]
    public float incidentCheckInterval = 15f;

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

            OnFlakEvent?.Invoke();
        }
    }

    private void TryTriggerFighters(ThreatProfile threats)
    {
        float roll = UnityEngine.Random.value;
        if (roll <= threats.FighterChance)
        {
            // Notify systems that we've entered a fighter encounter.
            OnFighterEncounter?.Invoke();

            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.EnterFighterCombat();
            }

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
            OnIncidentEvent?.Invoke();

            // Example stub:
            // - random crew member gets Light injury
            // - or random system gets Damaged
            // You can hook those into CrewManager / PlaneManager later.
        }
    }

    // Optional: helpers to reset timers, e.g., when a new segment starts.
    public void ResetTimers()
    {
        _flakTimer = 0f;
        _fighterTimer = 0f;
        _incidentTimer = 0f;
    }
}
