using UnityEngine;

/// <summary>
/// UI panel for debug controls. Provides buttons to trigger debug functions.
/// Attach to a UI panel GameObject with buttons as children.
/// </summary>
public class DebugPanel : MonoBehaviour
{
    [Header("Panel Control")]
    [Tooltip("Hotkey to toggle debug panel visibility.")]
    public KeyCode togglePanelHotkey = KeyCode.F1;
    
    [Tooltip("Start with panel visible or hidden.")]
    public bool startVisible = true;
    
    private GameObject panelObject;

    private void Awake()
    {
        panelObject = gameObject;
        panelObject.SetActive(startVisible);
    }

    private void Update()
    {
        // Toggle panel visibility
        if (Input.GetKeyDown(togglePanelHotkey))
        {
            panelObject.SetActive(!panelObject.activeSelf);
        }
    }

    // Button callbacks - wire these up to UI buttons in Inspector
    
    public void OnRandomFireButton()
    {
        DebugManager.Instance?.StartRandomFire();
    }
    
    public void OnDamageEngineButton()
    {
        DebugManager.Instance?.DamageRandomEngine();
    }
    
    public void OnDestroyEngineButton()
    {
        DebugManager.Instance?.DestroyRandomEngine();
    }
    
    public void OnRepairAllEnginesButton()
    {
        DebugManager.Instance?.RepairAllEngines();
    }
    
    public void OnExtinguishAllFiresButton()
    {
        DebugManager.Instance?.ExtinguishAllFires();
    }
    
    public void OnIncreaseAltitudeButton()
    {
        DebugManager.Instance?.AdjustAltitude(1000f);
    }
    
    public void OnDecreaseAltitudeButton()
    {
        DebugManager.Instance?.AdjustAltitude(-1000f);
    }
    
    public void OnAddFuelButton()
    {
        DebugManager.Instance?.AdjustFuel(100f);
    }
    
    public void OnRemoveFuelButton()
    {
        DebugManager.Instance?.AdjustFuel(-100f);
    }
    
    public void OnSetCriticalAltitudeButton()
    {
        if (PlaneManager.Instance != null)
        {
            PlaneManager.Instance.currentAltitudeFeet = 3000f;
            EventLogUI.Instance?.Log("[DEBUG] Altitude set to CRITICAL: 3000 ft", Color.red);
        }
    }
    
    public void OnSetMaxAltitudeButton()
    {
        if (PlaneManager.Instance != null)
        {
            PlaneManager.Instance.currentAltitudeFeet = 25000f;
            EventLogUI.Instance?.Log("[DEBUG] Altitude set to MAX: 25000 ft", Color.green);
        }
    }
}
