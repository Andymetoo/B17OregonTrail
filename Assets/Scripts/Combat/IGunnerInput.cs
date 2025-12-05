using UnityEngine;

/// <summary>
/// Interface for gunner input - allows swapping between mouse and touch controls.
/// </summary>
public interface IGunnerInput
{
    /// <summary>
    /// Get the look delta (how much to rotate camera this frame).
    /// Returns Vector2: (horizontal delta, vertical delta) in degrees or pixels.
    /// </summary>
    Vector2 GetLookDelta();

    /// <summary>
    /// Check if fire button is pressed this frame.
    /// </summary>
    bool GetFirePressed();

    /// <summary>
    /// Check if fire button is held down.
    /// </summary>
    bool GetFireHeld();

    /// <summary>
    /// Optional: Get sensitivity multiplier for fine-tuning.
    /// </summary>
    float GetSensitivity();
}
