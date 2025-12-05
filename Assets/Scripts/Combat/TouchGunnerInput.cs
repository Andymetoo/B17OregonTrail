using UnityEngine;

/// <summary>
/// Touch-based gunner input for mobile devices.
/// Uses touch drag for looking and tap for firing.
/// </summary>
public class TouchGunnerInput : MonoBehaviour, IGunnerInput
{
    [Header("Touch Settings")]
    [Tooltip("Touch sensitivity multiplier.")]
    [SerializeField] private float touchSensitivity = 0.5f;
    [Tooltip("Invert vertical look axis.")]
    [SerializeField] private bool invertY = false;
    
    [Header("Fire Zone")]
    [Tooltip("Fire button UI element (optional - if null, uses right 30% of screen).")]
    [SerializeField] private RectTransform fireButtonZone;
    [Tooltip("Percentage of screen width reserved for fire button (if no UI element assigned).")]
    [Range(0f, 0.5f)]
    [SerializeField] private float fireZoneWidthPercent = 0.3f;

    private Vector2 _lookDelta;
    private bool _firePressed;
    private bool _fireHeld;
    private int _lookTouchId = -1;

    private void Update()
    {
        // Only process input if in combat mode
        if (GameModeManager.Instance == null || GameModeManager.Instance.CurrentMode != GameMode.CombatMode)
        {
            ResetInput();
            return;
        }

        ProcessTouchInput();
    }

    private void ProcessTouchInput()
    {
        _lookDelta = Vector2.zero;
        _firePressed = false;

        if (Input.touchCount == 0)
        {
            _fireHeld = false;
            _lookTouchId = -1;
            return;
        }

        bool fireHeldThisFrame = false;

        foreach (Touch touch in Input.touches)
        {
            bool isInFireZone = IsInFireZone(touch.position);

            if (isInFireZone)
            {
                // Fire zone touch
                if (touch.phase == TouchPhase.Began)
                {
                    _firePressed = true;
                    fireHeldThisFrame = true;
                }
                else if (touch.phase == TouchPhase.Stationary || touch.phase == TouchPhase.Moved)
                {
                    fireHeldThisFrame = true;
                }
            }
            else
            {
                // Look zone touch
                if (touch.phase == TouchPhase.Began)
                {
                    _lookTouchId = touch.fingerId;
                }
                else if (touch.phase == TouchPhase.Moved && touch.fingerId == _lookTouchId)
                {
                    float deltaX = touch.deltaPosition.x;
                    float deltaY = touch.deltaPosition.y;

                    if (invertY) deltaY = -deltaY;

                    _lookDelta = new Vector2(deltaX, deltaY);
                }
                else if (touch.phase == TouchPhase.Ended && touch.fingerId == _lookTouchId)
                {
                    _lookTouchId = -1;
                }
            }
        }

        _fireHeld = fireHeldThisFrame;
    }

    private bool IsInFireZone(Vector2 touchPosition)
    {
        // If we have a UI button zone, use that
        if (fireButtonZone != null)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(fireButtonZone, touchPosition);
        }

        // Otherwise, use right side of screen
        float fireZoneStartX = Screen.width * (1f - fireZoneWidthPercent);
        return touchPosition.x >= fireZoneStartX;
    }

    private void ResetInput()
    {
        _lookDelta = Vector2.zero;
        _firePressed = false;
        _fireHeld = false;
        _lookTouchId = -1;
    }

    public Vector2 GetLookDelta()
    {
        return _lookDelta;
    }

    public bool GetFirePressed()
    {
        return _firePressed;
    }

    public bool GetFireHeld()
    {
        return _fireHeld;
    }

    public float GetSensitivity()
    {
        return touchSensitivity;
    }
}
