using UnityEngine;

/// <summary>
/// Mouse-based gunner input for desktop/editor.
/// Uses mouse movement for looking and left click for firing.
/// </summary>
public class MouseGunnerInput : MonoBehaviour, IGunnerInput
{
    [Header("Mouse Settings")]
    [Tooltip("Mouse sensitivity multiplier.")]
    [SerializeField] private float mouseSensitivity = 2f;
    [Tooltip("Invert vertical look axis.")]
    [SerializeField] private bool invertY = false;
    [Tooltip("Fire button (default: Left Mouse Button).")]
    [SerializeField] private KeyCode fireButton = KeyCode.Mouse0;

    private Vector2 _lookDelta;

    private void Update()
    {
        // Only process input if in combat mode
        if (GameModeManager.Instance == null || GameModeManager.Instance.CurrentMode != GameMode.CombatMode)
        {
            _lookDelta = Vector2.zero;
            return;
        }

        // Get mouse delta
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        if (invertY) mouseY = -mouseY;

        _lookDelta = new Vector2(mouseX, mouseY);
    }

    public Vector2 GetLookDelta()
    {
        return _lookDelta;
    }

    public bool GetFirePressed()
    {
        return Input.GetKeyDown(fireButton);
    }

    public bool GetFireHeld()
    {
        return Input.GetKey(fireButton);
    }

    public float GetSensitivity()
    {
        return mouseSensitivity;
    }
}
