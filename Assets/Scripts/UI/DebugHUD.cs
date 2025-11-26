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

        // Prefer currently selected crew, fall back to watchedCrewId
        string idToShow = watchedCrewId;
        if (OrdersUIController.Instance != null &&
            !string.IsNullOrEmpty(OrdersUIController.Instance.SelectedCrewId))
        {
            idToShow = OrdersUIController.Instance.SelectedCrewId;
        }

        var crew = CrewManager.Instance.GetCrewById(idToShow);
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


    // --------------------
    // BUTTON HOOKS
    // --------------------

    public void OnMoveCrewButton()
    {
        if (CrewCommandProcessor.Instance == null) return;

        var cmd = new MoveCrewCommand(watchedCrewId, moveTargetStationId, moveDuration: 4f);
        CrewCommandProcessor.Instance.Enqueue(cmd);
    }

    public void OnExtinguishFireButton()
    {
        if (CrewCommandProcessor.Instance == null) return;

        var cmd = new ExtinguishFireCommand(watchedCrewId, fireSectionId, duration: 8f);
        CrewCommandProcessor.Instance.Enqueue(cmd);
    }

    public void OnRepairSystemButton()
    {
        if (CrewCommandProcessor.Instance == null) return;

        var cmd = new RepairSystemCommand(watchedCrewId, repairSystemId, duration: 10f);
        CrewCommandProcessor.Instance.Enqueue(cmd);
    }

    public void OnTreatInjuryButton()
    {
        if (CrewCommandProcessor.Instance == null) return;

        var cmd = new TreatInjuryCommand(watchedCrewId, injuredCrewId, duration: 10f);
        CrewCommandProcessor.Instance.Enqueue(cmd);
    }
}
