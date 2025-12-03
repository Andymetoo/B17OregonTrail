using UnityEngine;

public enum PendingOrderType
{
    None,
    Move,
    ExtinguishFire,
    RepairSystem,
    TreatInjury,
    FeatherEngine,
    RestartEngine
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
    [SerializeField] private string lastInspectedEngineId;

    public string SelectedCrewId => selectedCrewId;
    public PendingOrderType PendingOrder => pendingOrder;
    public string LastInspectedSectionId => lastInspectedSectionId;
    public string LastInspectedEngineId => lastInspectedEngineId;

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

        // Validate selected crew can perform actions (Healthy or Light injured)
        var crew = CrewManager.Instance?.GetCrewById(selectedCrewId);
        if (crew == null)
        {
            Debug.Log("[OrdersUI] Selected crew not found.");
            return;
        }

        if (crew.Status != CrewStatus.Healthy && crew.Status != CrewStatus.Light)
        {
            Debug.Log($"[OrdersUI] {crew.Name} is {crew.Status} and cannot initiate actions. Too severely injured.");
            // Explicitly clear any pending order request
            pendingOrder = PendingOrderType.None;
            pendingTargetId = null;
            EventLogUI.Instance?.Log($"{crew.Name} is too severely injured to perform actions.", Color.red);
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
            lastInspectedEngineId = null; // Clear engine inspection
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
    /// Called when player clicks on an engine button.
    /// If no pending order, inspect the engine. Otherwise, treat as target for engine actions.
    /// </summary>
    public void OnEngineClicked(string engineId)
    {
        // If we don't have a pending order, treat this as an inspect
        if (pendingOrder == PendingOrderType.None)
        {
            // Clear any crew selection when inspecting an engine
            selectedCrewId = null;
            lastInspectedEngineId = engineId;
            lastInspectedSectionId = null; // Clear section inspection
            Debug.Log($"[OrdersUI] Inspecting engine: {engineId}");
            return;
        }

        // Check if this is an engine-specific action
        if (pendingOrder == PendingOrderType.ExtinguishFire || 
            pendingOrder == PendingOrderType.FeatherEngine || 
            pendingOrder == PendingOrderType.RestartEngine)
        {
            if (string.IsNullOrEmpty(selectedCrewId))
            {
                Debug.Log("[OrdersUI] No crew selected when engine target clicked.");
                return;
            }

            pendingTargetId = engineId;
            Debug.Log($"[OrdersUI] Engine target selected: {pendingTargetId} for order {pendingOrder}");
            TryCommitOrder();
            return;
        }

        // Not an engine action - ignore
        Debug.Log($"[OrdersUI] Ignoring engine click for non-engine action: {pendingOrder}");
    }
    
    /// <summary>
    /// Called when player clicks on a system button (gun, radio, navigator, bombsight).
    /// Shows system info in DebugHUD or allows repair action.
    /// </summary>
    public void OnSystemClicked(string systemId)
    {
        // If we don't have a pending order, treat this as an inspect
        if (pendingOrder == PendingOrderType.None)
        {
            // Clear any crew selection when inspecting a system
            selectedCrewId = null;
            lastInspectedSectionId = systemId; // Store in section ID (systems use repair flow)
            lastInspectedEngineId = null; // Clear engine inspection
            Debug.Log($"[OrdersUI] Inspecting system: {systemId}");
            return;
        }

        // Check if this is a repair action
        if (pendingOrder == PendingOrderType.RepairSystem)
        {
            if (string.IsNullOrEmpty(selectedCrewId))
            {
                Debug.Log("[OrdersUI] No crew selected when system repair target clicked.");
                return;
            }

            pendingTargetId = systemId;
            Debug.Log($"[OrdersUI] System repair target selected: {pendingTargetId}");
            TryCommitOrder();
            return;
        }

        // Not a system-compatible action - ignore
        Debug.Log($"[OrdersUI] Ignoring system click for non-system action: {pendingOrder}");
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
        // Allow healthy and lightly injured crew to be selected for orders
        var crew = CrewManager.Instance?.GetCrewById(crewId);
        if (crew != null && crew.Status != CrewStatus.Healthy && crew.Status != CrewStatus.Light)
        {
            Debug.Log($"[OrdersUI] {crew.Name} is too injured to perform actions (status: {crew.Status}).");
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

        // Engine actions (feather/restart) don't use consumables - execute directly
        if (pendingOrder == PendingOrderType.FeatherEngine || pendingOrder == PendingOrderType.RestartEngine)
        {
            ExecuteOrderCommand(useConsumable: false);
            return;
        }

        // Check if we should show consumable choice popup
        bool hasConsumable = false;
        SupplyType consumableType = SupplyType.MedKit;
        
        switch (pendingOrder)
        {
            case PendingOrderType.ExtinguishFire:
                consumableType = SupplyType.FireExtinguisher;
                hasConsumable = SupplyManager.Instance != null && SupplyManager.Instance.Inventory.GetCount(SupplyType.FireExtinguisher) > 0;
                break;
            case PendingOrderType.RepairSystem:
                consumableType = SupplyType.RepairKit;
                hasConsumable = SupplyManager.Instance != null && SupplyManager.Instance.Inventory.GetCount(SupplyType.RepairKit) > 0;
                break;
            case PendingOrderType.TreatInjury:
                consumableType = SupplyType.MedKit;
                hasConsumable = SupplyManager.Instance != null && SupplyManager.Instance.Inventory.GetCount(SupplyType.MedKit) > 0;
                break;
        }

        if (hasConsumable && ActionConfirmationPopup.Instance != null && CrewActionConfig.Instance != null)
        {
            // Show choice popup
            ShowConsumableChoicePopup(pendingOrder, consumableType);
        }
        else
        {
            // No consumable available or popup missing - execute base action immediately
            ExecuteOrderCommand(useConsumable: false);
        }
    }

    private void ShowConsumableChoicePopup(PendingOrderType orderType, SupplyType consumableType)
    {
        var config = CrewActionConfig.Instance;
        var upgrades = UpgradeManager.Instance;
        string actionTitle = "";
        float baseDuration = 0f, consumableDuration = 0f;
        float baseSuccess = 0f, consumableSuccess = 0f;
        string baseEffect = "", consumableEffect = "";
        int available = SupplyManager.Instance.Inventory.GetCount(consumableType);

        switch (orderType)
        {
            case PendingOrderType.ExtinguishFire:
                actionTitle = $"Extinguish Fire in {pendingTargetId}?";
                baseDuration = upgrades != null ? upgrades.GetModifiedExtinguishDuration() : config.baseExtinguishDuration;
                baseSuccess = upgrades != null ? upgrades.GetModifiedExtinguishSuccess() : config.baseExtinguishSuccessChance;
                baseEffect = "Put out fire";
                consumableDuration = upgrades != null ? upgrades.GetModifiedFireExtinguisherDuration() : config.fireExtinguisherDuration;
                consumableSuccess = config.fireExtinguisherSuccessChance;
                consumableEffect = "Put out fire (guaranteed)";
                break;

            case PendingOrderType.RepairSystem:
                actionTitle = $"Repair {pendingTargetId}?";
                int repairMin = upgrades != null ? upgrades.GetModifiedRepairAmountMin() : config.baseRepairAmountMin;
                int repairMax = upgrades != null ? upgrades.GetModifiedRepairAmountMax() : config.baseRepairAmountMax;
                int kitMin = upgrades != null ? upgrades.GetModifiedRepairKitAmountMin() : config.repairKitAmountMin;
                int kitMax = upgrades != null ? upgrades.GetModifiedRepairKitAmountMax() : config.repairKitAmountMax;
                baseDuration = upgrades != null ? upgrades.GetModifiedRepairDuration() : config.baseRepairDuration;
                baseSuccess = upgrades != null ? upgrades.GetModifiedRepairSuccess() : config.baseRepairSuccessChance;
                baseEffect = $"Restore {repairMin}-{repairMax} integrity";
                consumableDuration = upgrades != null ? upgrades.GetModifiedRepairKitDuration() : config.repairKitDuration;
                consumableSuccess = config.repairKitSuccessChance;
                consumableEffect = $"Restore {kitMin}-{kitMax} integrity (guaranteed)";
                break;

            case PendingOrderType.TreatInjury:
                actionTitle = $"Treat {pendingTargetId}?";
                baseDuration = upgrades != null ? upgrades.GetModifiedMedicalDuration() : config.baseMedicalDuration;
                baseSuccess = upgrades != null ? upgrades.GetModifiedMedicalSuccess() : config.baseMedicalSuccessChance;
                baseEffect = "Heal one injury level";
                consumableDuration = upgrades != null ? upgrades.GetModifiedMedkitDuration() : config.medkitDuration;
                consumableSuccess = config.medkitSuccessChance;
                consumableEffect = "Heal one injury level (guaranteed)";
                break;
        }

        ActionConfirmationPopup.Instance.Show(
            actionTitle,
            baseDuration, baseSuccess, baseEffect,
            consumableDuration, consumableSuccess, consumableEffect,
            available,
            (useConsumable) => ExecuteOrderCommand(useConsumable)
        );
    }

    private void ExecuteOrderCommand(bool useConsumable)
    {
        var config = CrewActionConfig.Instance;
        var upgrades = UpgradeManager.Instance;
        if (config == null)
        {
            Debug.LogError("[OrdersUI] CrewActionConfig.Instance is null!");
            return;
        }

        CrewCommand cmd = null;

        switch (pendingOrder)
        {
            case PendingOrderType.Move:
            {
                float duration = CrewManager.Instance != null ? CrewManager.Instance.moveActionDuration : 3f;
                cmd = new MoveCrewCommand(selectedCrewId, pendingTargetId, duration);
                break;
            }

            case PendingOrderType.ExtinguishFire:
            {
                float baseDuration = CrewManager.Instance != null ? CrewManager.Instance.extinguishFireActionDuration : 8f;
                float duration = useConsumable 
                    ? (upgrades != null ? upgrades.GetModifiedFireExtinguisherDuration() : config.fireExtinguisherDuration)
                    : (upgrades != null ? upgrades.GetModifiedExtinguishDuration() : baseDuration);
                float successChance = useConsumable 
                    ? config.fireExtinguisherSuccessChance 
                    : (upgrades != null ? upgrades.GetModifiedExtinguishSuccess() : config.baseExtinguishSuccessChance);
                cmd = new ExtinguishFireCommand(selectedCrewId, pendingTargetId, duration, successChance, useConsumable);
                break;
            }

            case PendingOrderType.RepairSystem:
            {
                float baseDuration = CrewManager.Instance != null ? CrewManager.Instance.repairActionDuration : 10f;
                float duration = useConsumable 
                    ? (upgrades != null ? upgrades.GetModifiedRepairKitDuration() : config.repairKitDuration)
                    : (upgrades != null ? upgrades.GetModifiedRepairDuration() : baseDuration);
                float successChance = useConsumable 
                    ? config.repairKitSuccessChance 
                    : (upgrades != null ? upgrades.GetModifiedRepairSuccess() : config.baseRepairSuccessChance);
                cmd = new RepairSystemCommand(selectedCrewId, pendingTargetId, duration, successChance, useConsumable);
                break;
            }

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
                float baseDuration = CrewManager.Instance != null ? CrewManager.Instance.medicalActionDuration : 10f;
                float duration = useConsumable 
                    ? (upgrades != null ? upgrades.GetModifiedMedkitDuration() : config.medkitDuration)
                    : (upgrades != null ? upgrades.GetModifiedMedicalDuration() : baseDuration);
                float successChance = useConsumable 
                    ? config.medkitSuccessChance 
                    : (upgrades != null ? upgrades.GetModifiedMedicalSuccess() : config.baseMedicalSuccessChance);
                cmd = new TreatInjuryCommand(selectedCrewId, pendingTargetId, duration, successChance, useConsumable);
                break;
            }
            
            case PendingOrderType.FeatherEngine:
            {
                float duration = 5f; // Default feather duration
                cmd = new FeatherEngineCommand(selectedCrewId, pendingTargetId, duration);
                break;
            }
            
            case PendingOrderType.RestartEngine:
            {
                float duration = 8f; // Default restart duration
                cmd = new RestartEngineCommand(selectedCrewId, pendingTargetId, duration);
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
                PendingOrderType.FeatherEngine => $"feathering {pendingTargetId}",
                PendingOrderType.RestartEngine => $"restarting {pendingTargetId}",
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
