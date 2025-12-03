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

    /// <summary>
    /// Fuel remaining - now sourced from PlaneManager.
    /// </summary>
    public float FuelRemaining => PlaneManager.Instance != null ? PlaneManager.Instance.FuelRemaining : 0f;

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
    /// Miles for the current leg (taken from destination node DistanceMiles).
    /// </summary>
    private float _segmentDistanceMiles;

    /// <summary>
    /// 0–1 progress along the current leg (0 = just left, 1 = arrived).
    /// </summary>
    public float SegmentProgress01 => SegmentDuration > 0f ? Mathf.Clamp01(SegmentElapsed / SegmentDuration) : 0f;

    /// <summary>
    /// Miles for the current leg based on the destination node's DistanceMiles.
    /// </summary>
    public float CurrentLegDistanceMi
    {
        get
        {
            var next = GetNode(NextNodeId);
            return next != null ? Mathf.Max(0f, next.DistanceMiles) : 0f;
        }
    }

    /// <summary>
    /// Remaining miles on the current leg.
    /// </summary>
    public float DistanceRemainingMi
    {
        get
        {
            float total = CurrentLegDistanceMi;
            return total > 0f ? Mathf.Max(0f, total * (1f - SegmentProgress01)) : 0f;
        }
    }

    /// <summary>
    /// Are we currently moving along a segment between two nodes?
    /// </summary>
    public bool IsTravelling => !string.IsNullOrEmpty(NextNodeId);

    [Header("Cruise Settings")]
    [Tooltip("If true and DistanceMiles > 0, travel time derives from distance & dynamic speed instead of TravelTime.")] public bool useDistanceForTiming = true;
    [Tooltip("Automatically start first segment on play instead of entering node selection.")] public bool autoStartFirstSegment = true;
    [Tooltip("Scale factor applied to cruise speed for travel timing (faux speed to keep gameplay snappy).")]
    public float travelSpeedScale = 8f;
    

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
        // Fuel is now managed by PlaneManager
        
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

        if (autoStartFirstSegment)
        {
            // Attempt to auto start cruise toward first connected node
            var startNode = GetNode(CurrentNodeId);
            if (startNode != null && startNode.ConnectedNodeIds.Count > 0)
            {
                var nextId = startNode.ConnectedNodeIds[0];
                var nextNode = GetNode(nextId);
                if (nextNode != null)
                {
                    StartSegment(startNode, nextNode);
                    return;
                }
            }
            // Fallback: enter cruise even if no segment
            GameStateManager.Instance?.SetPhase(GamePhase.Cruise);
        }
        else
        {
            GameStateManager.Instance?.EnterNodeSelection();
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
    
        // Dynamic speed approach when using distance timing
        if (useDistanceForTiming && _segmentDistanceMiles > 0f)
        {
            float speedMph = PlaneManager.Instance != null ? PlaneManager.Instance.CurrentCruiseSpeedMph * Mathf.Max(0.1f, travelSpeedScale) : 0f;
            // Advance elapsed time
            SegmentElapsed += deltaTime;
            float distanceTraveled = (SegmentElapsed * speedMph) / 3600f; // mph -> miles per second
            float progress = Mathf.Clamp01(distanceTraveled / _segmentDistanceMiles);
            // Derive a dynamic duration for compatibility (time if speed stayed constant)
            SegmentDuration = _segmentDistanceMiles / Mathf.Max(1f, speedMph) * 3600f;
            OnSegmentProgressChanged?.Invoke(progress);
            if (progress >= 1f)
            {
                // Prevent ending during an active hazard phase
                if (ChaosSimulator.Instance != null && ChaosSimulator.Instance.IsInHazardPhase)
                {
                    // Wait until hazard phase returns to cruise
                }
                else
                {
                    CompleteSegment();
                }
            }
        }
        else
        {
            // Legacy travel time based approach
            SegmentElapsed += deltaTime;
            OnSegmentProgressChanged?.Invoke(SegmentProgress01);
            if (SegmentElapsed >= SegmentDuration)
            {
                if (ChaosSimulator.Instance != null && ChaosSimulator.Instance.IsInHazardPhase)
                {
                    // Wait until hazard phase returns to cruise
                }
                else
                {
                    CompleteSegment();
                }
            }
        }
    }

    // Adjust fuel and notify interested systems (used by GameEvent effects)
    // Now delegates to PlaneManager
    public void AdjustFuel(float delta)
    {
        if (PlaneManager.Instance != null)
        {
            PlaneManager.Instance.AdjustFuel(delta);
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
        _segmentDistanceMiles = Mathf.Max(0f, to.DistanceMiles);
        if (useDistanceForTiming && _segmentDistanceMiles > 0f)
        {
            float speedMph = PlaneManager.Instance != null ? PlaneManager.Instance.CurrentCruiseSpeedMph * Mathf.Max(0.1f, travelSpeedScale) : 0f;
            SegmentDuration = _segmentDistanceMiles / Mathf.Max(1f, speedMph) * 3600f; // initial estimate
        }
        else
        {
            SegmentDuration = to.TravelTime; // fallback
        }

        // Configure ChaosSimulator for this leg so it can schedule phases
        if (ChaosSimulator.Instance != null && to != null)
        {
            ChaosSimulator.Instance.ConfigureLeg(
                to.StartDanger,
                to.EndDanger,
                to.PhaseWeights
            );
        }

        OnSegmentStarted?.Invoke(from, to);

        // Log departure for player feedback
        EventLogUI.Instance?.Log($"Departing {from.Id} → {to.Id}", Color.white);

        // Enter cruise phase if we're not already in it
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.SetPhase(GamePhase.Cruise);
        }
    }

    private void CompleteSegment()
    {
        // Ensure elapsed reflects final state
        if (useDistanceForTiming && _segmentDistanceMiles > 0f)
        {
            float speedMph = PlaneManager.Instance != null ? PlaneManager.Instance.CurrentCruiseSpeedMph * Mathf.Max(0.1f, travelSpeedScale) : 0f;
            SegmentElapsed = _segmentDistanceMiles / Mathf.Max(1f, speedMph) * 3600f;
        }
        else
        {
            SegmentElapsed = SegmentDuration;
        }
        var arrivedNode = GetNode(NextNodeId);

        // Update current node
        CurrentNodeId = NextNodeId;
        NextNodeId = null;
        SegmentElapsed = 0f;
        SegmentDuration = 0f;
        _segmentDistanceMiles = 0f;

        // Fuel is now consumed continuously in PlaneManager.TickFuel() based on engine power
        // No longer deducting fuel per waypoint

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

    // Speed now sourced from PlaneManager.CurrentCruiseSpeedMph
}
