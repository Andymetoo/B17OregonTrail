using System;
using UnityEngine;

/// <summary>
/// Base class for all crew-related commands.
/// UI will create these and enqueue them into CrewCommandProcessor,
/// which will execute them against CrewManager / PlaneManager.
/// </summary>
[Serializable]
public abstract class CrewCommand
{
    public string CrewId;

    protected CrewCommand(string crewId)
    {
        CrewId = crewId;
    }

    /// <summary>
    /// Execute this command. Usually this means scheduling a CrewAction
    /// via CrewManager, not applying instant effects.
    /// </summary>
    public abstract void Execute(CrewManager crewMgr, PlaneManager planeMgr);
}

/// <summary>
/// Order a crew member to move to a station (e.g. TailGun, Nose).
/// </summary>
[Serializable]
public class MoveCrewCommand : CrewCommand
{
    public string StationId;
    public float MoveDuration;

    public MoveCrewCommand(string crewId, string stationId, float moveDuration = 5f)
        : base(crewId)
    {
        StationId = stationId;
        MoveDuration = moveDuration;
    }

    public override void Execute(CrewManager crewMgr, PlaneManager planeMgr)
    {
        var action = new CrewAction
        {
            Type = ActionType.Move,
            TargetId = StationId,
            Duration = MoveDuration,
            Elapsed = 0f
        };

        crewMgr.TryAssignAction(CrewId, action);
    }
}

/// <summary>
/// Order a crew member to fight a fire in a given section.
/// </summary>
[Serializable]
public class ExtinguishFireCommand : CrewCommand
{
    public string SectionId;
    public float Duration;

    public ExtinguishFireCommand(string crewId, string sectionId, float duration = 8f)
        : base(crewId)
    {
        SectionId = sectionId;
        Duration = duration;
    }

    public override void Execute(CrewManager crewMgr, PlaneManager planeMgr)
    {
        var action = new CrewAction
        {
            Type = ActionType.ExtinguishFire,
            TargetId = SectionId,
            Duration = Duration,
            Elapsed = 0f
        };

        crewMgr.TryAssignAction(CrewId, action);
    }
}

/// <summary>
/// Order a crew member to repair a system (engine, turret, etc.).
/// </summary>
[Serializable]
public class RepairSystemCommand : CrewCommand
{
    public string SystemId;
    public float Duration;

    public RepairSystemCommand(string crewId, string systemId, float duration = 10f)
        : base(crewId)
    {
            SystemId = systemId;
            Duration = duration;
        }

    public override void Execute(CrewManager crewMgr, PlaneManager planeMgr)
    {
        var action = new CrewAction
        {
            Type = ActionType.Repair,
            TargetId = SystemId,
            Duration = Duration,
            Elapsed = 0f
        };

        crewMgr.TryAssignAction(CrewId, action);
    }
}

/// <summary>
/// Order a crew member to treat an injured crewmate.
/// </summary>
[Serializable]
public class TreatInjuryCommand : CrewCommand
{
    public string TargetCrewId;
    public float Duration;

    public TreatInjuryCommand(string medicCrewId, string targetCrewId, float duration = 10f)
        : base(medicCrewId)
    {
        TargetCrewId = targetCrewId;
        Duration = duration;
    }

    public override void Execute(CrewManager crewMgr, PlaneManager planeMgr)
    {
        var action = new CrewAction
        {
            Type = ActionType.TreatInjury,
            TargetId = TargetCrewId,
            Duration = Duration,
            Elapsed = 0f
        };

        crewMgr.TryAssignAction(CrewId, action);
    }
}
