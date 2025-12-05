using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Button component to enter combat mode for a specific gunner station.
/// Attach to "Fire" or gunner position buttons in the main UI.
/// </summary>
[RequireComponent(typeof(Button))]
public class EnterCombatButton : MonoBehaviour
{
    [Header("Station Assignment")]
    [Tooltip("Which gunner station this button corresponds to.")]
    [SerializeField] private GunnerStation targetStation = GunnerStation.None;

    [Header("Crew Validation")]
    [Tooltip("If true, button is only enabled if crew member is healthy.")]
    [SerializeField] private bool requireHealthyCrew = true;

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnButtonClicked);
    }

    private void Update()
    {
        // Update button interactability based on crew status
        if (requireHealthyCrew && GameModeManager.Instance != null)
        {
            bool isAvailable = GameModeManager.Instance.IsStationAvailable(targetStation);
            _button.interactable = isAvailable;
        }
    }

    private void OnButtonClicked()
    {
        if (targetStation == GunnerStation.None)
        {
            Debug.LogWarning("[EnterCombatButton] Target station is None - cannot enter combat mode");
            return;
        }

        if (GameModeManager.Instance == null)
        {
            Debug.LogError("[EnterCombatButton] GameModeManager not found!");
            return;
        }

        // Check if station is available
        if (requireHealthyCrew && !GameModeManager.Instance.IsStationAvailable(targetStation))
        {
            Debug.Log($"[EnterCombatButton] Station {targetStation} is not available (crew incapacitated)");
            // Could show a popup here
            return;
        }

        // Enter combat mode at this station
        GameModeManager.Instance.EnterCombatMode(targetStation);
    }

    /// <summary>
    /// Set the target station programmatically.
    /// </summary>
    public void SetStation(GunnerStation station)
    {
        targetStation = station;
    }
}
