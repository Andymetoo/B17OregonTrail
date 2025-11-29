using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CrewPositionRegistry))]
public class CrewPositionRegistryEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        CrewPositionRegistry registry = (CrewPositionRegistry)target;
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Quick Setup Tools", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Create Station Marker GameObjects", GUILayout.Height(30)))
        {
            CreateStationMarkers(registry);
        }
        
        EditorGUILayout.HelpBox(
            "Click the button above to create empty GameObjects for each station.\n" +
            "Position them in the Scene view, and they'll automatically be assigned as position references!",
            MessageType.Info
        );
        
        EditorGUILayout.Space(5);
        
        if (GUILayout.Button("Refresh All Positions from Transforms"))
        {
            RefreshPositions(registry);
        }
    }
    
    private void CreateStationMarkers(CrewPositionRegistry registry)
    {
        // Find or create a parent container
        Transform container = registry.transform.Find("StationMarkers");
        if (container == null)
        {
            GameObject containerObj = new GameObject("StationMarkers");
            containerObj.transform.SetParent(registry.transform);
            containerObj.transform.localPosition = Vector3.zero;
            container = containerObj.transform;
        }
        
        // Create markers for each station that doesn't have a transform
        foreach (var entry in registry.stationPositions)
        {
            if (entry.positionTransform == null && !string.IsNullOrEmpty(entry.stationId))
            {
                // Create a new empty GameObject as a marker
                GameObject marker = new GameObject($"Station_{entry.stationId}");
                marker.transform.SetParent(container);
                
                // Add a RectTransform if parent is Canvas-based
                if (registry.GetComponent<RectTransform>() != null)
                {
                    RectTransform rt = marker.AddComponent<RectTransform>();
                    rt.anchoredPosition = entry.manualPosition;
                    entry.positionTransform = rt;
                }
                else
                {
                    marker.transform.localPosition = entry.manualPosition;
                }
                
                // Add a visual indicator (optional - comment out if you don't want sprites)
                var image = marker.AddComponent<UnityEngine.UI.Image>();
                image.color = new Color(0, 1, 1, 0.5f); // Cyan, semi-transparent
                image.raycastTarget = false;
                
                var rect = marker.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.sizeDelta = new Vector2(20, 20);
                    entry.positionTransform = rect;
                }
                
                Debug.Log($"Created station marker for '{entry.stationId}'");
            }
        }
        
        EditorUtility.SetDirty(registry);
        Debug.Log("Station markers created! Position them in the Scene view.");
    }
    
    private void RefreshPositions(CrewPositionRegistry registry)
    {
        int refreshed = 0;
        foreach (var entry in registry.stationPositions)
        {
            if (entry.positionTransform != null)
            {
                refreshed++;
            }
        }
        
        EditorUtility.SetDirty(registry);
        Debug.Log($"Refreshed {refreshed} station positions from transforms.");
    }
}
