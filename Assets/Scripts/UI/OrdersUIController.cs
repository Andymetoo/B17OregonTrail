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
