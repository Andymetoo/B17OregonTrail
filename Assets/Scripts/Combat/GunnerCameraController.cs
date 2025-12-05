using UnityEngine;

/// <summary>
/// Controls first-person gunner camera rotation using input from IGunnerInput.
/// Applies look constraints and handles firing.
/// </summary>
public class GunnerCameraController : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Active input handler (mouse or touch).")]
    [SerializeField] private MonoBehaviour inputHandler;

    [Header("Look Settings")]
    [Tooltip("Horizontal look speed multiplier.")]
    [SerializeField] private float horizontalSpeed = 2f;
    [Tooltip("Vertical look speed multiplier.")]
    [SerializeField] private float verticalSpeed = 2f;
    [Tooltip("Smooth camera rotation.")]
    [SerializeField] private bool smoothRotation = true;
    [Tooltip("Rotation smoothing factor (lower = smoother).")]
    [SerializeField] private float smoothFactor = 10f;

    private IGunnerInput _input;
    private Camera _combatCamera;
    private float _currentHorizontalAngle = 0f;
    private float _currentVerticalAngle = 0f;
    private float _targetHorizontalAngle = 0f;
    private float _targetVerticalAngle = 0f;

    private void Start()
    {
        // Get input interface
        if (inputHandler != null)
        {
            _input = inputHandler as IGunnerInput;
            if (_input == null)
            {
                Debug.LogError("[GunnerCameraController] Input handler does not implement IGunnerInput!");
            }
        }

        // Find combat camera
        _combatCamera = GetComponent<Camera>();
        if (_combatCamera == null)
        {
            Debug.LogError("[GunnerCameraController] No camera found on this GameObject!");
        }
    }

    private void Update()
    {
        // Only process in combat mode
        if (GameModeManager.Instance == null || GameModeManager.Instance.CurrentMode != GameMode.CombatMode)
        {
            return;
        }

        ProcessLookInput();
        ProcessFireInput();
    }

    private void ProcessLookInput()
    {
        if (_input == null) return;

        Vector2 lookDelta = _input.GetLookDelta();
        float sensitivity = _input.GetSensitivity();

        // Apply sensitivity and speed multipliers
        float horizontalDelta = lookDelta.x * sensitivity * horizontalSpeed;
        float verticalDelta = lookDelta.y * sensitivity * verticalSpeed;

        // Update target angles
        _targetHorizontalAngle += horizontalDelta;
        _targetVerticalAngle -= verticalDelta; // Invert for natural camera movement

        // Apply look limits from station controller
        if (GunnerStationController.Instance != null)
        {
            var limits = GunnerStationController.Instance.GetLookLimits();
            _targetHorizontalAngle = Mathf.Clamp(_targetHorizontalAngle, -limits.horizontal, limits.horizontal);
            _targetVerticalAngle = Mathf.Clamp(_targetVerticalAngle, -limits.vertical, limits.vertical);
        }

        // Apply rotation (smooth or instant)
        if (smoothRotation)
        {
            _currentHorizontalAngle = Mathf.Lerp(_currentHorizontalAngle, _targetHorizontalAngle, Time.deltaTime * smoothFactor);
            _currentVerticalAngle = Mathf.Lerp(_currentVerticalAngle, _targetVerticalAngle, Time.deltaTime * smoothFactor);
        }
        else
        {
            _currentHorizontalAngle = _targetHorizontalAngle;
            _currentVerticalAngle = _targetVerticalAngle;
        }

        // Apply to camera
        if (_combatCamera != null)
        {
            // Get base rotation from station
            Quaternion baseRotation = Quaternion.identity;
            if (GunnerStationController.Instance != null)
            {
                baseRotation = transform.parent != null ? transform.parent.rotation : Quaternion.identity;
            }

            // Apply look angles relative to base
            Quaternion lookRotation = Quaternion.Euler(_currentVerticalAngle, _currentHorizontalAngle, 0f);
            transform.localRotation = lookRotation;
        }
    }

    private void ProcessFireInput()
    {
        if (_input == null) return;

        if (_input.GetFirePressed())
        {
            Fire();
        }
    }

    private void Fire()
    {
        // TODO: Implement firing logic (raycast, spawn projectile, etc.)
        Debug.Log("[GunnerCameraController] FIRE!");
        
        // Placeholder: Flash muzzle, play sound, check for hits
        // Will implement in next phase
    }

    /// <summary>
    /// Reset camera angles when entering a new station.
    /// </summary>
    public void ResetLookAngles()
    {
        _currentHorizontalAngle = 0f;
        _currentVerticalAngle = 0f;
        _targetHorizontalAngle = 0f;
        _targetVerticalAngle = 0f;
    }

    /// <summary>
    /// Set the active input handler at runtime.
    /// </summary>
    public void SetInputHandler(MonoBehaviour handler)
    {
        inputHandler = handler;
        _input = handler as IGunnerInput;
        
        if (_input == null)
        {
            Debug.LogError("[GunnerCameraController] Provided handler does not implement IGunnerInput!");
        }
    }
}
