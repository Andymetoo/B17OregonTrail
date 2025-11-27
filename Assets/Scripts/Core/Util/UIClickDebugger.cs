using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIClickDebugger : MonoBehaviour
{
    [Header("Optional explicit refs (or will auto-find)")]
    public GraphicRaycaster raycaster;
    public EventSystem eventSystem;

    void Awake()
    {
        // Try assigned raycaster first, else auto-find one
        if (raycaster == null)
        {
            raycaster = GetComponent<GraphicRaycaster>();
            if (raycaster == null)
            {
                raycaster = FindObjectOfType<GraphicRaycaster>();
            }
        }

        if (raycaster == null)
        {
            Debug.LogWarning("[UIClickDebugger] No GraphicRaycaster found in scene.");
        }

        // Try assigned EventSystem first, else auto-find
        if (eventSystem == null)
        {
            eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                eventSystem = FindObjectOfType<EventSystem>();
            }
        }

        if (eventSystem == null)
        {
            Debug.LogWarning("[UIClickDebugger] No EventSystem found in scene.");
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            CheckUIUnderMouse();
        }
    }

    private void CheckUIUnderMouse()
    {
        if (raycaster == null || eventSystem == null)
        {
            Debug.LogWarning("[UIClickDebugger] Missing raycaster or eventSystem, cannot raycast.");
            return;
        }

        PointerEventData pointerData = new PointerEventData(eventSystem)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        raycaster.Raycast(pointerData, results);

        if (results.Count == 0)
        {
            Debug.Log("[UIClickDebugger] Clicked UI: NOTHING");
            return;
        }

        Debug.Log($"[UIClickDebugger] Clicked UI ({results.Count} hits):");
        foreach (var result in results)
        {
            Debug.Log(" → " + result.gameObject.name);
        }
    }
}
