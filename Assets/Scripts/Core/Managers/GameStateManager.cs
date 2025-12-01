using System;
using UnityEngine;

public enum GamePhase
{
    Cruise,         // Flying along segments between nodes
    FighterCombat,  // In a fighter encounter / turret minigame
    NodeSelection,  // On the nav map choosing next node
    BombRun,        // Over the target, bombing minigame
    Debrief,        // Post-mission results / meta layer
    Paused          // Global pause
}

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    [Header("Time Settings")]
    [Tooltip("Base time scale for the simulation (1 = normal, 0.5 = slow motion).")]
    public float timeScale = 1f;

    /// <summary>
    /// Total simulated time since mission start (in seconds).
    /// This only advances when the game is not paused.
    /// </summary>
    public float SimulationTime { get; private set; }

    /// <summary>
    /// Current high-level phase of the game.
    /// </summary>
    public GamePhase CurrentPhase { get; private set; } = GamePhase.Cruise;

    /// <summary>
    /// Fired whenever the phase changes.
    /// </summary>
    public event Action<GamePhase> OnPhaseChanged;

    public bool IsPaused => CurrentPhase == GamePhase.Paused;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // optional, if you want it across scenes
    }

    private void Update()
    {
        // We centralize all simulation ticking here.
        if (IsPaused) return;

        float dt = Time.deltaTime * timeScale;

        // Advance global simulation time
        SimulationTime += dt;

        // If a dedicated SimulationTicker exists, let it drive ticking to avoid double ticks.
        if (SimulationTicker.Instance == null)
        {
            // Fallback ticking when no SimulationTicker present.
            CrewManager.Instance?.Tick(dt);
            PlaneManager.Instance?.Tick(dt);
            CrewCommandProcessor.Instance?.Tick(dt);
        }

        // Later:
        // if (MissionManager.Instance != null) MissionManager.Instance.Tick(dt);
        // if (EventManager.Instance != null) EventManager.Instance.Tick(dt);
    }

    // ------------------------------------------------------------------
    // PHASE CONTROL
    // ------------------------------------------------------------------

    public void SetPhase(GamePhase newPhase)
    {
        if (newPhase == CurrentPhase) return;

        CurrentPhase = newPhase;
        OnPhaseChanged?.Invoke(CurrentPhase);

        // You can add enter/exit logic per phase here later if needed.
        // e.g., switch cameras, toggle UI canvases, etc.
    }

    public void PauseGame()
    {
        SetPhase(GamePhase.Paused);
    }

    public void ResumeGame(GamePhase resumeToPhase = GamePhase.Cruise)
    {
        SetPhase(resumeToPhase);
    }

    public void TogglePause()
    {
        if (IsPaused)
        {
            // For now we just resume to Cruise.
            // Later, you may want to remember the previous phase.
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    /// <summary>
    /// Helper for other systems to request a fighter combat phase.
    /// </summary>
    public void EnterFighterCombat()
    {
        SetPhase(GamePhase.FighterCombat);
    }

    /// <summary>
    /// Helper to return from fighter combat back to cruise.
    /// </summary>
    public void ExitFighterCombat()
    {
        SetPhase(GamePhase.Cruise);
    }

    public void EnterNodeSelection()
    {
        SetPhase(GamePhase.NodeSelection);
    }

    public void ExitNodeSelectionToCruise()
    {
        SetPhase(GamePhase.Cruise);
    }

    public void EnterBombRun()
    {
        SetPhase(GamePhase.BombRun);
    }

    public void EnterDebrief()
    {
        SetPhase(GamePhase.Debrief);
    }
}
