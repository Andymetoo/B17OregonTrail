using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Simple scene management for transitioning between mission and meta progression.
/// Keeps the architecture clean by separating mission gameplay from between-runs progression.
/// </summary>
public class SceneController : MonoBehaviour
{
    public static SceneController Instance { get; private set; }
    
    [Header("Scene Names")]
    [SerializeField] private string missionSceneName = "MissionScene";
    [SerializeField] private string hangarSceneName = "HangarScene";
    [SerializeField] private string briefingSceneName = "BriefingScene";
    
    public enum GameScene
    {
        Mission,    // Active bomber mission with crew management
        Hangar,     // Between-missions shop, upgrades, crew management
        Briefing    // Mission selection and planning
    }
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    /// <summary>
    /// Load the mission scene and start a new mission
    /// </summary>
    public void StartMission(string missionId = "")
    {
        Debug.Log($"[Scene] Starting mission: {missionId}");
        // Could save mission ID to persistent data here
        LoadScene(missionSceneName);
    }
    
    /// <summary>
    /// Mission completed - return to hangar for repairs/upgrades
    /// </summary>
    public void CompleteMission(bool success, int pointsEarned = 0)
    {
        Debug.Log($"[Scene] Mission completed. Success: {success}, Points: {pointsEarned}");
        // Save mission results to persistent progression data here
        LoadScene(hangarSceneName);
    }
    
    /// <summary>
    /// Go to mission selection/briefing
    /// </summary>
    public void GoToBriefing()
    {
        Debug.Log("[Scene] Loading briefing scene");
        LoadScene(briefingSceneName);
    }
    
    /// <summary>
    /// Return to hangar from briefing without starting mission
    /// </summary>
    public void ReturnToHangar()
    {
        Debug.Log("[Scene] Returning to hangar");
        LoadScene(hangarSceneName);
    }
    
    private void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError($"[Scene] Scene name is null or empty!");
            return;
        }
        
        SceneManager.LoadScene(sceneName);
    }
    
    /// <summary>
    /// For debugging - restart current mission
    /// </summary>
    [ContextMenu("Restart Mission")]
    public void RestartMission()
    {
        LoadScene(missionSceneName);
    }
}