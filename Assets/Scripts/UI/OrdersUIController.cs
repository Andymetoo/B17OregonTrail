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
        selectedCrewId = crewId;
        pendingOrder = PendingOrderType.None;
        pendingTargetId = null;

        Debug.Log($"[OrdersUI] Selected crew: {selectedCrewId}");
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

        pendingOrder = orderType;
        pendingTargetId = null;
        Debug.Log($"[OrdersUI] Pending order: {orderType} for {selectedCrewId}");
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
            lastInspectedSectionId = targetId;
            Debug.Log($"[OrdersUI] Inspecting section: {targetId}");
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
        }
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
                cmd = new TreatInjuryCommand(selectedCrewId, pendingTargetId, 10f);
                break;
        }

        if (cmd != null)
        {
            CrewCommandProcessor.Instance.Enqueue(cmd);
            Debug.Log($"[OrdersUI] Enqueued {pendingOrder} from {selectedCrewId} to {pendingTargetId}");
        }

        // Reset order state
        pendingOrder = PendingOrderType.None;
        pendingTargetId = null;
    }
}
