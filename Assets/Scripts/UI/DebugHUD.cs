using UnityEngine;
using UnityEngine.UI;
using TMPro;      // For Button, Text
// If you prefer TextMeshPro, swap Text -> TMP_Text and add "using TMPro;"

public class DebugHUD : MonoBehaviour
{
    [Header("Text Readouts")]
    public TMPro.TMP_Text phaseText;
    public TMPro.TMP_Text timeText;
    public TMPro.TMP_Text fuelText;
    public TMPro.TMP_Text nodeText;
    public TMPro.TMP_Text segmentProgressText;
    public TMPro.TMP_Text pendingActionText; // Shows what action is queued
    [Header("Crew Readout (Debug: one crew)")]
    public string watchedCrewId = "Crew_1";
    public TMPro.TMP_Text crewNameText;
    public TMPro.TMP_Text crewStatusText;
    public TMPro.TMP_Text crewStationText;
    public TMPro.TMP_Text crewActionText;

    [Header("Hardcoded IDs for demo commands")]
    public string moveTargetStationId = "TailGun";
    public string fireSectionId = "LeftWing";
    public string repairSystemId = "Engine1";
    public string injuredCrewId = "Crew_2";

    [Header("Action Buttons (for visibility control)")]
    public GameObject moveButton;
    public GameObject fireButton;
    public GameObject repairButton;
    public GameObject medicalButton;
    public GameObject cancelActionButton;

    [Header("Pending Message Display")]
    public float pendingMessageLingerSeconds = 5f;

    private string _pendingMessage;
    private float _pendingMessageExpireTime;

    private void Update()
    {
        UpdateTopBar();
        UpdateCrewPanel();
        UpdateCancelButtonVisibility();
    }

    private void Start()
    {
        if (OrdersUIController.Instance != null)
        {
            OrdersUIController.Instance.OnPendingActionMessage += HandlePendingActionMessage;
        }
    }

    private void OnDestroy()
    {
        if (OrdersUIController.Instance != null)
        {
            OrdersUIController.Instance.OnPendingActionMessage -= HandlePendingActionMessage;
        }
    }

    private void UpdateTopBar()
    {
        if (GameStateManager.Instance != null)
        {
            phaseText.text = $"Phase: {GameStateManager.Instance.CurrentPhase}";
            timeText.text = $"Time: {GameStateManager.Instance.SimulationTime:F1}s";
        }

        if (MissionManager.Instance != null)
        {
            var mm = MissionManager.Instance;
            fuelText.text = $"Fuel: {mm.FuelRemaining:F1}";

            string currentNode = string.IsNullOrEmpty(mm.CurrentNodeId) ? "None" : mm.CurrentNodeId;
            string nextNode = string.IsNullOrEmpty(mm.NextNodeId) ? "None" : mm.NextNodeId;

            nodeText.text = $"Node: {currentNode} -> {nextNode}";
            segmentProgressText.text = $"Segment: {mm.SegmentProgress01 * 100f:F0}%";
        }
        
        // Show pending action status
        UpdatePendingActionText();
    }
    
    private void UpdatePendingActionText()
    {
        if (pendingActionText == null) return;
        
        // If we have a message and it's not expired, keep showing it
        if (!string.IsNullOrEmpty(_pendingMessage) && Time.time < _pendingMessageExpireTime)
        {
            pendingActionText.text = _pendingMessage;
        }
        else
        {
            pendingActionText.text = "";
        }
    }

    private void HandlePendingActionMessage(string message)
    {
        if (pendingActionText == null) return;
        _pendingMessage = $"<color=yellow>{message}</color>";
        _pendingMessageExpireTime = Time.time + pendingMessageLingerSeconds;
    }

    private void UpdateCrewPanel()
    {
        if (CrewManager.Instance == null) return;

        // Prefer showing selected crew if there is one
        string crewIdToShow = null;
        if (OrdersUIController.Instance != null &&
            !string.IsNullOrEmpty(OrdersUIController.Instance.SelectedCrewId))
        {
            crewIdToShow = OrdersUIController.Instance.SelectedCrewId;
        }

        if (!string.IsNullOrEmpty(crewIdToShow))
        {
            ShowCrewInfo(crewIdToShow);
            return;
        }

        // Otherwise, if we have an inspected section, show section info
        if (OrdersUIController.Instance != null &&
            !string.IsNullOrEmpty(OrdersUIController.Instance.LastInspectedSectionId))
        {
            ShowSectionInfo(OrdersUIController.Instance.LastInspectedSectionId);
            return;
        }

        // Nothing selected
        crewNameText.text = "Crew / Section: (none selected)";
        crewStatusText.text = "";
        crewStationText.text = "";
        crewActionText.text = "";
        
        // Hide action buttons when no crew selected
        UpdateActionButtonVisibility(false);
    }

