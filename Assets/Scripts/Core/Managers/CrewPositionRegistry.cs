using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple registry mapping IDs to RectTransform positions.
/// - Station IDs -> where crew idle at home
/// - Section IDs -> where crew go to perform actions (repair/extinguish)
/// All transforms MUST use center anchors (0.5, 0.5) and share the same parent.
/// </summary>
public class CrewPositionRegistry : MonoBehaviour
{
    public static CrewPositionRegistry Instance { get; private set; }
    
    [Header("Crew Home Positions (Stations)")]
    [Tooltip("Map each crew's station ID to their home idle position")]
    public List<PositionEntry> stationPositions = new List<PositionEntry>();
    
    [Header("Action Target Positions (Sections)")]
    [Tooltip("Map each section ID to where crew should stand when repairing/extinguishing")]
    public List<PositionEntry> sectionPositions = new List<PositionEntry>();
    
    private Dictionary<string, RectTransform> stationLookup = new Dictionary<string, RectTransform>();
    private Dictionary<string, RectTransform> sectionLookup = new Dictionary<string, RectTransform>();
    
    [System.Serializable]
    public class PositionEntry
    {
        public string id;
        public RectTransform transform;
    }
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // Build lookup dictionaries
        foreach (var entry in stationPositions)
        {
            if (!string.IsNullOrEmpty(entry.id) && entry.transform != null)
            {
                stationLookup[entry.id] = entry.transform;
            }
        }
        
        foreach (var entry in sectionPositions)
        {
            if (!string.IsNullOrEmpty(entry.id) && entry.transform != null)
            {
                sectionLookup[entry.id] = entry.transform;
            }
        }
    }
    
    /// <summary>
    /// Get the position where a crew member should stand when working on a section
    /// </summary>
    public Vector2 GetSectionPosition(string sectionId)
    {
        if (sectionLookup.TryGetValue(sectionId, out var rectTransform) && rectTransform != null)
        {
            return rectTransform.anchoredPosition;
        }
        
        Debug.LogWarning($"[CrewPositionRegistry] Section '{sectionId}' not found!");
        return Vector2.zero;
    }
    
    /// <summary>
    /// Get the home position for a crew member's station
    /// </summary>
    public Vector2 GetStationPosition(string stationId)
    {
        if (stationLookup.TryGetValue(stationId, out var rectTransform) && rectTransform != null)
        {
            return rectTransform.anchoredPosition;
        }
        
        Debug.LogWarning($"[CrewPositionRegistry] Station '{stationId}' not found!");
        return Vector2.zero;
    }
}
