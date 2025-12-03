using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CrewMember {
    public string Id;
    public string Name;
    public CrewRole Role;
    public CrewStatus Status;

    public string CurrentStationId; // e.g. "TailGun", "Nose", "RadioRoom"
    public CrewAction CurrentAction;

    public float InjuryTimer; // seconds until next injury stage or death
    
    // --- STATION ASSIGNMENT ---
    public StationType DefaultStation = StationType.None;  // Station this crew is assigned to by default
    public StationType CurrentStation = StationType.None;  // Station they are currently occupying (None if not at a station)
    
    // --- VISUAL & MOVEMENT ---
    public Vector2 HomePosition;      // Where this crew's station is located on screen
    public Vector2 CurrentPosition;   // Current screen position (for movement lerp)
    public Vector2 HomeOffset;        // Optional per-crew offset added to station position
    public float MoveSpeed = 50f;     // Pixels per second movement speed
    public CrewVisualState VisualState = CrewVisualState.IdleAtStation;
    
    // --- ACTION STATUS FEEDBACK ---
    public string ActionStatusText;   // Status message displayed in UI (e.g., "Cannot pass through fire!")
}

[Serializable]
public class CrewAction {
    public ActionType Type;
    public string TargetId;  // sectionId, systemId, crewId, stationId
    public float Duration;
    public float Elapsed;
    
    // --- MOVEMENT TRACKING ---
    public ActionPhase Phase = ActionPhase.MoveToTarget;  // Current phase of the action
    public Vector2 TargetPosition;  // Screen position to move to for this action
    public Vector2 ReturnPosition;  // Where to return after action completes
    
    // --- PATH WAYPOINTS ---
    public List<Vector2> Waypoints; // Optional ordered points to traverse before reaching TargetPosition
    public int CurrentWaypointIndex = 0;

    // --- SECTION CONTEXT (for return pathing)
    public string TargetSectionId; // resolved section id for section-based actions
    public List<Vector2> ReturnWaypoints; // path to traverse on the way back to ReturnPosition
    public int ReturnWaypointIndex = 0;

    // --- CONSUMABLE & SUCCESS TRACKING ---
    public bool UsesConsumable;          // True if using a consumable item (medkit, repair kit, fire extinguisher)
    public SupplyType ConsumableType;    // Which consumable type is being used
    public float SuccessChance;          // Probability of success (0-1); rolled when action completes
    public bool RolledSuccess;           // Result of the success roll (set after Performing phase)
    public int RepairAmount;             // For repair actions: how much integrity to restore (sampled from config)
    
    // --- STATION TRACKING (for temporary vacation during actions) ---
    public StationType PreviousStation = StationType.None;  // Station vacated when starting this action (to restore on return)

    public bool IsComplete => Elapsed >= Duration;
}

/// <summary>
/// Multi-phase action flow: Move to target → Perform action → Move back to station
/// </summary>
public enum ActionPhase {
    MoveToTarget,   // Crew is walking to the target location
    Performing,     // Crew is performing the action (repair, medical, etc.)
    Returning       // Crew is walking back to their station
}

/// <summary>
/// Visual state of crew member for sprite selection
/// </summary>
public enum CrewVisualState {
    IdleAtStation,  // Standing at their station
    Moving,         // Walking to/from a target
    Working         // Performing an action (repair, medical, fire)
}

[Serializable]
public class PlaneSectionState {
    public string Id;        // "Nose", "Cockpit", "LeftWing", etc.
    public int Integrity;    // 0–100 health
    public bool OnFire;
    
    // Fire damage tracking (not serialized, runtime only)
    [NonSerialized] public float FireDamageAccumulator; // Accumulates fractional damage between ticks
    [NonSerialized] public int LastFireDamageThreshold; // Last 10-point threshold we logged (90, 80, 70, etc.)
}

[Serializable]
public class PlaneSystemState {
    public string Id;        // "Engine1", "MainOxygen", "TopTurret"
    public SystemType Type;
    public SystemStatus Status;
    public SpecialState Special;
    public string SectionId; // where it physically lives
    
    // Engine-specific fields
    public int Integrity = 100;          // 0-100, engines can be damaged like sections
    public bool OnFire = false;          // Engines can catch fire
    public bool IsFeathered = false;     // Whether engine has been feathered (stopped to prevent drag/damage)
    
    // Fire damage tracking (non-serialized runtime state)
    [NonSerialized] public float FireDamageAccumulator; // Accumulated damage waiting to be applied
    [NonSerialized] public int LastFireDamageThreshold;  // Last 10-point threshold logged (90, 80, 70, etc.)
}

[Serializable]
public class ThreatProfile {
    public float FighterChance;
    public float FlakChance;
    public float IncidentChance;
}

[Serializable]
public class LegPhaseWeights {
    [Range(0f,1f)] public float Cruise = 0.6f;
    [Range(0f,1f)] public float Flak = 0.2f;
    [Range(0f,1f)] public float Fighters = 0.2f;
}

[Serializable]
public class MissionNode {
    public string Id;
    public List<string> ConnectedNodeIds = new List<string>();

    public float TravelTime; // seconds along this leg
    public float FuelCost;   // fuel burned to traverse this node
    public float DistanceMiles; // distance to reach this node from the previous one

    public ThreatProfile Threats;

    // Leg configuration when travelling TO this node
    [Header("Leg Danger (0-1) to this node")]
    [Range(0f,1f)] public float StartDanger = 0f;
    [Range(0f,1f)] public float EndDanger = 0.6f;

    [Header("Phase Weights (choose next segment type)")]
    public LegPhaseWeights PhaseWeights = new LegPhaseWeights();
}

public enum CrewRole {
    Pilot, Copilot, Navigator, Bombardier, RadioOp, Gunner, Engineer
}

public enum CrewStatus {
    Healthy, Light, Serious, Critical, Unconscious, Dead
}

public enum ActionType {
    Idle,
    Move,
    ExtinguishFire,
    TreatInjury,
    Repair,
    ManStation,
    OccupyStation,  // Move to and occupy an unmanned station
    FeatherEngine   // Feather an engine to prevent drag/further damage
}

public enum SystemType {
    Engine,
    Oxygen,
    Fuel,
    Electrical,
    Turret,
    LandingGear,
    Hydraulics
}

public enum SystemStatus {
    Operational,
    Damaged,
    Destroyed
}

public enum SpecialState {
    None,
    OnFire,
    Leaking,
    Jammed
}

/// <summary>
/// Defines all operable crew stations on the B-17.
/// These are positions where crew can be assigned to perform specific duties.
/// </summary>
public enum StationType {
    // Combat Stations (guns)
    TopTurret,
    BallTurret,
    LeftWaistGun,
    RightWaistGun,
    TailGun,
    
    // Flight Deck
    Pilot,
    CoPilot,
    
    // Specialized Positions (Navigator & Bombardier each operate a nose gun in B-17F)
    Navigator,
    RadioOperator,
    Bombardier,
    
    // Non-Station (for crew without assigned posts)
    None
}

/// <summary>
/// Tracks the state of a single crew station.
/// </summary>
[Serializable]
public class StationState {
    public StationType Type;
    public string StationId;         // e.g. "TailGun", "Pilot" - matches station IDs in scene
    public string SectionId;         // Which plane section this station is in
    public string OccupiedByCrewId;  // Crew member ID currently manning this station, or null if empty
    public bool IsOperational;       // False if station is destroyed/inoperable
    
    public bool IsOccupied => !string.IsNullOrEmpty(OccupiedByCrewId);
    public bool IsAvailable => IsOperational && !IsOccupied;
}

