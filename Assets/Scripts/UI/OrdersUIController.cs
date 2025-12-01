using UnityEngine;

public enum PendingOrderType
{
    None,
    Move,
    ExtinguishFire,
    RepairSystem,
    TreatInjury
}

/// <summary>
/// UI-side controller that coordinates:
/// 1) Which crew is selected
/// 2) Which action type is selected
/// 3) Which target is selected
///
/// When all three are known, it creates a CrewCommand and enqueues it.
/// </summary>
public class OrdersUIController : MonoBehaviour
{
    public static OrdersUIController Instance { get; private set; }

    [Header("Debug / Status")]
    [SerializeField] private string selectedCrewId;
    [SerializeField] private PendingOrderType pendingOrder = PendingOrderType.None;
    [SerializeField] private string pendingTargetId;
    [SerializeField] private string lastInspectedSectionId;

    public string SelectedCrewId => selectedCrewId;
    public PendingOrderType PendingOrder => pendingOrder;
    public string LastInspectedSectionId => lastInspectedSectionId;

    // UI message event for pending action updates
    public System.Action<string> OnPendingActionMessage;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    private void Update()
    {
        // Right-click or Escape to cancel pending action
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            CancelPendingAction();
        }
    }

    // -------------------------------
    // Called by UI
    // -------------------------------

    public void OnCrewClicked(string crewId)
    {
        // Special handling: if we are pending a medical action, interpret this click as target selection
        if (pendingOrder == PendingOrderType.TreatInjury && !string.IsNullOrEmpty(selectedCrewId))
        {
            if (crewId == selectedCrewId)
            {
                Debug.Log("[OrdersUI] Can't treat yourself.");
                return;
            }

            var targetCrew = CrewManager.Instance?.GetCrewById(crewId);
            if (targetCrew == null)
            {
                Debug.Log("[OrdersUI] Target crew not found.");
                return;
            }

            if (targetCrew.Status == CrewStatus.Healthy)
            {
                Debug.Log("[OrdersUI] Target crew is not injured.");
                return;
            }

            // This is the medical target
            pendingTargetId = crewId;
            Debug.Log($"[OrdersUI] Medical target selected via OnCrewClicked: {crewId}");
            TryCommitOrder();
            return;
        }

        // Normal selection path
        selectedCrewId = crewId;
        pendingOrder = PendingOrderType.None;
        pendingTargetId = null;

        Debug.Log($"[OrdersUI] Selected crew: {selectedCrewId}");
        // Fire message: show selected crew name
        var crew = CrewManager.Instance?.GetCrewById(selectedCrewId);
        string crewName = crew != null ? crew.Name : selectedCrewId;
        OnPendingActionMessage?.Invoke($"{crewName} selected.");
        EventLogUI.Instance?.Log($"{crewName} selected.", Color.yellow);
    }

    /// <summary>
    /// Unified method for action buttons - can be called directly from UI with enum parameter
    /// </summary>
    public void OnActionClicked(PendingOrderType orderType)
    {
        if (string.IsNullOrEmpty(selectedCrewId))
        {
            Debug.Log($"[OrdersUI] No crew selected for {orderType}.");
            return;
        }

        // Validate selected crew is Healthy before allowing any pending order
        var crew = CrewManager.Instance?.GetCrewById(selectedCrewId);
        if (crew == null)
        {
            Debug.Log("[OrdersUI] Selected crew not found.");
            return;
        }

        if (crew.Status != CrewStatus.Healthy)
        {
            Debug.Log($"[OrdersUI] {crew.Name} is {crew.Status} and cannot initiate actions. Select a healthy crew.");
            // Explicitly clear any pending order request
            pendingOrder = PendingOrderType.None;
            pendingTargetId = null;
            EventLogUI.Instance?.Log($"{crew.Name} is {crew.Status} and cannot initiate actions.", Color.red);
            return;
        }

        pendingOrder = orderType;
        pendingTargetId = null;
        Debug.Log($"[OrdersUI] Pending order: {orderType} for {selectedCrewId}");
        // Fire message: selected crew is performing action
        var crewInfo = CrewManager.Instance?.GetCrewById(selectedCrewId);
        string crewName = crewInfo != null ? crewInfo.Name : selectedCrewId;
        string actionText = orderType switch
        {
            PendingOrderType.Move => "moving to...",
            PendingOrderType.ExtinguishFire => "extinguishing fire in...",
            PendingOrderType.RepairSystem => "repairing...",
            PendingOrderType.TreatInjury => "performing medical...",
            _ => "performing action..."
        };
        OnPendingActionMessage?.Invoke($"{crewName} is {actionText}");
        EventLogUI.Instance?.Log($"{crewName} is {actionText}", Color.yellow);
    }

    // Legacy methods - kept for compatibility if any UI buttons still reference them
    // New UI buttons should use OnActionClicked(PendingOrderType) instead
    public void OnActionClicked_Move() => OnActionClicked(PendingOrderType.Move);
    public void OnActionClicked_Extinguish() => OnActionClicked(PendingOrderType.ExtinguishFire);
    public void OnActionClicked_Repair() => OnActionClicked(PendingOrderType.RepairSystem);
    public void OnActionClicked_Treat() => OnActionClicked(PendingOrderType.TreatInjury);

    /// <summary>
    /// Called when the player clicks a target:
    /// - station (for Move / ManStation)
    /// - section (for ExtinguishFire)
    /// - system (for RepairSystem)
    /// - crew (for TreatInjury)
    /// </summary>
    public void OnTargetClicked(string targetId)
    {
        // If we don't have a pending order, treat this as an inspect
        if (pendingOrder == PendingOrderType.None)
        {
            // Clear any crew selection when inspecting a section to show section stats
            selectedCrewId = null;
            lastInspectedSectionId = targetId;
            Debug.Log($"[OrdersUI] Inspecting section: {targetId}");
            return;
        }

        // Medical must target a crew, not a section/station
        if (pendingOrder == PendingOrderType.TreatInjury)
        {
            OnPendingActionMessage?.Invoke("Select an injured crew member to treat.");
            EventLogUI.Instance?.Log("Medical requires clicking an injured crew, not a section.", Color.red);
            Debug.Log("[OrdersUI] Ignoring non-crew target for medical.");
            return;
        }

        if (string.IsNullOrEmpty(selectedCrewId))
        {
            Debug.Log("[OrdersUI] No crew selected when target clicked.");
            return;
        }

        pendingTargetId = targetId;
        Debug.Log($"[OrdersUI] Target selected: {pendingTargetId} for order {pendingOrder}");

        TryCommitOrder();
    }
    
    /// <summary>
    /// Called when player clicks on a crew member - used for medical target selection.
    /// If pending TreatInjury, this is the target. Otherwise it's a crew selection.
    /// </summary>
    public void OnCrewButtonClicked(string crewId)
    {
        Debug.Log($"[OrdersUI] OnCrewButtonClicked: {crewId}, pendingOrder={pendingOrder}, selectedCrew={selectedCrewId}");
        
        // If we have a pending medical action, treat this as the target
        if (pendingOrder == PendingOrderType.TreatInjury && !string.IsNullOrEmpty(selectedCrewId))
        {
            // Don't allow self-treatment or treating healthy crew
            if (crewId == selectedCrewId)
            {
                Debug.Log("[OrdersUI] Can't treat yourself.");
                return;
            }
            
            var targetCrew = CrewManager.Instance?.GetCrewById(crewId);
            if (targetCrew != null && targetCrew.Status == CrewStatus.Healthy)
            {
                Debug.Log("[OrdersUI] Target crew is not injured.");
                return;
            }
            
            // This is the medical target
            pendingTargetId = crewId;
            Debug.Log($"[OrdersUI] Medical target selected: {crewId}");
            TryCommitOrder();
            return;
        }
        
        // Otherwise, this is a crew selection (not a target)
        // Only allow selecting healthy crew for giving orders
        var crew = CrewManager.Instance?.GetCrewById(crewId);
        if (crew != null && crew.Status != CrewStatus.Healthy)
        {
            Debug.Log($"[OrdersUI] {crew.Name} is injured and cannot be selected to perform actions.");
            return;
        }
        
        OnCrewClicked(crewId);
    }
    
    /// <summary>
    /// Cancel any pending action.
    /// </summary>
    public void CancelPendingAction()
    {
        if (pendingOrder != PendingOrderType.None)
        {
            Debug.Log($"[OrdersUI] Cancelled pending action: {pendingOrder}");
            pendingOrder = PendingOrderType.None;
            pendingTargetId = null;
            EventLogUI.Instance?.Log("Action cancelled.", Color.yellow);
        }
    }

    /// <summary>
    /// Clear any current selection and inspection, and cancel actions.
    /// </summary>
    public void ClearSelection()
    {
        selectedCrewId = null;
        lastInspectedSectionId = null;
        CancelPendingAction();
        OnPendingActionMessage?.Invoke("");
        Debug.Log("[OrdersUI] Cleared selection and inspection.");
        EventLogUI.Instance?.Log("Selection cleared.", Color.yellow);
    }

    /// <summary>
    /// Optional explicit hook for section buttons.
    /// </summary>
    public void OnSectionButtonClicked(string sectionId)
    {
        OnTargetClicked(sectionId);
    }

    // -------------------------------
    // Internal
    // -------------------------------

    private void TryCommitOrder()
    {
        if (CrewCommandProcessor.Instance == null)
        {
            Debug.LogWarning("[OrdersUI] No CrewCommandProcessor available.");
            return;
        }

        if (string.IsNullOrEmpty(selectedCrewId) || string.IsNullOrEmpty(pendingTargetId))
        {
            return;
        }

        CrewCommand cmd = null;

        switch (pendingOrder)
        {
            case PendingOrderType.Move:
                cmd = new MoveCrewCommand(selectedCrewId, pendingTargetId, 4f);
                break;

            case PendingOrderType.ExtinguishFire:
                cmd = new ExtinguishFireCommand(selectedCrewId, pendingTargetId, 8f);
                break;

            case PendingOrderType.RepairSystem:
                cmd = new RepairSystemCommand(selectedCrewId, pendingTargetId, 10f);
                break;

            case PendingOrderType.TreatInjury:
            {
                // Validate target is a crew and is injured
                var target = CrewManager.Instance?.GetCrewById(pendingTargetId);
                if (target == null)
                {
                    EventLogUI.Instance?.Log("Select an injured crew to treat (not a section).", Color.red);
                    OnPendingActionMessage?.Invoke("Select an injured crew member.");
                    return;
                }
                if (target.Status == CrewStatus.Healthy)
                {
                    EventLogUI.Instance?.Log($"{target.Name} is not injured.", Color.red);
                    OnPendingActionMessage?.Invoke($"{target.Name} is not injured.");
                    return;
                }
                cmd = new TreatInjuryCommand(selectedCrewId, pendingTargetId, 10f);
                break;
            }
        }

        if (cmd != null)
        {
            CrewCommandProcessor.Instance.Enqueue(cmd);
            Debug.Log($"[OrdersUI] Enqueued {pendingOrder} from {selectedCrewId} to {pendingTargetId}");
            // Fire message: full action text with target
            var crew = CrewManager.Instance?.GetCrewById(selectedCrewId);
            string crewName = crew != null ? crew.Name : selectedCrewId;
            string actionText = pendingOrder switch
            {
                PendingOrderType.Move => $"moving to {pendingTargetId}",
                PendingOrderType.ExtinguishFire => $"extinguishing fire in {pendingTargetId}",
                PendingOrderType.RepairSystem => $"repairing {pendingTargetId}",
                PendingOrderType.TreatInjury => $"performing medical on {pendingTargetId}",
                _ => $"performing action at {pendingTargetId}"
            };
            OnPendingActionMessage?.Invoke($"{crewName} is {actionText}");
            // Also log to EventLog
            var color = pendingOrder == PendingOrderType.TreatInjury ? Color.green : Color.yellow;
            EventLogUI.Instance?.Log($"{crewName} is {actionText}", color);
        }

        // Reset order state
        pendingOrder = PendingOrderType.None;
        pendingTargetId = null;
    }
}
