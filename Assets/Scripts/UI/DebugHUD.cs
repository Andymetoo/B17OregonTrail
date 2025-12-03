using System.Linq;
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
    public TMPro.TMP_Text speedText;
    public TMPro.TMP_Text altitudeText;
    public TMPro.TMP_Text nodeText;
    public TMPro.TMP_Text segmentProgressText;
    public TMPro.TMP_Text legNamesText;
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
    
    [Header("Engine Action Buttons")]
    public GameObject extinguishEngineButton;
    public GameObject featherEngineButton;
    public GameObject restartEngineButton;
    
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
            // Show ChaosSimulator hazard phase instead of GameStateManager game phase for better visibility
            string phaseDisplay = ChaosSimulator.Instance != null 
                ? $"Hazard: {ChaosSimulator.Instance.CurrentPhase} ({ChaosSimulator.Instance.PhaseTimeRemaining:F1}s) | Danger: {ChaosSimulator.Instance.CurrentDanger:F2}"
                : $"Game: {GameStateManager.Instance.CurrentPhase}";
            phaseText.text = phaseDisplay;
            timeText.text = $"Time: {GameStateManager.Instance.SimulationTime:F1}s";
        }

        if (MissionManager.Instance != null)
        {
            var mm = MissionManager.Instance;
            fuelText.text = $"Fuel: {mm.FuelRemaining:F1}";

            // Speed from PlaneManager
            if (PlaneManager.Instance != null && speedText != null)
            {
                float mph = PlaneManager.Instance.CurrentCruiseSpeedMph;
                speedText.text = $"Speed: {mph:0} mph";
            }
            
            // Altitude from PlaneManager
            if (PlaneManager.Instance != null && altitudeText != null)
            {
                float altitude = PlaneManager.Instance.currentAltitudeFeet;
                altitudeText.text = $"Altitude: {altitude:F0} ft";
            }

            string currentNode = string.IsNullOrEmpty(mm.CurrentNodeId) ? "None" : mm.CurrentNodeId;
            string nextNode = string.IsNullOrEmpty(mm.NextNodeId) ? "None" : mm.NextNodeId;

            nodeText.text = $"Node: {currentNode} -> {nextNode}";
            if (legNamesText != null)
            {
                legNamesText.text = $"Leg: {currentNode} → {nextNode}";
            }
            if (mm.IsTravelling)
            {
                float miles = mm.DistanceRemainingMi;
                segmentProgressText.text = $"Miles to next waypoint: {miles:F1} mi";
            }
            else
            {
                segmentProgressText.text = "Miles to next waypoint: (waiting)";
            }
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

        // Check for inspected engine
        if (OrdersUIController.Instance != null &&
            !string.IsNullOrEmpty(OrdersUIController.Instance.LastInspectedEngineId))
        {
            ShowEngineInfo(OrdersUIController.Instance.LastInspectedEngineId);
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

        if (crew.CurrentAction != null && crew.CurrentAction.Type != ActionType.Idle)
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
        UpdateEngineActionButtonVisibility(false);
    }

    private void ShowEngineInfo(string engineId)
    {
        var engine = PlaneManager.Instance?.GetEngine(engineId);
        if (engine == null)
        {
            crewNameText.text = $"Engine: {engineId} (not found)";
            crewStatusText.text = "";
            crewStationText.text = "";
            crewActionText.text = "";
            UpdateActionButtonVisibility(false);
            UpdateEngineActionButtonVisibility(false);
            return;
        }

        crewNameText.text = $"Engine: {engine.Id}";
        crewStatusText.text = $"Integrity: {engine.Integrity} ({engine.Status})";
        crewStationText.text = $"On Fire: {engine.OnFire} | Feathered: {engine.IsFeathered}";
        crewActionText.text = ""; // No systems display for engines
        
        // Hide crew action buttons when showing engine info
        UpdateActionButtonVisibility(false);
        
        // Show contextual engine action buttons
        UpdateEngineActionButtonVisibility(true);
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
    
    private void UpdateEngineActionButtonVisibility(bool visible)
    {
        if (!visible)
        {
            // Hide all engine buttons
            if (extinguishEngineButton != null) extinguishEngineButton.SetActive(false);
            if (featherEngineButton != null) featherEngineButton.SetActive(false);
            if (restartEngineButton != null) restartEngineButton.SetActive(false);
            return;
        }
        
        // Show buttons contextually based on engine state and pilot/copilot occupancy
        if (OrdersUIController.Instance == null || PlaneManager.Instance == null)
            return;
            
        string engineId = OrdersUIController.Instance.LastInspectedEngineId;
        var engine = PlaneManager.Instance.GetEngine(engineId);
        if (engine == null) return;
        
        // Check if pilot or copilot is occupied
        bool pilotPresent = IsPilotOrCopilotOccupied();
        
        // Debug logging
        Debug.Log($"[DebugHUD] Engine {engine.Id}: OnFire={engine.OnFire}, Feathered={engine.IsFeathered}, Status={engine.Status}, Pilot={pilotPresent}");
        
        // Extinguish button: ONLY show if engine is on fire
        if (extinguishEngineButton != null)
        {
            bool showExtinguish = engine.OnFire;
            extinguishEngineButton.SetActive(showExtinguish);
            
            // Gray out if no pilot
            var button = extinguishEngineButton.GetComponent<Button>();
            if (button != null) button.interactable = pilotPresent;
            
            Debug.Log($"[DebugHUD] Extinguish button: show={showExtinguish}, interactable={pilotPresent}");
        }
        
        // Feather button: Only show if engine is damaged/critical but NOT destroyed and NOT on fire
        // Purpose: Shut down a damaged engine to prevent further damage/explosion
        if (featherEngineButton != null)
        {
            bool isDamagedButRunning = !engine.IsFeathered && !engine.OnFire && 
                                        engine.Status != SystemStatus.Destroyed && 
                                        engine.Integrity < 75; // Only show when damaged
            featherEngineButton.SetActive(isDamagedButRunning);
            
            var button = featherEngineButton.GetComponent<Button>();
            if (button != null) button.interactable = pilotPresent;
            
            Debug.Log($"[DebugHUD] Feather button: show={isDamagedButRunning}, integrity={engine.Integrity}");
        }
        
        // Restart button: ONLY show if engine is feathered (and not destroyed)
        if (restartEngineButton != null)
        {
            bool showRestart = engine.IsFeathered && engine.Status != SystemStatus.Destroyed;
            restartEngineButton.SetActive(showRestart);
            
            var button = restartEngineButton.GetComponent<Button>();
            if (button != null) button.interactable = pilotPresent;
            
            Debug.Log($"[DebugHUD] Restart button: show={showRestart}");
        }
    }
    
    /// <summary>
    /// Check if Pilot or CoPilot stations are occupied.
    /// Engine actions require a pilot to be present.
    /// </summary>
    private bool IsPilotOrCopilotOccupied()
    {
        if (StationManager.Instance == null)
        {
            Debug.LogWarning("[DebugHUD] StationManager.Instance is null!");
            return false;
        }
        
        // Check if either Pilot or CoPilot station is occupied
        var pilotStation = StationManager.Instance.GetStation(StationType.Pilot);
        var copilotStation = StationManager.Instance.GetStation(StationType.CoPilot);
        
        bool pilotOccupied = pilotStation != null && pilotStation.IsOccupied;
        bool copilotOccupied = copilotStation != null && copilotStation.IsOccupied;
        
        Debug.Log($"[DebugHUD] Pilot station: {(pilotStation != null ? $"found, occupied={pilotOccupied}, crewId={pilotStation.OccupiedByCrewId}" : "NOT FOUND")}");
        Debug.Log($"[DebugHUD] Copilot station: {(copilotStation != null ? $"found, occupied={copilotOccupied}, crewId={copilotStation.OccupiedByCrewId}" : "NOT FOUND")}");
        
        bool result = pilotOccupied || copilotOccupied;
        Debug.Log($"[DebugHUD] IsPilotOrCopilotOccupied = {result}");
        
        return result;
    }
    
    // Engine action button callbacks
    // These auto-select pilot/copilot and execute on the currently inspected engine
    public void OnExtinguishEngineButton()
    {
        Debug.Log("[DebugHUD] OnExtinguishEngineButton() CALLED!");
        ExecuteEngineAction(PendingOrderType.ExtinguishFire);
    }
    
    public void OnFeatherEngineButton()
    {
        Debug.Log("[DebugHUD] OnFeatherEngineButton() CALLED!");
        ExecuteEngineAction(PendingOrderType.FeatherEngine);
    }
    
    public void OnRestartEngineButton()
    {
        Debug.Log("[DebugHUD] OnRestartEngineButton() CALLED!");
        ExecuteEngineAction(PendingOrderType.RestartEngine);
    }
    
    /// <summary>
    /// Execute an engine action with auto-selected pilot/copilot on the inspected engine.
    /// </summary>
    private void ExecuteEngineAction(PendingOrderType actionType)
    {
        if (OrdersUIController.Instance == null || CrewCommandProcessor.Instance == null)
            return;
            
        // Get the inspected engine
        string engineId = OrdersUIController.Instance.LastInspectedEngineId;
        if (string.IsNullOrEmpty(engineId))
        {
            Debug.LogWarning("[DebugHUD] No engine selected for action.");
            return;
        }
        
        // Auto-select pilot or copilot (cascade: pilot first, then copilot)
        string pilotCrewId = GetPilotOrCopilotCrewId();
        if (string.IsNullOrEmpty(pilotCrewId))
        {
            Debug.LogWarning("[DebugHUD] No pilot or copilot available for engine action.");
            EventLogUI.Instance?.Log("Engine actions require a pilot or copilot to be present.", Color.red);
            return;
        }
        
        // Create and enqueue the command directly
        CrewCommand cmd = null;
        switch (actionType)
        {
            case PendingOrderType.ExtinguishFire:
                cmd = new ExtinguishFireCommand(pilotCrewId, engineId, 8f, 0.7f, useConsumable: false);
                break;
            case PendingOrderType.FeatherEngine:
                cmd = new FeatherEngineCommand(pilotCrewId, engineId, 5f);
                break;
            case PendingOrderType.RestartEngine:
                cmd = new RestartEngineCommand(pilotCrewId, engineId, 8f);
                break;
        }
        
        if (cmd != null)
        {
            Debug.Log($"[DebugHUD] Enqueueing command: {actionType} for crew {pilotCrewId} on {engineId}");
            CrewCommandProcessor.Instance.Enqueue(cmd);
            var crew = CrewManager.Instance?.GetCrewById(pilotCrewId);
            string crewName = crew != null ? crew.Name : pilotCrewId;
            Debug.Log($"[DebugHUD] Crew found: {crewName}, Current Station: {crew?.CurrentStation}, Current Action: {crew?.CurrentAction?.Type}");
            string actionText = actionType switch
            {
                PendingOrderType.ExtinguishFire => $"extinguishing fire on {engineId}",
                PendingOrderType.FeatherEngine => $"feathering {engineId}",
                PendingOrderType.RestartEngine => $"restarting {engineId}",
                _ => $"performing action on {engineId}"
            };
            EventLogUI.Instance?.Log($"{crewName} is {actionText}", Color.cyan);
            Debug.Log($"[DebugHUD] {crewName} executing {actionType} on {engineId}");
        }
    }
    
    /// <summary>
    /// Get crew ID of pilot (first priority) or copilot (second priority).
    /// Returns null if neither station is occupied.
    /// </summary>
    private string GetPilotOrCopilotCrewId()
    {
        if (StationManager.Instance == null) return null;
        
        // Try pilot first
        var pilot = StationManager.Instance.GetStationOccupant(StationType.Pilot);
        if (pilot != null)
        {
            return pilot.Id;
        }
        
        // Fall back to copilot
        var copilot = StationManager.Instance.GetStationOccupant(StationType.CoPilot);
        if (copilot != null)
        {
            return copilot.Id;
        }
        
        return null;
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

