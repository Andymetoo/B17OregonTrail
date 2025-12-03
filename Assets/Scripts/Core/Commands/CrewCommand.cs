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
    public float SuccessChance;
    public bool UseConsumable;

    public ExtinguishFireCommand(string crewId, string sectionId, float duration = 8f, float successChance = 0.75f, bool useConsumable = false)
        : base(crewId)
    {
        SectionId = sectionId;
        Duration = duration;
        SuccessChance = successChance;
        UseConsumable = useConsumable;
    }

    public override void Execute(CrewManager crewMgr, PlaneManager planeMgr)
    {
        var action = new CrewAction
        {
            Type = ActionType.ExtinguishFire,
            TargetId = SectionId,
            Duration = Duration,
            Elapsed = 0f,
            SuccessChance = SuccessChance,
            UsesConsumable = UseConsumable,
            ConsumableType = SupplyType.FireExtinguisher
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
    public float SuccessChance;
    public bool UseConsumable;

    public RepairSystemCommand(string crewId, string systemId, float duration = 10f, float successChance = 0.70f, bool useConsumable = false)
        : base(crewId)
    {
            SystemId = systemId;
            Duration = duration;
            SuccessChance = successChance;
            UseConsumable = useConsumable;
        }

    public override void Execute(CrewManager crewMgr, PlaneManager planeMgr)
    {
        var config = CrewActionConfig.Instance;
        var upgrades = UpgradeManager.Instance;
        int repairAmount = 0;
        
        if (config != null)
        {
            if (UseConsumable)
            {
                int min = upgrades != null ? upgrades.GetModifiedRepairKitAmountMin() : config.repairKitAmountMin;
                int max = upgrades != null ? upgrades.GetModifiedRepairKitAmountMax() : config.repairKitAmountMax;
                repairAmount = config.SampleRepairAmount(min, max);
            }
            else
            {
                int min = upgrades != null ? upgrades.GetModifiedRepairAmountMin() : config.baseRepairAmountMin;
                int max = upgrades != null ? upgrades.GetModifiedRepairAmountMax() : config.baseRepairAmountMax;
                repairAmount = config.SampleRepairAmount(min, max);
            }
        }

        var action = new CrewAction
        {
            Type = ActionType.Repair,
            TargetId = SystemId,
            Duration = Duration,
            Elapsed = 0f,
            SuccessChance = SuccessChance,
            UsesConsumable = UseConsumable,
            ConsumableType = SupplyType.RepairKit,
            RepairAmount = repairAmount
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
    public float SuccessChance;
    public bool UseConsumable;

    public TreatInjuryCommand(string medicCrewId, string targetCrewId, float duration = 10f, float successChance = 0.65f, bool useConsumable = false)
        : base(medicCrewId)
    {
        TargetCrewId = targetCrewId;
        Duration = duration;
        SuccessChance = successChance;
        UseConsumable = useConsumable;
    }

    public override void Execute(CrewManager crewMgr, PlaneManager planeMgr)
    {
        var action = new CrewAction
        {
            Type = ActionType.TreatInjury,
            TargetId = TargetCrewId,
            Duration = Duration,
            Elapsed = 0f,
            SuccessChance = SuccessChance,
            UsesConsumable = UseConsumable,
            ConsumableType = SupplyType.MedKit
        };

        crewMgr.TryAssignAction(CrewId, action);
    }
}

/// <summary>
/// Order a crew member to feather an engine.
/// </summary>
[Serializable]
public class FeatherEngineCommand : CrewCommand
{
    public string EngineId;
    public float Duration;

    public FeatherEngineCommand(string crewId, string engineId, float duration = 5f)
        : base(crewId)
    {
        EngineId = engineId;
        Duration = duration;
    }

    public override void Execute(CrewManager crewMgr, PlaneManager planeMgr)
    {
        var action = new CrewAction
        {
            Type = ActionType.FeatherEngine,
            TargetId = EngineId,
            Duration = Duration,
            Elapsed = 0f
        };

        crewMgr.TryAssignAction(CrewId, action);
    }
}

public class RestartEngineCommand : CrewCommand
{
    public string EngineId;
    public float Duration;

    public RestartEngineCommand(string crewId, string engineId, float duration = 8f) : base(crewId)
    {
        EngineId = engineId;
        Duration = duration;
    }

    public override void Execute(CrewManager crewMgr, PlaneManager planeMgr)
    {
        var action = new CrewAction
        {
            Type = ActionType.RestartEngine,
            TargetId = EngineId,
            Duration = Duration,
            Elapsed = 0f
        };

        crewMgr.TryAssignAction(CrewId, action);
    }
}

