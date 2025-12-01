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
    
    [Header("Coordinate Space")]
    [Tooltip("All returned positions will be converted into this parent's local space (usually the parent of crew sprites). If null, raw anchoredPosition is returned and ALL transforms must share the same parent.")]
    public RectTransform referenceParent;
    
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
    /// Get ordered section IDs as defined in the inspector list.
    /// The order defines adjacency: neighbors are adjacent; first/last are ends.
    /// </summary>
    public List<string> GetOrderedSectionIds()
    {
        var ids = new List<string>();
        foreach (var entry in sectionPositions)
        {
            if (!string.IsNullOrEmpty(entry.id)) ids.Add(entry.id);
        }
        return ids;
    }

    public int GetSectionIndex(string sectionId)
    {
        for (int i = 0; i < sectionPositions.Count; i++)
        {
            if (sectionPositions[i].id == sectionId) return i;
        }
        return -1;
    }

    /// <summary>
    /// Find nearest section by X coordinate to a given reference-parent-space position.
    /// Uses referenceParent to convert section RectTransforms before comparison.
    /// </summary>
    public string GetNearestSectionIdByPosition(Vector2 referenceSpacePos)
    {
        float bestDist = float.MaxValue;
        string bestId = null;
        foreach (var entry in sectionPositions)
        {
            if (entry.transform == null || string.IsNullOrEmpty(entry.id)) continue;
            var p = ToReferenceSpace(entry.transform);
            float d = Mathf.Abs(p.x - referenceSpacePos.x) + Mathf.Abs(p.y - referenceSpacePos.y);
            if (d < bestDist)
            {
                bestDist = d;
                bestId = entry.id;
            }
        }
        return bestId;
    }

    /// <summary>
    /// Build waypoint positions from start section to end section, inclusive, in inspector-defined order.
    /// Converts each to referenceParent space.
    /// </summary>
    public List<Vector2> GetSectionPathPositionsBetween(string startSectionId, string endSectionId)
    {
        var waypoints = new List<Vector2>();
        int start = GetSectionIndex(startSectionId);
        int end = GetSectionIndex(endSectionId);
        if (start == -1 || end == -1) return waypoints;
        if (start <= end)
        {
            for (int i = start; i <= end; i++)
            {
                var rt = sectionPositions[i].transform;
                if (rt != null) waypoints.Add(ToReferenceSpace(rt));
            }
        }
        else
        {
            for (int i = start; i >= end; i--)
            {
                var rt = sectionPositions[i].transform;
                if (rt != null) waypoints.Add(ToReferenceSpace(rt));
            }
        }
        return waypoints;
    }
    
    /// <summary>
    /// Get the position where a crew member should stand when working on a section
    /// </summary>
    public Vector2 GetSectionPosition(string sectionId)
    {
        if (sectionLookup.TryGetValue(sectionId, out var rectTransform) && rectTransform != null)
        {
            return ToReferenceSpace(rectTransform);
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
            return ToReferenceSpace(rectTransform);
        }
        
        Debug.LogWarning($"[CrewPositionRegistry] Station '{stationId}' not found!");
        return Vector2.zero;
    }

    /// <summary>
    /// Get the RectTransform for a section by id (for diagnostics).
    /// </summary>
    public RectTransform GetSectionRect(string sectionId)
    {
        sectionLookup.TryGetValue(sectionId, out var rectTransform);
        return rectTransform;
    }

    /// <summary>
    /// Get the RectTransform for a station by id (for diagnostics).
    /// </summary>
    public RectTransform GetStationRect(string stationId)
    {
        stationLookup.TryGetValue(stationId, out var rectTransform);
        return rectTransform;
    }

    private Vector2 ToReferenceSpace(RectTransform source)
    {
        if (source == null) return Vector2.zero;
        if (referenceParent == null)
        {
            // Fallback: raw anchoredPosition requires same-parent setup
            return source.anchoredPosition;
        }
        // Convert the source world position to reference parent's local space
        Vector3 world = source.position; // pivot world position
        Vector3 local = referenceParent.InverseTransformPoint(world);
        return new Vector2(local.x, local.y);
    }
}
