using System;
using System.Collections.Generic;
using UnityEngine;

public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance { get; private set; }

    [Header("Mission Data")]
    [Tooltip("All mission nodes that form the path graph for this mission.")]
    public List<MissionNode> missionNodes = new List<MissionNode>();

    [Tooltip("Id of the node where the mission starts.")]
    public string startNodeId;

    [Tooltip("Id of the node that represents the bomb target.")]
    public string bombTargetNodeId;

    [Header("Fuel")]
    [Tooltip("Starting fuel amount for the mission.")]
    public float startingFuel = 100f;

    public float FuelRemaining { get; private set; }

    /// <summary>
    /// Id of the node the plane is currently at.
    /// </summary>
    public string CurrentNodeId { get; private set; }

    /// <summary>
    /// Id of the node we are currently traveling toward (null/empty if not traveling).
    /// </summary>
    public string NextNodeId { get; private set; }

    /// <summary>
    /// Seconds elapsed along the current leg.
    /// </summary>
    public float SegmentElapsed { get; private set; }

    /// <summary>
    /// Total seconds for the current leg.
    /// </summary>
    public float SegmentDuration { get; private set; }

    /// <summary>
    /// 0–1 progress along the current leg (0 = just left, 1 = arrived).
    /// </summary>
    public float SegmentProgress01 => SegmentDuration > 0f ? Mathf.Clamp01(SegmentElapsed / SegmentDuration) : 0f;

    /// <summary>
    /// Are we currently moving along a segment between two nodes?
    /// </summary>
    public bool IsTravelling => !string.IsNullOrEmpty(NextNodeId);

    // Quick lookup by Id
    private Dictionary<string, MissionNode> _nodeLookup = new Dictionary<string, MissionNode>();

    // Events
    public event Action<MissionNode, MissionNode> OnSegmentStarted;       // fromNode, toNode
    public event Action<MissionNode> OnSegmentCompleted;                  // arrivedNode
    public event Action<MissionNode> OnArrivedAtTarget;                   // target node
    public event Action<float> OnFuelChanged;                             // new fuel value
    public event Action<float> OnSegmentProgressChanged;                  // 0–1

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuildLookup();
        InitializeMissionState();
    }

    private void BuildLookup()
    {
        _nodeLookup.Clear();
        foreach (var node in missionNodes)
        {
            if (node == null || string.IsNullOrEmpty(node.Id)) continue;
            if (_nodeLookup.ContainsKey(node.Id)) continue; // avoid dup key crash
            _nodeLookup.Add(node.Id, node);
        }
    }

    private void InitializeMissionState()
    {
        FuelRemaining = startingFuel;
        OnFuelChanged?.Invoke(FuelRemaining);

        if (string.IsNullOrEmpty(startNodeId))
        {
            // Fall back to first node if start not set.
            if (missionNodes.Count > 0)
            {
                CurrentNodeId = missionNodes[0].Id;
            }
        }
        else
        {
            CurrentNodeId = startNodeId;
        }

        NextNodeId = null;
        SegmentElapsed = 0f;
        SegmentDuration = 0f;

        // At start of mission, we usually want to go to node selection.
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.EnterNodeSelection();
        }
    }

    /// <summary>
    /// Called once per simulation tick by GameStateManager, 
    /// but only actually moves us if we are in Cruise and traveling.
    /// </summary>
    public void Tick(float deltaTime)
    {
        if (!IsTravelling) return;

        // Optionally, only advance in Cruise phase
        if (GameStateManager.Instance != null &&
            GameStateManager.Instance.CurrentPhase != GamePhase.Cruise)
        {
            return;
        }

        SegmentElapsed += deltaTime;
        OnSegmentProgressChanged?.Invoke(SegmentProgress01);

        if (SegmentElapsed >= SegmentDuration)
        {
            CompleteSegment();
        }
    }

    // ------------------------------------------------------------------
    // NODE / SEGMENT CONTROL
    // ------------------------------------------------------------------

    /// <summary>
    /// UI or other systems call this when the player selects a connected node
    /// to travel to from the current node.
    /// </summary>
    public bool TryChooseNextNode(string nodeId)
    {
        if (string.IsNullOrEmpty(CurrentNodeId)) return false;
        if (IsTravelling) return false; // already on a leg

        var current = GetNode(CurrentNodeId);
        var next = GetNode(nodeId);

        if (current == null || next == null) return false;

        // Check if the requested node is connected
        if (!current.ConnectedNodeIds.Contains(nodeId))
        {
            // Not a valid adjacent node
            return false;
        }

        StartSegment(current, next);
        return true;
    }

    private void StartSegment(MissionNode from, MissionNode to)
    {
        CurrentNodeId = from.Id;
        NextNodeId = to.Id;
        SegmentElapsed = 0f;
        SegmentDuration = to.TravelTime;

        OnSegmentStarted?.Invoke(from, to);

        // Enter cruise phase if we're not already in it
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetPhase(GamePhase.Cruise);
        }
    }

    private void CompleteSegment()
    {
        SegmentElapsed = SegmentDuration;
        var arrivedNode = GetNode(NextNodeId);

        // Update current node
        CurrentNodeId = NextNodeId;
        NextNodeId = null;
        SegmentElapsed = 0f;
        SegmentDuration = 0f;

        // Consume fuel based on the node we arrived at
        if (arrivedNode != null)
        {
            FuelRemaining = Mathf.Max(0f, FuelRemaining - arrivedNode.FuelCost);
            OnFuelChanged?.Invoke(FuelRemaining);
        }

        OnSegmentCompleted?.Invoke(arrivedNode);

        // Check if we've reached the bomb target
        if (arrivedNode != null && !string.IsNullOrEmpty(bombTargetNodeId) &&
            arrivedNode.Id == bombTargetNodeId)
        {
            OnArrivedAtTarget?.Invoke(arrivedNode);

            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.EnterBombRun();
            }
        }
        else
        {
            // Otherwise, go back to node selection for next leg
            if (GameStateManager.Instance != null)
            {
                GameStateManager.Instance.EnterNodeSelection();
            }
        }
    }

    public MissionNode GetNode(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId)) return null;
        _nodeLookup.TryGetValue(nodeId, out var node);
        return node;
    }
}
