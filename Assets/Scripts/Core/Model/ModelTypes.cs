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
    
    // --- VISUAL & MOVEMENT ---
    public Vector2 HomePosition;      // Where this crew's station is located on screen
    public Vector2 CurrentPosition;   // Current screen position (for movement lerp)
    public float MoveSpeed = 50f;     // Pixels per second movement speed
    public CrewVisualState VisualState = CrewVisualState.IdleAtStation;
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
}

[Serializable]
public class PlaneSystemState {
    public string Id;        // "Engine1", "MainOxygen", "TopTurret"
    public SystemType Type;
    public SystemStatus Status;
    public SpecialState Special;
    public string SectionId; // where it physically lives
}

[Serializable]
public class ThreatProfile {
    public float FighterChance;
    public float FlakChance;
    public float IncidentChance;
}

[Serializable]
public class MissionNode {
    public string Id;
    public List<string> ConnectedNodeIds = new List<string>();

    public float TravelTime; // seconds along this leg
    public float FuelCost;   // fuel burned to traverse this node

    public ThreatProfile Threats;
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
    ManStation
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
