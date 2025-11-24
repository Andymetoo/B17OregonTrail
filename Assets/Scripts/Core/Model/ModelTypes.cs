using System;
using System.Collections.Generic;

[Serializable]
public class CrewMember {
    public string Id;
    public string Name;
    public CrewRole Role;
    public CrewStatus Status;

    public string CurrentStationId; // e.g. "TailGun", "Nose", "RadioRoom"
    public CrewAction CurrentAction;

    public float InjuryTimer; // seconds until next injury stage or death
}

[Serializable]
public class CrewAction {
    public ActionType Type;
    public string TargetId;  // sectionId, systemId, crewId, stationId
    public float Duration;
    public float Elapsed;

    public bool IsComplete => Elapsed >= Duration;
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
