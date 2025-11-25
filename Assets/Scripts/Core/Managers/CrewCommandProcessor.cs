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
        while (_commandQueue.Count > 0)
        {
            var cmd = _commandQueue.Dequeue();
            cmd.Execute(CrewManager.Instance, PlaneManager.Instance);
        }
    }
}
