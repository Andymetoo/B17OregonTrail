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

    private void Update()
    {
        UpdateTopBar();
        UpdateCrewPanel();
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
        crewActionText.text = ""; // or "Click crew + action to operate"
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
}