    private void ShowCrewInfo(string crewId)
    {
        var crew = CrewManager.Instance?.GetCrewById(crewId);
        if (crew == null)
        {
            crewNameText.text = "Crew: (not found)";
            crewStatusText.text = "";
            crewStationText.text = "";
            crewActionText.text = "";
            return;
        }

        crewNameText.text = $"Crew: {crew.Name} ({crew.Role})";
        crewStatusText.text = $"Status: {crew.Status}";
        crewStationText.text = $"Station: {crew.CurrentStationId}";

        if (crew.CurrentAction != null)
        {
            float pct = crew.CurrentAction.Duration > 0f
                ? Mathf.Clamp01(crew.CurrentAction.Elapsed / crew.CurrentAction.Duration) * 100f
                : 0f;
            crewActionText.text = $"Action: {crew.CurrentAction.Type} -> {crew.CurrentAction.TargetId} ({pct:F0}%)";
        }
        else
        {
            crewActionText.text = "Action: Idle";
        }
        
        // Show action buttons when crew is selected
        UpdateActionButtonVisibility(true);
    }

    private void ShowSectionInfo(string sectionId)
    {
        var section = PlaneManager.Instance?.GetSection(sectionId);
        if (section == null)
        {
            crewNameText.text = $"Section: {sectionId} (not found)";
            crewStatusText.text = "";
            crewStationText.text = "";
            crewActionText.text = "";
            return;
        }

        crewNameText.text = $"Section: {section.Id}";
        crewStatusText.text = $"Integrity: {section.Integrity}";
        crewStationText.text = $"On Fire: {section.OnFire}";

        // Summarize systems in this section (operational/damaged/destroyed)
        int op = 0, dmg = 0, des = 0;
        if (PlaneManager.Instance != null)
        {
            foreach (var sys in PlaneManager.Instance.Systems)
            {
                if (sys.SectionId != section.Id) continue;
                switch (sys.Status)
                {
                    case SystemStatus.Operational: op++; break;
                    case SystemStatus.Damaged: dmg++; break;
                    case SystemStatus.Destroyed: des++; break;
                }
            }
        }
        crewActionText.text = $"Systems: Op {op} / Dmg {dmg} / Des {des}";
        
        // Hide action buttons when showing section info
        UpdateActionButtonVisibility(false);
    }


    // --------------------
    // BUTTON HOOKS (Now using OrdersUIController)
    // --------------------
    // These methods now delegate to OrdersUIController instead of creating commands directly.
    // The old hardcoded approach is being phased out in favor of the click-to-select-target workflow.

    public void OnMoveCrewButton()
    {
        if (OrdersUIController.Instance == null) return;
        OrdersUIController.Instance.OnActionClicked(PendingOrderType.Move);
    }

    public void OnExtinguishFireButton()
    {
        if (OrdersUIController.Instance == null) return;
        OrdersUIController.Instance.OnActionClicked(PendingOrderType.ExtinguishFire);
    }

    public void OnRepairSystemButton()
    {
        if (OrdersUIController.Instance == null) return;
        OrdersUIController.Instance.OnActionClicked(PendingOrderType.RepairSystem);
    }

    public void OnTreatInjuryButton()
    {
        if (OrdersUIController.Instance == null) return;
        OrdersUIController.Instance.OnActionClicked(PendingOrderType.TreatInjury);
    }

    private void UpdateActionButtonVisibility(bool visible)
    {
        if (moveButton != null) moveButton.SetActive(visible);
        if (fireButton != null) fireButton.SetActive(visible);
        if (repairButton != null) repairButton.SetActive(visible);
        if (medicalButton != null) medicalButton.SetActive(visible);
    }

    private void UpdateCancelButtonVisibility()
    {
        if (cancelActionButton == null) return;
        var ctrl = OrdersUIController.Instance;
        bool show = ctrl != null && ctrl.PendingOrder != PendingOrderType.None;
        cancelActionButton.SetActive(show);
    }

    public void OnCancelPendingButton()
    {
        OrdersUIController.Instance?.CancelPendingAction();
        UpdateCancelButtonVisibility();
    }
}
