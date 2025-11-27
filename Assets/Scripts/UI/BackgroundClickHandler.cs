using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Attach this to a full-screen invisible image (behind all UI) to detect background clicks.
/// Cancels pending actions when the player clicks on empty space.
/// </summary>
public class BackgroundClickHandler : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        // Cancel any pending action when clicking on background
        if (OrdersUIController.Instance != null)
        {
            OrdersUIController.Instance.CancelPendingAction();
        }
    }
}
