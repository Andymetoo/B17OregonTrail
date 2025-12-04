using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple button component that opens the Plane Overview UI when clicked.
/// Attach this to any button in the UI.
/// </summary>
[RequireComponent(typeof(Button))]
public class OverviewButton : MonoBehaviour
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClicked);
        }
    }

    private void OnButtonClicked()
    {
        if (PlaneOverviewUI.Instance != null)
        {
            PlaneOverviewUI.Instance.OpenOverview();
        }
        else
        {
            Debug.LogWarning("[OverviewButton] PlaneOverviewUI.Instance is null. Make sure PlaneOverviewUI exists in the scene.");
        }
    }
}
