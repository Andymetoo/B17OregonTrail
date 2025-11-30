using System;
using System.Collections.Generic;
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
        InitializeCrewPositions();
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
        
        foreach (var crew in AllCrew)
        {
            string stationId = !string.IsNullOrEmpty(crew.CurrentStationId) ? crew.CurrentStationId : crew.Id;
            Vector2 homePos = CrewPositionRegistry.Instance.GetStationPosition(stationId);
            
            crew.HomePosition = homePos;
            crew.CurrentPosition = homePos;
        }
    }

    /// <summary>
    /// Tick should be called once per simulation update
    /// (GameStateManager controls when time flows).
    /// </summary>
    public void Tick(float deltaTime)
    {
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

        // Only damaged statuses tick down
        if (crew.Status == CrewStatus.Light ||
            crew.Status == CrewStatus.Serious ||
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
        // Move to next stage
        switch (crew.Status)
        {
            case CrewStatus.Light:
                crew.Status = CrewStatus.Serious;
                crew.InjuryTimer = 60f; // example number
                break;

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
        // Cancel if crew becomes injured/unhealthy while performing an action
        if (crew.Status != CrewStatus.Healthy)
        {
            CancelCurrentAction(crew, "Crew became injured or incapacitated");
            return;
        }
        
        var action = crew.CurrentAction;
        
        // Handle multi-phase actions (move → perform → return)
        switch (action.Phase)
        {
            case ActionPhase.MoveToTarget:
                TickMovement(crew, action.TargetPosition, deltaTime);
                
                if (Vector2.Distance(crew.CurrentPosition, action.TargetPosition) < 1f)
                {
                    action.Phase = ActionPhase.Performing;
                    action.Elapsed = 0f;
                    crew.VisualState = CrewVisualState.Working;
                }
                break;
                
            case ActionPhase.Performing:
                action.Elapsed += deltaTime;
                crew.VisualState = CrewVisualState.Working;
                
                if (action.IsComplete)
                {
                    ExecuteActionEffect(crew);
                    action.Phase = ActionPhase.Returning;
                    action.Elapsed = 0f;
                    crew.VisualState = CrewVisualState.Moving;
                }
                break;
                
            case ActionPhase.Returning:
                TickMovement(crew, action.ReturnPosition, deltaTime);
                
                if (Vector2.Distance(crew.CurrentPosition, action.ReturnPosition) < 1f)
                {
                    CompleteAction(crew);
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
        float moveAmount = crew.MoveSpeed * deltaTime;
        
        if (moveAmount >= distance)
        {
            // Arrived
            crew.CurrentPosition = targetPosition;
        }
        else
        {
            // Keep moving
            crew.CurrentPosition += direction * moveAmount;
        }
    }

    private void CompleteAction(CrewMember crew)
    {
        if (crew.CurrentAction == null)
            return;

        var action = crew.CurrentAction;

        // Fire completion event while action info is still available to listeners
        OnCrewActionCompleted?.Invoke(crew);
        
        // Clear action and release lock
        crew.CurrentAction = null;
        crew.VisualState = CrewVisualState.IdleAtStation;
        
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
        
        switch (action.Type)
        {
            case ActionType.Move:
                crew.CurrentStationId = action.TargetId;
                break;

            case ActionType.ExtinguishFire:
                if (PlaneManager.Instance != null)
                {
                    PlaneManager.Instance.TryExtinguishFire(action.TargetId);
                }
                break;

            case ActionType.Repair:
                if (PlaneManager.Instance != null)
                {
                    bool repaired = PlaneManager.Instance.TryRepairSystem(action.TargetId);
                    if (!repaired)
                    {
                        PlaneManager.Instance.TryRepairSection(action.TargetId);
                    }
                }
                break;

            case ActionType.TreatInjury:
                TryHealCrew(action.TargetId);
                break;

            case ActionType.ManStation:
                crew.CurrentStationId = action.TargetId;
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
        
        // Skip movement if target is invalid or too close
        if (action.TargetPosition == Vector2.zero || Vector2.Distance(crew.CurrentPosition, action.TargetPosition) < 5f)
        {
            action.Phase = ActionPhase.Performing;
            crew.VisualState = CrewVisualState.Working;
        }
        else
        {
            crew.VisualState = CrewVisualState.Moving;
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
                    return CrewPositionRegistry.Instance.GetSectionPosition(sectionId);
                }
                return Vector2.zero;
                
            case ActionType.TreatInjury:
                // Move to target crew's current position
                var targetCrew = GetCrewById(action.TargetId);
                return targetCrew != null ? targetCrew.CurrentPosition : Vector2.zero;
                
            case ActionType.Move:
            case ActionType.ManStation:
                // Move to station position
                return CrewPositionRegistry.Instance.GetStationPosition(action.TargetId);
                
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
        // Only healthy crew can be assigned actions
        if (crew.Status != CrewStatus.Healthy) return false;
        if (crew.CurrentAction != null) return false;

        // Enforce exclusive target locks
        if (action != null)
        {
            switch (action.Type)
            {
                case ActionType.TreatInjury:
                    if (_treatingCrewTargets.Contains(action.TargetId)) return false;
                    break;
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

        crew.CurrentAction = action;
        
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
        return true;
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
            Duration = 5f,    // example duration
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

}
