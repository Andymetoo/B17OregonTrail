using UnityEngine;
using UnityEngine.EventSystems;

public class TargetClick : MonoBehaviour, IPointerClickHandler
{
    public string targetId;

    public void OnPointerClick(PointerEventData eventData)
    {
        OrdersUIController.Instance?.OnTargetClicked(targetId);
    }
}
