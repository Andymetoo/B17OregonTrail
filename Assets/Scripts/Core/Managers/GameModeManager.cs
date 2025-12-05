using UnityEngine;
using System;

/// <summary>
/// Manages transitions between UI Mode (2D plane management) and Combat Mode (3D first-person gunner view).
/// Handles camera switching, UI canvas toggling, and input context changes.
/// </summary>
public class GameModeManager : MonoBehaviour
{
    public static GameModeManager Instance { get; private set; }

    [Header("Cameras")]
    [Tooltip("Main orthographic camera for 2D UI mode.")]
    [SerializeField] private Camera mainUICamera;
    [Tooltip("Perspective camera for 3D first-person combat mode.")]
    [SerializeField] private Camera combatCamera;

    [Header("UI Canvases")]
    [Tooltip("Canvas for 2D plane management UI (Orders, Overview, etc).")]
    [SerializeField] private Canvas uiModeCanvas;
    [Tooltip("Canvas for 3D combat mode UI (station switcher, return button).")]
    [SerializeField] private Canvas combatModeCanvas;

    [Header("Audio Listeners")]
    [Tooltip("Disable redundant audio listeners to prevent warnings.")]
    [SerializeField] private bool autoManageAudioListeners = true;

    /// <summary>
    /// Current game mode.
    /// </summary>
    public GameMode CurrentMode { get; private set; } = GameMode.UIMode;

    /// <summary>
    /// Event fired when mode changes.
    /// </summary>
    public event Action<GameMode> OnModeChanged;

    /// <summary>
    /// Current gunner station (only relevant in Combat mode).
    /// </summary>
    public GunnerStation CurrentStation { get; private set; } = GunnerStation.None;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // Start in UI mode
        SetMode(GameMode.UIMode);
    }

    /// <summary>
    /// Enter combat mode at the specified gunner station.
    /// </summary>
    public void EnterCombatMode(GunnerStation station)
    {
        if (station == GunnerStation.None)
        {
            Debug.LogWarning("[GameModeManager] Cannot enter combat mode with station = None");
            return;
        }

        CurrentStation = station;
        SetMode(GameMode.CombatMode);
        
        Debug.Log($"[GameModeManager] Entered combat mode at {station}");
    }

    /// <summary>
    /// Exit combat mode and return to UI mode.
    /// </summary>
    public void ExitCombatMode()
    {
        CurrentStation = GunnerStation.None;
        SetMode(GameMode.UIMode);
        
        Debug.Log("[GameModeManager] Exited combat mode");
    }

    /// <summary>
    /// Switch to a different gunner station (only works if already in combat mode).
    /// </summary>
    public void SwitchStation(GunnerStation newStation)
    {
        if (CurrentMode != GameMode.CombatMode)
        {
            Debug.LogWarning("[GameModeManager] Cannot switch station - not in combat mode");
            return;
        }

        if (newStation == GunnerStation.None)
        {
            Debug.LogWarning("[GameModeManager] Cannot switch to station = None");
            return;
        }

        CurrentStation = newStation;
        
        // Notify station controller to update camera position
        if (GunnerStationController.Instance != null)
        {
            GunnerStationController.Instance.MoveToStation(newStation);
        }
        
        Debug.Log($"[GameModeManager] Switched to station: {newStation}");
    }

    /// <summary>
    /// Internal method to handle mode switching logic.
    /// </summary>
    private void SetMode(GameMode mode)
    {
        CurrentMode = mode;

        switch (mode)
        {
            case GameMode.UIMode:
                EnableUIMode();
                break;
            case GameMode.CombatMode:
                EnableCombatMode();
                break;
        }

        OnModeChanged?.Invoke(mode);
    }

    private void EnableUIMode()
    {
        // Enable UI camera and canvas
        if (mainUICamera != null)
        {
            mainUICamera.enabled = true;
            if (autoManageAudioListeners)
            {
                var listener = mainUICamera.GetComponent<AudioListener>();
                if (listener != null) listener.enabled = true;
            }
        }
        if (uiModeCanvas != null) uiModeCanvas.enabled = true;

        // Disable combat camera and canvas
        if (combatCamera != null)
        {
            combatCamera.enabled = false;
            if (autoManageAudioListeners)
            {
                var listener = combatCamera.GetComponent<AudioListener>();
                if (listener != null) listener.enabled = false;
            }
        }
        if (combatModeCanvas != null) combatModeCanvas.enabled = false;
    }

    private void EnableCombatMode()
    {
        // Disable UI camera and canvas
        if (mainUICamera != null)
        {
            mainUICamera.enabled = false;
            if (autoManageAudioListeners)
            {
                var listener = mainUICamera.GetComponent<AudioListener>();
                if (listener != null) listener.enabled = false;
            }
        }
        if (uiModeCanvas != null) uiModeCanvas.enabled = false;

        // Enable combat camera and canvas
        if (combatCamera != null)
        {
            combatCamera.enabled = true;
            if (autoManageAudioListeners)
            {
                var listener = combatCamera.GetComponent<AudioListener>();
                if (listener != null) listener.enabled = true;
            }
        }
        if (combatModeCanvas != null) combatModeCanvas.enabled = true;

        // Position combat camera at current station
        if (GunnerStationController.Instance != null && CurrentStation != GunnerStation.None)
        {
            GunnerStationController.Instance.MoveToStation(CurrentStation);
        }
    }

    /// <summary>
    /// Check if a specific gunner station is available (crew is healthy and not incapacitated).
    /// </summary>
    public bool IsStationAvailable(GunnerStation station)
    {
        if (CrewManager.Instance == null) return false;

        // Map station to crew role
        CrewRole? crewRole = GetCrewRoleForStation(station);
        if (!crewRole.HasValue) return false;

        // Find crew member for this role
        var crew = CrewManager.Instance.AllCrew.Find(c => c.Role == crewRole.Value);
        if (crew == null) return false;

        // Station is available if crew is healthy
        return crew.Status == CrewStatus.Healthy;
    }

    private CrewRole? GetCrewRoleForStation(GunnerStation station)
    {
        switch (station)
        {
            case GunnerStation.TopTurret: return CrewRole.Gunner;
            case GunnerStation.BallTurret: return CrewRole.Gunner;
            case GunnerStation.LeftWaist: return CrewRole.Gunner;
            case GunnerStation.RightWaist: return CrewRole.Gunner;
            case GunnerStation.TailGunner: return CrewRole.Gunner;
            case GunnerStation.Nose: return CrewRole.Bombardier; // Bombardier operates nose guns
            default: return null;
        }
    }
}

/// <summary>
/// Game modes for the main gameplay loop.
/// </summary>
public enum GameMode
{
    UIMode,      // 2D plane management view (default)
    CombatMode   // 3D first-person gunner view
}

/// <summary>
/// All gunner stations on the B-17.
/// </summary>
public enum GunnerStation
{
    None,
    TopTurret,
    BallTurret,
    LeftWaist,
    RightWaist,
    TailGunner,
    Nose
}
