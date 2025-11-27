using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple component for crew buttons that routes clicks to OrdersUIController.
/// Attach this to each crew button alongside CrewStatusIndicator.
/// </summary>
[RequireComponent(typeof(Button))]
public class CrewButton : MonoBehaviour
{
    [Header("Configuration")]
    public string crewId;
    
    private Button button;
    
    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnClicked);
    }
    
    private void OnClicked()
    {
        if (OrdersUIController.Instance != null)
        {
            OrdersUIController.Instance.OnCrewButtonClicked(crewId);
        }
    }
}
