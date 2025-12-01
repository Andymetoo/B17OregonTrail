using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central place for processing crew-related commands.
/// UI should talk to THIS, not directly to CrewManager/PlaneManager,
/// so we keep a clean separation of concerns.
/// </summary>
public class CrewCommandProcessor : MonoBehaviour
{
    public static CrewCommandProcessor Instance { get; private set; }

    private readonly Queue<CrewCommand> _commandQueue = new Queue<CrewCommand>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// Enqueue a crew command to be executed on the next simulation tick.
    /// </summary>
    public void Enqueue(CrewCommand command)
    {
        if (command == null) return;
        _commandQueue.Enqueue(command);
    }

    /// <summary>
    /// Called once per simulation tick by GameStateManager.
    /// Processes all queued commands.
    /// </summary>
    public void Tick(float deltaTime)
    {
        if (CrewManager.Instance == null || PlaneManager.Instance == null)
            return;

        // For now, process all queued commands immediately.
        // If you want, you could limit how many are processed per tick.
        int processed = 0;
        int queued = _commandQueue.Count;
        if (CrewManager.Instance.verboseLogging && queued > 0)
        {
            Debug.Log($"[CmdProc] Tick start queued={queued}");
        }
        while (_commandQueue.Count > 0)
        {
            var cmd = _commandQueue.Dequeue();
            // Validate issuing crew is healthy before executing
            var issuingCrew = CrewManager.Instance.GetCrewById(cmd.CrewId);
            if (issuingCrew == null)
            {
                Debug.LogWarning("[CmdProc] Issuing crew not found; skipping command.");
                continue;
            }

            if (issuingCrew.Status != CrewStatus.Healthy)
            {
                Debug.Log($"[CmdProc] {issuingCrew.Name} is not healthy; command skipped.");
                continue;
            }

            cmd.Execute(CrewManager.Instance, PlaneManager.Instance);
            processed++;
            if (CrewManager.Instance.verboseLogging)
            {
                Debug.Log($"[CmdProc] Executed command type={cmd.GetType().Name} crew={cmd.CrewId}");
            }
        }
        if (CrewManager.Instance.verboseLogging && processed > 0)
        {
            Debug.Log($"[CmdProc] Tick processed={processed}");
        }
    }
}
