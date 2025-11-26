using UnityEngine;

/// <summary>
/// Fixed simulation tick controller - keeps simulation deterministic and pausable.
/// All managers should register their Tick methods with this controller.
/// </summary>
public class SimulationTicker : MonoBehaviour
{
    public static SimulationTicker Instance { get; private set; }
    
    [Header("Simulation Control")]
    [SerializeField] private bool isPaused = false;
    [SerializeField] private float timeScale = 1f;
    [SerializeField] private float tickRate = 30f; // Fixed updates per second
    
    private float lastTickTime;
    private float simulationTime;
    
    public float SimulationTime => simulationTime;
    public bool IsPaused => isPaused;
    public float TimeScale => timeScale;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        lastTickTime = Time.time;
    }
    
    private void Update()
    {
        if (isPaused) return;
        
        float currentTime = Time.time;
        float deltaTime = (currentTime - lastTickTime) * timeScale;
        
        // Fixed tick rate to keep simulation consistent
        if (deltaTime >= (1f / tickRate))
        {
            simulationTime += deltaTime;
            TickAllSystems(deltaTime);
            lastTickTime = currentTime;
        }
    }
    
    private void TickAllSystems(float deltaTime)
    {
        // Tick all managers in dependency order
        CrewManager.Instance?.Tick(deltaTime);
        PlaneManager.Instance?.Tick(deltaTime);
        MissionManager.Instance?.Tick(deltaTime);
        CrewCommandProcessor.Instance?.Tick(deltaTime);
        // EventManager would go here when created
    }
    
    public void Pause()
    {
        isPaused = true;
        Debug.Log("[Simulation] Paused");
    }
    
    public void Resume()
    {
        isPaused = false;
        lastTickTime = Time.time; // Reset to avoid time jump
        Debug.Log("[Simulation] Resumed");
    }
    
    public void SetTimeScale(float scale)
    {
        timeScale = Mathf.Clamp(scale, 0.1f, 5f);
        Debug.Log($"[Simulation] Time scale set to {timeScale}x");
    }
    
    /// <summary>
    /// Get current game state snapshot for debugging/saving
    /// </summary>
    public GameState GetGameStateSnapshot()
    {
        var gameState = new GameState();
        
        // Populate from managers
        if (MissionManager.Instance != null)
        {
            gameState.currentNodeId = MissionManager.Instance.CurrentNodeId;
            gameState.nextNodeId = MissionManager.Instance.NextNodeId;
            gameState.segmentProgress01 = MissionManager.Instance.SegmentProgress01;
            gameState.fuelRemaining = MissionManager.Instance.FuelRemaining;
            gameState.missionTime = simulationTime;
        }
        
        if (CrewManager.Instance != null)
        {
            gameState.crewMembers.AddRange(CrewManager.Instance.AllCrew);
        }
        
        if (PlaneManager.Instance != null)
        {
            gameState.planeSections.AddRange(PlaneManager.Instance.Sections);
            gameState.planeSystems.AddRange(PlaneManager.Instance.Systems);
        }
        
        return gameState;
    }
}