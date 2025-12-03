using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CrewManager : MonoBehaviour
{
    public static CrewManager Instance { get; private set; }

    // The actual crew data is stored here
    public List<CrewMember> AllCrew = new List<CrewMember>();

    // Events for UI or other systems to subscribe to
    public event Action<CrewMember> OnCrewActionAssigned;
    public event Action<CrewMember> OnCrewActionCompleted;
    public event Action<CrewMember> OnCrewActionCancelled;
    public event Action<CrewMember> OnCrewInjuryStageChanged;
    public event Action<CrewMember> OnCrewDied;

    // Exclusive target locks so only one crew can act on a target
    private readonly HashSet<string> _treatingCrewTargets = new HashSet<string>();      // crewId being treated
    private readonly HashSet<string> _extinguishSectionTargets = new HashSet<string>(); // sectionId being extinguished
    private readonly HashSet<string> _repairSectionTargets = new HashSet<string>();     // sectionId being repaired

    [Header("Action Timing & Performance")]
    [Tooltip("Base duration for Move action (seconds).")]
    public float moveActionDuration = 3f;
    
    [Tooltip("Base duration for Repair action (seconds).")]
    public float repairActionDuration = 10f;
    
    [Tooltip("Base duration for Medical treatment action (seconds).")]
    public float medicalActionDuration = 10f;
    
    [Tooltip("Base duration for ExtinguishFire action (seconds).")]
    public float extinguishFireActionDuration = 8f;
    
    [Tooltip("Base duration for OccupyStation action (seconds).")]
    public float occupyStationActionDuration = 2f;
    
    [Header("Injury Performance Penalties")]
    [Tooltip("Movement speed multiplier for Light injured crew (1.0 = normal, 0.75 = 25% slower).")]
    [Range(0.1f, 1f)]
    public float lightInjuryMovementPenalty = 0.75f;
    
    [Tooltip("Action performance multiplier for Light injured crew (1.0 = normal, 0.75 = 25% slower).")]
    [Range(0.1f, 1f)]
    public float lightInjuryActionPenalty = 0.75f;

    // Toggle for deep diagnostic logging
    [Header("Debug")]
    public bool verboseLogging = false;
    [Tooltip("When enabled, prints detailed trace logs for a specific crew.")]
    public bool diagnosticMode = true;
    [Tooltip("Crew Id to trace (leave empty to trace all crew).")]
    public string diagnosticCrewId = "TailGunner";
    [Tooltip("If true, section targets are re-resolved from CrewPositionRegistry every tick while moving.")]
    public bool resolveSectionTargetLive = false;

    // Initialization gate to avoid ticking before positions are resolved
    private bool _positionsInitialized = false;
    public bool PositionsInitialized => _positionsInitialized;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    private void Start()
    {
        // Delay one frame so any UI layout adjustments finalize before we read anchoredPositions
        StartCoroutine(DelayedInitPositions());
    }

    private System.Collections.IEnumerator DelayedInitPositions()
    {
        yield return null; // wait one frame
        InitializeCrewPositions();
        ResetAllCrewState();
        _positionsInitialized = true;
        if (verboseLogging) Debug.Log("[CrewManager] Positions initialized.");
        
        // Hook up StationManager events
        if (StationManager.Instance != null)
        {
            StationManager.Instance.AssignDefaultStations();
            OnCrewDied += StationManager.Instance.OnCrewDied;
            OnCrewInjuryStageChanged += StationManager.Instance.OnCrewIncapacitated;
        }

        // Hook up PlaneManager fire events for safety rules
        if (PlaneManager.Instance != null)
        {
            PlaneManager.Instance.OnFireStarted += HandleFireStarted;
        }
    }
    
    /// <summary>
    /// Initialize crew home positions from CrewPositionRegistry
    /// </summary>
    private void InitializeCrewPositions()
    {
        if (CrewPositionRegistry.Instance == null)
        {
            Debug.LogError("[CrewManager] CrewPositionRegistry not found! Crew will not have valid positions.");
            return;
        }
        List<string> missingStations = new List<string>();
        foreach (var crew in AllCrew)
        {
            string stationId = !string.IsNullOrEmpty(crew.CurrentStationId) ? crew.CurrentStationId : crew.Id;
            var rect = CrewPositionRegistry.Instance.GetStationRect(stationId);
            // Always resolve via registry to ensure referenceParent-space conversion
            Vector2 baseHomePos = CrewPositionRegistry.Instance.GetStationPosition(stationId);
            Vector2 homePos = baseHomePos + crew.HomeOffset;

            crew.HomePosition = homePos;
            crew.CurrentPosition = homePos;

            if (homePos == Vector2.zero)
            {
                missingStations.Add(stationId);
            }

            if (ShouldTrace(crew))
            {
                string rectInfo = rect == null ? "<null>" : $"{BuildPath(rect)} rectAnchored={rect.anchoredPosition}";
                Debug.Log($"[Trace] StartPosition crew={crew.Id} stationId={stationId} rect={rectInfo} baseHome(refParent)={baseHomePos} offset={crew.HomeOffset} finalHome={homePos}");
            }
        }
        if (missingStations.Count > 0)
        {
            Debug.LogWarning($"[CrewManager] Missing station position entries for: {string.Join(", ", missingStations)}. Add them to CrewPositionRegistry.stationPositions.");
        }
    }

    private void ResetAllCrewState()
    {
        foreach (var crew in AllCrew)
        {
            // Clear any lingering actions (including serialized Idle placeholders)
            crew.CurrentAction = null;
            crew.VisualState = CrewVisualState.IdleAtStation;
            crew.CurrentPosition = crew.HomePosition;
            // Suppress reset logs to reduce noise
        }
        // No aggregate log; keep console clean
    }

    /// <summary>
    /// Returns a random healthy crew member or null if none.
    /// </summary>
    public CrewMember GetRandomHealthyCrew()
    {
        var healthy = new List<CrewMember>();
        foreach (var c in AllCrew)
        {
            if (c != null && c.Status == CrewStatus.Healthy)
            {
                healthy.Add(c);
            }
        }
        if (healthy.Count == 0) return null;
        int idx = UnityEngine.Random.Range(0, healthy.Count);
        return healthy[idx];
    }

    /// <summary>
    /// Tick should be called once per simulation update
    /// (GameStateManager controls when time flows).
    /// </summary>
    public void Tick(float deltaTime)
    {
        if (!_positionsInitialized) return; // Do not tick movement/actions until positions ready
        foreach (var crew in AllCrew)
        {
            TickInjuries(crew, deltaTime);

            if (crew.CurrentAction != null)
            {
                TickAction(crew, deltaTime);
            }
        }
    }

    private void TickInjuries(CrewMember crew, float deltaTime)
    {
        // Dead crew do not progress
        if (crew.Status == CrewStatus.Dead) return;

        // Only Serious and Critical tick down; Light injuries are stable
        if (crew.Status == CrewStatus.Serious ||
            crew.Status == CrewStatus.Critical)
        {
            crew.InjuryTimer -= deltaTime;

            if (crew.InjuryTimer <= 0f)
            {
                ProgressInjury(crew);
            }
        }
    }

    private void ProgressInjury(CrewMember crew)
    {
        // Move to next stage (Light no longer progresses)
        switch (crew.Status)
        {
            case CrewStatus.Serious:
                crew.Status = CrewStatus.Critical;
                crew.InjuryTimer = 45f;
                break;

            case CrewStatus.Critical:
                crew.Status = CrewStatus.Dead;
                OnCrewDied?.Invoke(crew);
                return;
        }

        OnCrewInjuryStageChanged?.Invoke(crew);
    }

    private void TickAction(CrewMember crew, float deltaTime)
    {
        // Cancel if crew becomes seriously injured while performing an action (Light injuries OK)
        if (crew.Status != CrewStatus.Healthy && crew.Status != CrewStatus.Light)
        {
            CancelCurrentAction(crew, "Crew became seriously injured or incapacitated");
            return;
        }
        var action = crew.CurrentAction;

        // Treat Idle as no action: immediately clear and stay at station
        if (action.Type == ActionType.Idle)
        {
            crew.CurrentAction = null;
            crew.VisualState = CrewVisualState.IdleAtStation;
            return;
        }
        
        // Handle multi-phase actions (move → perform → return)
        switch (action.Phase)
        {
            case ActionPhase.MoveToTarget:
                // Optional: re-resolve section target live to reflect moved transforms (always in referenceParent space)
                if (resolveSectionTargetLive && (action.Type == ActionType.Repair || action.Type == ActionType.ExtinguishFire))
                {
                    string sectionId = action.TargetId;
                    if (PlaneManager.Instance != null)
                    {
                        var sys = PlaneManager.Instance.GetSystem(action.TargetId);
                        if (sys != null) sectionId = sys.SectionId;
                    }
                    if (CrewPositionRegistry.Instance != null)
                    {
                        action.TargetPosition = CrewPositionRegistry.Instance.GetSectionPosition(sectionId);
                    }
                }
                // Move through waypoints if present
                if (action.Waypoints != null && action.Waypoints.Count > 0 && action.CurrentWaypointIndex < action.Waypoints.Count)
                {
                    Vector2 wp = action.Waypoints[action.CurrentWaypointIndex];
                    TickMovement(crew, wp, deltaTime);
                    if (Vector2.Distance(crew.CurrentPosition, wp) < 1f)
                    {
                        action.CurrentWaypointIndex++;
                        if (action.CurrentWaypointIndex >= action.Waypoints.Count)
                        {
                            // Final hop to exact target
                            TickMovement(crew, action.TargetPosition, deltaTime);
                        }
                    }
                }
                else
                {
                    TickMovement(crew, action.TargetPosition, deltaTime);
                }
                
                if (Vector2.Distance(crew.CurrentPosition, action.TargetPosition) < 1f)
                {
                    // Move action: complete immediately on arrival, no "Performing" phase
                    if (action.Type == ActionType.Move)
                    {
                        ExecuteActionEffect(crew);
                        crew.HomePosition = action.TargetPosition;
                        crew.CurrentPosition = action.TargetPosition;
                        CompleteAction(crew);
                        if (verboseLogging && ShouldTrace(crew))
                            Debug.Log($"[Trace] Move complete on arrival: crew={crew.Id} newHome={crew.HomePosition}");
                        return;
                    }
                    
                    action.Phase = ActionPhase.Performing;
                    action.Elapsed = 0f;
                    crew.VisualState = CrewVisualState.Working;
                    if (verboseLogging && ShouldTrace(crew))
                        Debug.Log($"[Trace] Phase->Performing crew={crew.Id} action={action.Type} at={crew.CurrentPosition}");
                }
                break;
                
            case ActionPhase.Performing:
                // Advance action timer with injury penalty
                float performanceRate = 1f;
                
                // Light injury: action performance penalty (default 25% slower)
                if (crew.Status == CrewStatus.Light)
                {
                    performanceRate = lightInjuryActionPenalty;
                }
                
                action.Elapsed += deltaTime * performanceRate;
                crew.VisualState = CrewVisualState.Working;
                
                if (action.IsComplete)
                {
                    ExecuteActionEffect(crew);
                    
                    // OccupyStation action: complete immediately, no return phase
                    if (action.Type == ActionType.OccupyStation)
                    {
                        // Update HomePosition to new station position permanently
                        crew.HomePosition = action.TargetPosition;
                        crew.CurrentPosition = action.TargetPosition;
                        CompleteAction(crew);
                        if (verboseLogging && ShouldTrace(crew))
                            Debug.Log($"[Trace] OccupyStation complete: crew={crew.Id} newStation={crew.CurrentStation} newHome={crew.HomePosition}");
                        return;
                    }
                    
                    action.Phase = ActionPhase.Returning;
                    action.Elapsed = 0f;
                    crew.VisualState = CrewVisualState.Moving;

                    // Build return path along ordered sections for section-based actions
                    if ((action.Type == ActionType.Repair || action.Type == ActionType.ExtinguishFire || action.Type == ActionType.TreatInjury) && CrewPositionRegistry.Instance != null)
                    {
                        var reg = CrewPositionRegistry.Instance;
                        string fromSection = !string.IsNullOrEmpty(action.TargetSectionId)
                            ? action.TargetSectionId
                            : reg.GetNearestSectionIdByPosition(action.TargetPosition);
                        string toSection = reg.GetNearestSectionIdByPosition(crew.HomePosition);
                        var rpath = reg.GetSectionPathPositionsBetween(fromSection, toSection);
                        if (rpath != null && rpath.Count > 0)
                        {
                            if (Vector2.Distance(crew.CurrentPosition, rpath[0]) < 1f && rpath.Count > 1)
                                rpath.RemoveAt(0);
                            action.ReturnWaypoints = rpath;
                            action.ReturnWaypointIndex = 0;
                        }
                    }
                    if (verboseLogging && ShouldTrace(crew))
                        Debug.Log($"[Trace] Phase->Returning crew={crew.Id} action={action.Type} returnPos={action.ReturnPosition}");
                }
                break;
                
            case ActionPhase.Returning:
                // Traverse return waypoints (if any), then final hop to home
                if (action.ReturnWaypoints != null && action.ReturnWaypoints.Count > 0 && action.ReturnWaypointIndex < action.ReturnWaypoints.Count)
                {
                    Vector2 rwp = action.ReturnWaypoints[action.ReturnWaypointIndex];
                    TickMovement(crew, rwp, deltaTime);
                    if (Vector2.Distance(crew.CurrentPosition, rwp) < 1f)
                    {
                        action.ReturnWaypointIndex++;
                        if (action.ReturnWaypointIndex >= action.ReturnWaypoints.Count)
                        {
                            TickMovement(crew, action.ReturnPosition, deltaTime);
                        }
                    }
                }
                else
                {
                    TickMovement(crew, action.ReturnPosition, deltaTime);
                }
                
                if (Vector2.Distance(crew.CurrentPosition, action.ReturnPosition) < 1f)
                {
                    CompleteAction(crew);
                    if (verboseLogging && ShouldTrace(crew))
                        Debug.Log($"[Trace] ActionComplete crew={crew.Id} type={action.Type} finalPos={crew.CurrentPosition}");
                }
                break;
        }
    }
    
    /// <summary>
    /// Tick crew movement toward a target position
    /// </summary>
    private void TickMovement(CrewMember crew, Vector2 targetPosition, float deltaTime)
    {
        crew.VisualState = CrewVisualState.Moving;
        
        Vector2 direction = (targetPosition - crew.CurrentPosition).normalized;
        float distance = Vector2.Distance(crew.CurrentPosition, targetPosition);
        // Apply movement with injury penalty
        float moveSpeed = crew.MoveSpeed;
        
        // Light injury: movement speed penalty (default 25% slower)
        if (crew.Status == CrewStatus.Light)
        {
            moveSpeed *= lightInjuryMovementPenalty;
        }
        
        float moveAmount = moveSpeed * deltaTime;
        
        if (moveAmount >= distance)
        {
            // Arrived
            crew.CurrentPosition = targetPosition;
            // Movement logs suppressed to avoid spam
        }
        else
        {
            // Keep moving
            crew.CurrentPosition += direction * moveAmount;
            // Movement logs suppressed to avoid spam
        }
    }

    private void CompleteAction(CrewMember crew)
    {
        if (crew.CurrentAction == null)
            return;

        var action = crew.CurrentAction;

        // Attempt to restore previous station if crew temporarily vacated it
        if (StationManager.Instance != null && action.PreviousStation != StationType.None)
        {
            // Try to re-occupy the original station
            bool restored = StationManager.Instance.AssignCrewToStation(crew.Id, action.PreviousStation);
            if (restored)
            {
                Debug.Log($"[CrewManager] Crew {crew.Id} restored to station {action.PreviousStation} after completing {action.Type}");
                // Update home position to the station's position
                if (CrewPositionRegistry.Instance != null)
                {
                    var station = StationManager.Instance.GetStation(action.PreviousStation);
                    if (station != null)
                    {
                        crew.HomePosition = CrewPositionRegistry.Instance.GetStationPosition(station.StationId);
                    }
                }
            }
            else
            {
                Debug.LogWarning($"[CrewManager] Could not restore crew {crew.Id} to station {action.PreviousStation} - station occupied or unavailable");
                // Station was taken - crew stays at current position as new home
                crew.HomePosition = crew.CurrentPosition;
            }
        }

        // Fire completion event while action info is still available to listeners
        OnCrewActionCompleted?.Invoke(crew);
        
        // Clear action and release lock
        crew.CurrentAction = null;
        crew.VisualState = CrewVisualState.IdleAtStation;
        crew.ActionStatusText = null; // Clear any error messages
        
        // Only reset position if we have a valid home position
        if (crew.HomePosition != Vector2.zero)
        {
            crew.CurrentPosition = crew.HomePosition;
        }
        
        ReleaseTargetLock(action);
    }
    
    /// <summary>
    /// Execute the actual effect of an action (called during Performing phase)
    /// </summary>
    private void ExecuteActionEffect(CrewMember crew)
    {
        var action = crew.CurrentAction;
        
        // Move and ManStation don't have success/failure - they always succeed
        if (action.Type == ActionType.Move || action.Type == ActionType.ManStation)
        {
            action.RolledSuccess = true;
            ApplyActionEffect(crew, action);
            return;
        }
        
        // Roll for success for other actions
        float roll = UnityEngine.Random.value;
        bool success = roll <= action.SuccessChance;
        action.RolledSuccess = success;
        
        if (!success)
        {
            // Action failed - show feedback in EventLog only (no popup)
            string actionVerb = action.Type switch
            {
                ActionType.ExtinguishFire => "extinguish the fire",
                ActionType.Repair => "complete repairs",
                ActionType.TreatInjury => "treat the injury",
                ActionType.OccupyStation => "occupy the station",
                _ => "complete the action"
            };
            
            EventLogUI.Instance?.Log($"{crew.Name} failed to {actionVerb}.", Color.yellow);
            return; // Don't apply effect
        }
        
        // Action succeeded - consume item if used
        if (action.UsesConsumable && SupplyManager.Instance != null)
        {
            bool consumed = SupplyManager.Instance.Inventory.TryConsume(action.ConsumableType, 1);
            if (!consumed)
            {
                Debug.LogWarning($"[CrewManager] Failed to consume {action.ConsumableType} for {crew.Name}'s action!");
            }
        }
        
        ApplyActionEffect(crew, action);
    }
    
    /// <summary>
    /// Apply the actual effect of a successful action.
    /// </summary>
    private void ApplyActionEffect(CrewMember crew, CrewAction action)
    {
        switch (action.Type)
        {
            case ActionType.Move:
                crew.CurrentStationId = action.TargetId;
                
                // Auto-occupy station if moving to an available station position
                if (StationManager.Instance != null && CrewPositionRegistry.Instance != null)
                {
                    // Check if target position matches any station position
                    var allStations = StationManager.Instance.AllStations;
                    if (allStations != null)
                    {
                        // Find all stations at or near this position
                        var stationsAtPosition = allStations
                            .Where(s => {
                                var stationPos = CrewPositionRegistry.Instance.GetStationPosition(s.StationId);
                                return Vector2.Distance(stationPos, action.TargetPosition) < 5f; // Within 5 pixels
                            })
                            .OrderBy(s => GetStationPriority(s.Type)) // Pilot > CoPilot > others
                            .ToList();
                        
                        // Try to occupy first available station at this position
                        foreach (var station in stationsAtPosition)
                        {
                            if (station.IsAvailable)
                            {
                                bool assigned = StationManager.Instance.AssignCrewToStation(crew.Id, station);
                                if (assigned)
                                {
                                    Debug.Log($"[CrewManager] {crew.Name} auto-occupied {station.Type} when moving to position");
                                    break; // Only occupy one station
                                }
                            }
                        }
                    }
                }
                break;

            case ActionType.ExtinguishFire:
                if (PlaneManager.Instance != null)
                {
                    bool extinguished = PlaneManager.Instance.TryExtinguishFire(action.TargetId);
                    if (extinguished)
                    {
                        EventLogUI.Instance?.Log($"{crew.Name} successfully extinguished the fire in {action.TargetId}.", Color.green);
                    }
                }
                break;

            case ActionType.Repair:
                if (PlaneManager.Instance != null)
                {
                    // Pass repair amount from action
                    bool repaired = PlaneManager.Instance.TryRepairSystem(action.TargetId, action.RepairAmount);
                    if (repaired)
                    {
                        EventLogUI.Instance?.Log($"{crew.Name} successfully repaired {action.TargetId}.", Color.green);
                    }
                    else
                    {
                        bool sectionRepaired = PlaneManager.Instance.TryRepairSection(action.TargetId, action.RepairAmount);
                        if (sectionRepaired)
                        {
                            EventLogUI.Instance?.Log($"{crew.Name} successfully repaired {action.TargetId}.", Color.green);
                        }
                    }
                }
                break;

            case ActionType.TreatInjury:
            {
                var targetCrew = GetCrewById(action.TargetId);
                string targetName = targetCrew != null ? targetCrew.Name : action.TargetId;
                TryHealCrew(action.TargetId);
                EventLogUI.Instance?.Log($"{crew.Name} successfully treated {targetName}.", Color.green);
                break;
            }

            case ActionType.ManStation:
                crew.CurrentStationId = action.TargetId;
                break;

            case ActionType.OccupyStation:
                // Assign crew to the station via StationManager
                if (StationManager.Instance != null)
                {
                    var station = StationManager.Instance.GetStationById(action.TargetId);
                    if (station != null)
                    {
                        StationManager.Instance.AssignCrewToStation(crew.Id, station);
                        crew.CurrentStationId = action.TargetId;
                    }
                }
                break;

            case ActionType.Idle:
            default:
                break;
        }
    }

    private void CancelCurrentAction(CrewMember crew, string reason)
    {
        if (crew.CurrentAction == null) return;
        
        var action = crew.CurrentAction;
        
        // Return crew to station if they were moving/working
        crew.CurrentPosition = crew.HomePosition;
        crew.VisualState = CrewVisualState.IdleAtStation;
        
        crew.CurrentAction = null;
        OnCrewActionCancelled?.Invoke(crew);
        ReleaseTargetLock(action);
        
        Debug.Log($"[CrewManager] {crew.Name}'s action cancelled: {reason}");
    }

    private void ReleaseTargetLock(CrewAction action)
    {
        if (action == null) return;
        switch (action.Type)
        {
            case ActionType.TreatInjury:
                _treatingCrewTargets.Remove(action.TargetId);
                break;
            case ActionType.ExtinguishFire:
                _extinguishSectionTargets.Remove(action.TargetId);
                break;
            case ActionType.Repair:
                if (PlaneManager.Instance != null)
                {
                    var sys = PlaneManager.Instance.GetSystem(action.TargetId);
                    if (sys != null)
                    {
                        _repairSectionTargets.Remove(sys.SectionId);
                    }
                    else
                    {
                        _repairSectionTargets.Remove(action.TargetId);
                    }
                }
                break;
            case ActionType.ManStation:
            case ActionType.OccupyStation:
                // Stations don't have exclusive locks
                break;
        }
    }


    public CrewMember GetCrewById(string crewId)
    {
        return AllCrew.Find(c => c.Id == crewId);
    }

    // ------------------------------------------------------
    // ACTION ASSIGNMENT
    // ------------------------------------------------------
    
    /// <summary>
    /// Initialize movement phases and target positions for an action.
    /// Sets up: MoveToTarget → Perform → Return flow
    /// </summary>
    private void InitializeActionMovement(CrewMember crew, CrewAction action)
    {
        action.Phase = ActionPhase.MoveToTarget;
        action.ReturnPosition = crew.HomePosition;
        action.TargetPosition = GetTargetPositionForAction(action);
        action.Waypoints = null;
        action.CurrentWaypointIndex = 0;
        
        // Handle station vacation logic based on action type
        if (StationManager.Instance != null && crew.CurrentStation != StationType.None)
        {
            if (action.Type == ActionType.Move)
            {
                // Move action: permanently vacate station
                StationManager.Instance.VacateStation(crew.CurrentStation, crew.Id);
                action.PreviousStation = StationType.None; // Don't restore station on return
                Debug.Log($"[CrewManager] Crew {crew.Id} permanently vacating station {crew.CurrentStation} for Move action");
            }
            else if (action.Type != ActionType.OccupyStation && action.Type != ActionType.ManStation && action.Type != ActionType.Idle)
            {
                // Other actions (Repair, Medical, Fire): temporarily vacate station
                action.PreviousStation = crew.CurrentStation;
                StationManager.Instance.VacateStation(crew.CurrentStation, crew.Id);
                Debug.Log($"[CrewManager] Crew {crew.Id} temporarily vacating station {crew.CurrentStation} for {action.Type} action");
            }
        }
        
        // For section-based actions, build waypoint path following ordered sections
        if (action.Type == ActionType.Move || action.Type == ActionType.Repair || action.Type == ActionType.ExtinguishFire)
        {
            string sectionId = action.TargetId;
            if (PlaneManager.Instance != null && action.Type == ActionType.Repair)
            {
                var sys = PlaneManager.Instance.GetSystem(action.TargetId);
                if (sys != null) sectionId = sys.SectionId;
            }
            var reg = CrewPositionRegistry.Instance;
            if (reg != null)
            {
                action.TargetSectionId = sectionId;
                string startSection = reg.GetNearestSectionIdByPosition(crew.CurrentPosition);
                var path = reg.GetSectionPathPositionsBetween(startSection, sectionId);
                if (path != null && path.Count > 0)
                {
                    if (Vector2.Distance(crew.CurrentPosition, path[0]) < 1f && path.Count > 1)
                        path.RemoveAt(0);
                    action.Waypoints = path;
                    action.CurrentWaypointIndex = 0;
                }
            }
        }
        // TreatInjury also uses section pathfinding to reach target crew's section
        else if (action.Type == ActionType.TreatInjury)
        {
            var targetCrew = GetCrewById(action.TargetId);
            if (targetCrew != null)
            {
                var reg = CrewPositionRegistry.Instance;
                if (reg != null)
                {
                    string startSection = reg.GetNearestSectionIdByPosition(crew.CurrentPosition);
                    string targetSection = reg.GetNearestSectionIdByPosition(targetCrew.CurrentPosition);
                    var path = reg.GetSectionPathPositionsBetween(startSection, targetSection);
                    if (path != null && path.Count > 0)
                    {
                        if (Vector2.Distance(crew.CurrentPosition, path[0]) < 1f && path.Count > 1)
                            path.RemoveAt(0);
                        action.Waypoints = path;
                        action.CurrentWaypointIndex = 0;
                    }
                }
            }
        }
        // OccupyStation uses section pathfinding to reach target station's section
        else if (action.Type == ActionType.OccupyStation)
        {
            if (StationManager.Instance != null)
            {
                var station = StationManager.Instance.GetStationById(action.TargetId);
                if (station != null)
                {
                    var reg = CrewPositionRegistry.Instance;
                    if (reg != null)
                    {
                        string startSection = reg.GetNearestSectionIdByPosition(crew.CurrentPosition);
                        string targetSection = station.SectionId;
                        var path = reg.GetSectionPathPositionsBetween(startSection, targetSection);
                        if (path != null && path.Count > 0)
                        {
                            if (Vector2.Distance(crew.CurrentPosition, path[0]) < 1f && path.Count > 1)
                                path.RemoveAt(0);
                            action.Waypoints = path;
                            action.CurrentWaypointIndex = 0;
                        }
                        action.TargetSectionId = targetSection;
                    }
                }
            }
        }

        // If we have waypoints, skip any that we're already effectively on top of
        if (action.Waypoints != null && action.Waypoints.Count > 0)
        {
            while (action.CurrentWaypointIndex < action.Waypoints.Count &&
                   Vector2.Distance(crew.CurrentPosition, action.Waypoints[action.CurrentWaypointIndex]) < 1f)
            {
                action.CurrentWaypointIndex++;
            }
        }

        // Decide whether we are already at the final target
        bool atFinal = Vector2.Distance(crew.CurrentPosition, action.TargetPosition) < 1f;
        if (atFinal)
        {
            action.Phase = ActionPhase.Performing;
            crew.VisualState = CrewVisualState.Working;
        }
        else
        {
            crew.VisualState = CrewVisualState.Moving;
        }
        if (verboseLogging && ShouldTrace(crew))
        {
            // Provide exact resolution chain and transform used
            switch (action.Type)
            {
                case ActionType.Repair:
                case ActionType.ExtinguishFire:
                {
                    string sectionId = action.TargetId;
                    string viaSystem = "none";
                    if (PlaneManager.Instance != null)
                    {
                        var sys = PlaneManager.Instance.GetSystem(action.TargetId);
                        if (sys != null)
                        {
                            sectionId = sys.SectionId;
                            viaSystem = sys.Id;
                        }
                    }
                    var rect = CrewPositionRegistry.Instance?.GetSectionRect(sectionId);
                    string rectInfo = rect == null ? "<null>" : $"{BuildPath(rect)} pos={rect.anchoredPosition}";
                    Debug.Log($"[Trace] ActionInit crew={crew.Id} type={action.Type} inputTargetId={action.TargetId} viaSystem={viaSystem} sectionId={sectionId} sectionRect={rectInfo} targetPos={action.TargetPosition} returnPos={action.ReturnPosition} phase={action.Phase}");
                    break;
                }
                case ActionType.Move:
                case ActionType.ManStation:
                case ActionType.OccupyStation:
                {
                    var rect = CrewPositionRegistry.Instance?.GetStationRect(action.TargetId);
                    string rectInfo = rect == null ? "<null>" : $"{BuildPath(rect)} pos={rect.anchoredPosition}";
                    Debug.Log($"[Trace] ActionInit crew={crew.Id} type={action.Type} stationId={action.TargetId} stationRect={rectInfo} targetPos={action.TargetPosition} returnPos={action.ReturnPosition} phase={action.Phase}");
                    break;
                }
                case ActionType.TreatInjury:
                {
                    var targetCrew = GetCrewById(action.TargetId);
                    var tpos = targetCrew != null ? targetCrew.CurrentPosition : Vector2.zero;
                    Debug.Log($"[Trace] ActionInit crew={crew.Id} type=TreatInjury targetCrewId={action.TargetId} targetCrewPos={tpos} returnPos={action.ReturnPosition} phase={action.Phase}");
                    break;
                }
                default:
                    Debug.Log($"[Trace] ActionInit crew={crew.Id} type={action.Type} targetId={action.TargetId} targetPos={action.TargetPosition} returnPos={action.ReturnPosition} phase={action.Phase}");
                    break;
            }
        }
    }
    
    /// <summary>
    /// Get the screen position where crew should go to perform this action
    /// </summary>
    private Vector2 GetTargetPositionForAction(CrewAction action)
    {
        if (CrewPositionRegistry.Instance == null)
        {
            Debug.LogError("[CrewManager] CrewPositionRegistry.Instance is null!");
            return Vector2.zero;
        }
        
        switch (action.Type)
        {
            case ActionType.Repair:
            case ActionType.ExtinguishFire:
                // Get section position
                if (PlaneManager.Instance != null)
                {
                    var system = PlaneManager.Instance.GetSystem(action.TargetId);
                    string sectionId = system != null ? system.SectionId : action.TargetId;
                    var pos = CrewPositionRegistry.Instance.GetSectionPosition(sectionId);
                    if (pos == Vector2.zero)
                    {
                        Debug.LogWarning($"[CrewManager] Target section '{sectionId}' position unresolved (0,0). Check CrewPositionRegistry sectionPositions list.");
                    }
                    return pos;
                }
                return Vector2.zero;
                
            case ActionType.TreatInjury:
                // Move to target crew's current position
                var targetCrew = GetCrewById(action.TargetId);
                if (targetCrew == null)
                {
                    Debug.LogWarning($"[CrewManager] TreatInjury target crew '{action.TargetId}' not found.");
                    return Vector2.zero;
                }
                return targetCrew.CurrentPosition;
                
            case ActionType.Move:
            case ActionType.ManStation:
            case ActionType.OccupyStation:
                // Move to station position
                var sPos = CrewPositionRegistry.Instance.GetStationPosition(action.TargetId);
                if (sPos == Vector2.zero)
                {
                    Debug.LogWarning($"[CrewManager] Station target '{action.TargetId}' position unresolved (0,0). Check CrewPositionRegistry stationPositions list.");
                }
                return sPos;
                
            default:
                return Vector2.zero;
        }
    }

    public bool TryAssignAction(string crewId, CrewAction action)
    {
        var crew = AllCrew.Find(c => c.Id == crewId);
        if (crew == null) return false;
        
        // Basic validation examples:
        if (crew.Status == CrewStatus.Dead) return false;
        // Treat explicit Idle assignment as a request to clear any action
        if (action != null && action.Type == ActionType.Idle)
        {
            // If there was an action, cancel back to home
            if (crew.CurrentAction != null)
            {
                CancelCurrentAction(crew, "Idle assigned");
            }
            else
            {
                crew.CurrentAction = null;
                crew.VisualState = CrewVisualState.IdleAtStation;
                crew.CurrentPosition = crew.HomePosition;
            }
            return true;
        }
        // Healthy and Light injured crew can perform actions
        if (crew.Status != CrewStatus.Healthy && crew.Status != CrewStatus.Light)
        {
            if (verboseLogging && ShouldTrace(crew)) Debug.Log($"[Trace] Reject assign (not healthy/light) crew={crew.Id} status={crew.Status}");
            return false;
        }
        if (crew.CurrentAction != null)
        {
            if (verboseLogging && ShouldTrace(crew)) Debug.Log($"[Trace] Reject assign (already has action) crew={crew.Id} currentType={crew.CurrentAction.Type} phase={crew.CurrentAction.Phase}");
            return false;
        }

        // Enforce exclusive target locks
        if (action != null)
        {
            switch (action.Type)
            {
                case ActionType.TreatInjury:
                {
                    var target = GetCrewById(action.TargetId);
                    if (target == null || target.Status == CrewStatus.Healthy)
                    {
                        if (verboseLogging && ShouldTrace(crew)) Debug.Log($"[Trace] Reject assign TreatInjury: target invalid or healthy targetId={action.TargetId}");
                        return false;
                    }
                    if (_treatingCrewTargets.Contains(action.TargetId)) return false;
                    break;
                }
                case ActionType.ExtinguishFire:
                    if (_extinguishSectionTargets.Contains(action.TargetId)) return false;
                    break;
                case ActionType.Repair:
                    if (PlaneManager.Instance != null)
                    {
                        var sys = PlaneManager.Instance.GetSystem(action.TargetId);
                        if (sys != null)
                        {
                            if (_repairSectionTargets.Contains(sys.SectionId)) return false;
                        }
                        else
                        {
                            // Target may be a section id directly
                            if (_repairSectionTargets.Contains(action.TargetId)) return false;
                        }

                        // Fire precedence and nothing-to-repair checks
                        string sectionId = sys != null ? sys.SectionId : action.TargetId;
                        var section = PlaneManager.Instance.GetSection(sectionId);
                        if (section != null)
                        {
                            if (section.OnFire)
                            {
                                return false;
                            }
                            if (section.Integrity >= 100)
                            {
                                bool systemNeedsRepair = sys != null && sys.Status != SystemStatus.Operational;
                                if (!systemNeedsRepair)
                                {
                                    return false;
                                }
                            }
                        }
                    }
                    break;
            }
        }

        // Fire safety validation - check if path is blocked by fire
        if (action != null && PlaneManager.Instance != null && CrewPositionRegistry.Instance != null)
        {
            string currentSectionId = GetCrewCurrentSectionId(crew);
            string targetSectionId = null;

            // Determine target section based on action type
            switch (action.Type)
            {
                case ActionType.Move:
                    targetSectionId = action.TargetId; // TargetId for Move is sectionId
                    break;
                case ActionType.Repair:
                    var sys = PlaneManager.Instance.GetSystem(action.TargetId);
                    targetSectionId = sys != null ? sys.SectionId : action.TargetId;
                    break;
                case ActionType.ExtinguishFire:
                    targetSectionId = action.TargetId; // TargetId is sectionId
                    break;
                case ActionType.TreatInjury:
                    var targetCrew = GetCrewById(action.TargetId);
                    if (targetCrew != null)
                    {
                        targetSectionId = GetCrewCurrentSectionId(targetCrew);
                    }
                    break;
            }

            // Check if path is blocked by fire
            if (!string.IsNullOrEmpty(currentSectionId) && !string.IsNullOrEmpty(targetSectionId))
            {
                bool isExtinguish = action.Type == ActionType.ExtinguishFire;
                if (PlaneManager.Instance.IsPathBlockedByFire(currentSectionId, targetSectionId, isExtinguish))
                {
                    // Path blocked - cancel action and show error
                    crew.ActionStatusText = "Cannot pass through fire!";
                    Debug.LogWarning($"[CrewManager] {crew.Name} cannot reach {targetSectionId} - path blocked by fire!");
                    return false;
                }
            }
        }

        crew.CurrentAction = action;
        crew.ActionStatusText = null; // Clear any previous error messages
        
        // Initialize movement phases and positions
        InitializeActionMovement(crew, action);

        // Acquire locks
        if (action != null)
        {
            switch (action.Type)
            {
                case ActionType.TreatInjury:
                    _treatingCrewTargets.Add(action.TargetId);
                    break;
                case ActionType.ExtinguishFire:
                    _extinguishSectionTargets.Add(action.TargetId);
                    break;
                case ActionType.Repair:
                    if (PlaneManager.Instance != null)
                    {
                        var sys = PlaneManager.Instance.GetSystem(action.TargetId);
                        if (sys != null)
                        {
                            _repairSectionTargets.Add(sys.SectionId);
                        }
                        else
                        {
                            _repairSectionTargets.Add(action.TargetId);
                        }
                    }
                    break;
            }
        }
        OnCrewActionAssigned?.Invoke(crew);
        if (verboseLogging && ShouldTrace(crew))
            Debug.Log($"[Trace] Assigned action crew={crew.Id} type={action.Type} targetId={action.TargetId}");
        return true;
    }

    // ------------------------
    // Diagnostics helpers
    // ------------------------
    public bool ShouldTrace(string crewId)
    {
        if (!diagnosticMode) return false;
        if (string.IsNullOrEmpty(diagnosticCrewId)) return true;
        return string.Equals(diagnosticCrewId, crewId, StringComparison.OrdinalIgnoreCase);
    }

    public bool ShouldTrace(CrewMember crew)
    {
        return crew != null && ShouldTrace(crew.Id);
    }

    private static string BuildPath(RectTransform rect)
    {
        if (rect == null) return "<null>";
        var t = (Transform)rect;
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        while (t != null)
        {
            if (sb.Length == 0) sb.Insert(0, t.name);
            else sb.Insert(0, $"/{t.name}");
            t = t.parent;
        }
        return sb.ToString();
    }

    public bool TryMoveCrew(string crewId, string stationId)
    {
        var crew = AllCrew.Find(c => c.Id == crewId);
        if (crew == null) return false;
        if (crew.Status == CrewStatus.Dead) return false;

        // Movement = a simple action with duration
        CrewAction move = new CrewAction
        {
            Type = ActionType.Move,
            TargetId = stationId,
            Duration = moveActionDuration,
            Elapsed = 0f
        };

        return TryAssignAction(crewId, move);
    }

    /// <summary>
    /// Simple healing logic: move target toward healthier status and reset injury timer.
    /// You can make this more nuanced later.
    /// </summary>
    public bool TryHealCrew(string targetCrewId)
    {
        var target = GetCrewById(targetCrewId);
        if (target == null) return false;
        if (target.Status == CrewStatus.Dead) return false;

        switch (target.Status)
        {
            case CrewStatus.Critical:
                target.Status = CrewStatus.Serious;
                target.InjuryTimer = 60f;
                break;
            case CrewStatus.Serious:
                target.Status = CrewStatus.Light;
                target.InjuryTimer = 90f;
                // Light injury no longer incapacitates - if they have an action they can resume
                if (target.CurrentAction != null && target.CurrentAction.Phase == ActionPhase.Performing)
                {
                    target.VisualState = CrewVisualState.Working;
                }
                break;
            case CrewStatus.Light:
                target.Status = CrewStatus.Healthy;
                target.InjuryTimer = 0f;
                break;
            default:
                // Already Healthy or Unconscious/Dead, no effect for now
                return false;
        }

        OnCrewInjuryStageChanged?.Invoke(target);
        return true;
    }
    
    /// <summary>
    /// Apply an injury to a crew member (used by chaos simulator and combat).
    /// </summary>
    public bool ApplyInjury(string crewId, CrewStatus injuryLevel)
    {
        var crew = GetCrewById(crewId);
        if (crew == null) return false;
        if (crew.Status == CrewStatus.Dead) return false;
        
        // Set injury status and timer
        crew.Status = injuryLevel;
        crew.InjuryTimer = injuryLevel switch
        {
            CrewStatus.Light => 90f,
            CrewStatus.Serious => 60f,
            CrewStatus.Critical => 45f,
            _ => 0f
        };
        
        OnCrewInjuryStageChanged?.Invoke(crew);
        return true;
    }

    /// <summary>
    /// Get station priority for auto-occupation (lower = higher priority).
    /// Pilot > CoPilot > all other stations.
    /// </summary>
    private int GetStationPriority(StationType type)
    {
        return type switch
        {
            StationType.Pilot => 1,
            StationType.CoPilot => 2,
            _ => 3
        };
    }

    // ------------------------------------------------------------------
    // FIRE SAFETY RULES
    // ------------------------------------------------------------------

    /// <summary>
    /// Handle fire starting in a section - auto-evacuate crew and check for traps.
    /// </summary>
    private void HandleFireStarted(PlaneSectionState section)
    {
        if (PlaneManager.Instance == null || CrewPositionRegistry.Instance == null) return;

        Debug.Log($"[CrewManager] Fire started in {section.Id} - checking crew safety...");

        // Find all crew in this section
        var orderedSections = CrewPositionRegistry.Instance.GetOrderedSectionIds();
        int sectionIdx = orderedSections.IndexOf(section.Id);
        if (sectionIdx == -1) return;

        foreach (var crew in AllCrew)
        {
            if (crew.Status == CrewStatus.Dead) continue;

            // Check if crew is in the burning section (or moving through it)
            string crewSectionId = GetCrewCurrentSectionId(crew);
            
            if (crewSectionId == section.Id)
            {
                // Check if crew can move (incapacitated crew die immediately)
                bool isIncapacitated = crew.Status == CrewStatus.Serious || 
                                      crew.Status == CrewStatus.Critical || 
                                      crew.Status == CrewStatus.Unconscious;
                
                if (isIncapacitated)
                {
                    // INCAPACITATED CREW CANNOT EVACUATE - DIES IN FIRE
                    Debug.LogWarning($"[CrewManager] {crew.Name} is incapacitated in {section.Id} - cannot evacuate, KILLED by fire!");
                    crew.Status = CrewStatus.Dead;
                    crew.InjuryTimer = 0f;
                    
                    // Cancel current action
                    if (crew.CurrentAction != null)
                    {
                        CancelCurrentAction(crew, "Died in fire (incapacitated)");
                    }
                    
                    OnCrewDied?.Invoke(crew);
                    continue;
                }
                
                // Healthy/Light injured crew can evacuate - find escape route
                string safeSection = PlaneManager.Instance.GetNearestSafeAdjacentSection(section.Id);
                
                if (safeSection == null)
                {
                    // TRAPPED BY FIRE - NO ESCAPE ROUTE
                    Debug.LogWarning($"[CrewManager] {crew.Name} trapped by fire in {section.Id} with no escape - KILLED!");
                    crew.Status = CrewStatus.Dead;
                    crew.InjuryTimer = 0f;
                    
                    // Cancel current action
                    if (crew.CurrentAction != null)
                    {
                        CancelCurrentAction(crew, "Trapped by fire");
                    }
                    
                    OnCrewDied?.Invoke(crew);
                }
                else
                {
                    // AUTO-EVACUATE to nearest safe adjacent section
                    Debug.Log($"[CrewManager] Auto-evacuating {crew.Name} from {section.Id} to {safeSection}");
                    
                    // Cancel any current action (repair, medical, etc.)
                    if (crew.CurrentAction != null)
                    {
                        CancelCurrentAction(crew, "Emergency evacuation from fire");
                    }
                    
                    // Cancel any pending UI action selection for this crew
                    if (OrdersUIController.Instance != null && 
                        OrdersUIController.Instance.SelectedCrewId == crew.Id)
                    {
                        OrdersUIController.Instance.CancelPendingAction();
                    }
                    
                    // Force emergency move to safe section
                    Vector2 safePos = CrewPositionRegistry.Instance.GetSectionPosition(safeSection);
                    var moveAction = new CrewAction
                    {
                        Type = ActionType.Move,
                        TargetId = safeSection,
                        Duration = moveActionDuration // Use standard move duration for evacuation
                    };
                    TryAssignAction(crew.Id, moveAction);
                }
            }
        }
    }

    /// <summary>
    /// Get the section ID where a crew member is currently located.
    /// </summary>
    private string GetCrewCurrentSectionId(CrewMember crew)
    {
        if (CrewPositionRegistry.Instance == null) return null;

        // Use current position to find nearest section
        return CrewPositionRegistry.Instance.GetNearestSectionIdByPosition(crew.CurrentPosition);
    }

}

