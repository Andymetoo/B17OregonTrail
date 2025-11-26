using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central game state that can be serialized for save/load and debugging.
/// This is the "snapshot" of the entire simulation at any point.
/// </summary>
[Serializable]
public class GameState
{
    [Header("Mission Progress")]
    public string currentMissionId;
    public string currentNodeId;
    public string nextNodeId;
    public float segmentProgress01; // 0.0 to 1.0 along current segment
    public float missionTime; // Total time elapsed this mission
    
    [Header("Plane State")]
    public List<PlaneSectionState> planeSections = new List<PlaneSectionState>();
    public List<PlaneSystemState> planeSystems = new List<PlaneSystemState>();
    public float fuelRemaining;
    public float oxygenLevel;
    
    [Header("Crew State")]
    public List<CrewMember> crewMembers = new List<CrewMember>();
    
    [Header("Inventory")]
    public int medKits;
    public int fireExtinguishers;
    public int repairKits;
    public int ammunition;

    /// <summary>
    /// Serialize to JSON for debugging/saving
    /// </summary>
    public string ToJson()
    {
        return JsonUtility.ToJson(this, true);
    }
    
    /// <summary>
    /// Load from JSON
    /// </summary>
    public static GameState FromJson(string json)
    {
        return JsonUtility.FromJson<GameState>(json);
    }
}