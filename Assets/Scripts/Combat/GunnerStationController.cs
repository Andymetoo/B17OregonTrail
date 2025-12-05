using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages gunner station positions and camera placement for first-person combat view.
/// Each station has a Transform that defines camera position and rotation.
/// </summary>
public class GunnerStationController : MonoBehaviour
{
    public static GunnerStationController Instance { get; private set; }

    [Header("Station Transforms")]
    [Tooltip("Camera position/rotation for Top Turret gunner.")]
    [SerializeField] private Transform topTurretStation;
    [Tooltip("Camera position/rotation for Ball Turret gunner.")]
    [SerializeField] private Transform ballTurretStation;
    [Tooltip("Camera position/rotation for Left Waist gunner.")]
    [SerializeField] private Transform leftWaistStation;
    [Tooltip("Camera position/rotation for Right Waist gunner.")]
    [SerializeField] private Transform rightWaistStation;
    [Tooltip("Camera position/rotation for Tail gunner.")]
    [SerializeField] private Transform tailGunnerStation;
    [Tooltip("Camera position/rotation for Nose/Bombardier gunner.")]
    [SerializeField] private Transform noseStation;

    [Header("Camera Settings")]
    [Tooltip("Reference to the combat camera (assigned by GameModeManager or manually).")]
    [SerializeField] private Camera combatCamera;
    [Tooltip("Speed of camera transitions between stations (units per second).")]
    [SerializeField] private float transitionSpeed = 5f;
    [Tooltip("Use smooth interpolation for station transitions.")]
    [SerializeField] private bool smoothTransitions = true;

    [Header("Look Constraints")]
    [Tooltip("Horizontal look angle limit (degrees from center).")]
    [SerializeField] private float horizontalLookLimit = 60f;
    [Tooltip("Vertical look angle limit (degrees from center).")]
    [SerializeField] private float verticalLookLimit = 45f;

    private Transform _currentStationTransform;
    private bool _isTransitioning = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Auto-assign combat camera if not set
        if (combatCamera == null && GameModeManager.Instance != null)
        {
            // Try to find it from GameModeManager (we'll add this reference later)
            combatCamera = Camera.main; // Fallback
        }
    }

    /// <summary>
    /// Move the combat camera to the specified gunner station.
    /// </summary>
    public void MoveToStation(GunnerStation station)
    {
        Transform stationTransform = GetStationTransform(station);
        
        if (stationTransform == null)
        {
            Debug.LogWarning($"[GunnerStationController] No transform assigned for station: {station}");
            return;
        }

        _currentStationTransform = stationTransform;

        if (combatCamera == null)
        {
            Debug.LogError("[GunnerStationController] Combat camera not assigned!");
            return;
        }

        if (smoothTransitions)
        {
            // Start smooth transition coroutine
            StopAllCoroutines();
            StartCoroutine(SmoothTransitionToStation(stationTransform));
        }
        else
        {
            // Instant snap
            SnapCameraToStation(stationTransform);
        }
    }

    private void SnapCameraToStation(Transform station)
    {
        combatCamera.transform.position = station.position;
        combatCamera.transform.rotation = station.rotation;
    }

    private System.Collections.IEnumerator SmoothTransitionToStation(Transform targetStation)
    {
        _isTransitioning = true;

        Transform camTransform = combatCamera.transform;
        Vector3 startPos = camTransform.position;
        Quaternion startRot = camTransform.rotation;

        float elapsed = 0f;
        float duration = Vector3.Distance(startPos, targetStation.position) / transitionSpeed;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            camTransform.position = Vector3.Lerp(startPos, targetStation.position, t);
            camTransform.rotation = Quaternion.Slerp(startRot, targetStation.rotation, t);

            yield return null;
        }

        // Snap to final position
        camTransform.position = targetStation.position;
        camTransform.rotation = targetStation.rotation;

        _isTransitioning = false;
    }

    /// <summary>
    /// Get the Transform for a given gunner station.
    /// </summary>
    private Transform GetStationTransform(GunnerStation station)
    {
        switch (station)
        {
            case GunnerStation.TopTurret: return topTurretStation;
            case GunnerStation.BallTurret: return ballTurretStation;
            case GunnerStation.LeftWaist: return leftWaistStation;
            case GunnerStation.RightWaist: return rightWaistStation;
            case GunnerStation.TailGunner: return tailGunnerStation;
            case GunnerStation.Nose: return noseStation;
            default: return null;
        }
    }

    /// <summary>
    /// Check if camera is currently transitioning between stations.
    /// </summary>
    public bool IsTransitioning => _isTransitioning;

    /// <summary>
    /// Get look angle constraints for the current station.
    /// Returns (horizontalLimit, verticalLimit) in degrees.
    /// </summary>
    public (float horizontal, float vertical) GetLookLimits()
    {
        // Can be customized per station later
        return (horizontalLookLimit, verticalLookLimit);
    }

    /// <summary>
    /// Set the combat camera reference (called by GameModeManager or manually).
    /// </summary>
    public void SetCombatCamera(Camera camera)
    {
        combatCamera = camera;
    }

    /// <summary>
    /// Get all available stations based on crew status.
    /// </summary>
    public List<GunnerStation> GetAvailableStations()
    {
        var available = new List<GunnerStation>();

        foreach (GunnerStation station in System.Enum.GetValues(typeof(GunnerStation)))
        {
            if (station == GunnerStation.None) continue;
            
            if (GameModeManager.Instance != null && GameModeManager.Instance.IsStationAvailable(station))
            {
                available.Add(station);
            }
        }

        return available;
    }

#if UNITY_EDITOR
    /// <summary>
    /// Visualize station positions in the Scene view.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!enabled) return;

        DrawStationGizmo(topTurretStation, Color.red, "Top Turret");
        DrawStationGizmo(ballTurretStation, Color.blue, "Ball Turret");
        DrawStationGizmo(leftWaistStation, Color.green, "Left Waist");
        DrawStationGizmo(rightWaistStation, Color.yellow, "Right Waist");
        DrawStationGizmo(tailGunnerStation, Color.cyan, "Tail Gunner");
        DrawStationGizmo(noseStation, Color.magenta, "Nose");
    }

    private void DrawStationGizmo(Transform station, Color color, string label)
    {
        if (station == null) return;

        Gizmos.color = color;
        Gizmos.DrawWireSphere(station.position, 0.3f);
        Gizmos.DrawRay(station.position, station.forward * 1.5f);

        #if UNITY_EDITOR
        UnityEditor.Handles.Label(station.position + Vector3.up * 0.5f, label);
        #endif
    }
#endif
}
