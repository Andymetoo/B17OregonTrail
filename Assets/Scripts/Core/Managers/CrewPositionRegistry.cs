using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Maps section IDs and station IDs to screen positions for crew movement.
/// Attach this to your UI root and assign section buttons/positions.
/// </summary>
public class CrewPositionRegistry : MonoBehaviour
{
    public static CrewPositionRegistry Instance { get; private set; }
    
    [Header("Section Positions")]
    [Tooltip("Map section IDs to their RectTransform (usually the section button)")]
    public List<SectionPositionEntry> sectionPositions = new List<SectionPositionEntry>();
    
    [Header("Station Positions")]
    [Tooltip("Map station IDs to crew home positions (use RectTransforms for easy positioning)")]
    public List<StationPositionEntry> stationPositions = new List<StationPositionEntry>();
    
    [Header("Visual Gizmos")]
    [Tooltip("Show station positions in Scene view as colored spheres")]
    public bool showGizmos = true;
    
    [Header("Main Fuselage Path")]
    [Tooltip("Y-coordinate of the main fuselage line for path snapping")]
    public float mainPathY = 0f;
    
    [Tooltip("Crew snap to main path when moving (for turret/cockpit positions above/below)")]
    public bool usePathSnapping = true;
    
    private Dictionary<string, RectTransform> sectionLookup = new Dictionary<string, RectTransform>();
    private Dictionary<string, StationPositionEntry> stationLookupEntries = new Dictionary<string, StationPositionEntry>();
    
    [System.Serializable]
    public class SectionPositionEntry
    {
        public string sectionId;
        public RectTransform positionTransform;
    }
    
    [System.Serializable]
    public class StationPositionEntry
    {
        public string stationId;
        [Tooltip("Use a RectTransform as visual marker for station position")]
        public RectTransform positionTransform;
        [Tooltip("Or manually set position if no transform assigned")]
        public Vector2 manualPosition;
        
        public Vector2 GetPosition()
        {
            if (positionTransform != null)
                return positionTransform.anchoredPosition;
            return manualPosition;
        }
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
        foreach (var entry in sectionPositions)
        {
            if (!string.IsNullOrEmpty(entry.sectionId) && entry.positionTransform != null)
            {
                sectionLookup[entry.sectionId] = entry.positionTransform;
            }
        }
        
        foreach (var entry in stationPositions)
        {
            if (!string.IsNullOrEmpty(entry.stationId))
            {
                stationLookupEntries[entry.stationId] = entry;
                
                Vector2 pos = entry.GetPosition();
                string source = entry.positionTransform != null ? 
                    $"from Transform '{entry.positionTransform.name}' at anchored pos {entry.positionTransform.anchoredPosition}" : 
                    $"from manual position";
                Debug.Log($"[CrewPositionRegistry] Station '{entry.stationId}' registered: will query {source}");
                
                if (entry.positionTransform != null)
                {
                    Debug.Log($"[CrewPositionRegistry] Station '{entry.stationId}' parent: {entry.positionTransform.parent.name}, anchors: min={entry.positionTransform.anchorMin}, max={entry.positionTransform.anchorMax}");
                }
            }
        }
        
        Debug.Log($"[CrewPositionRegistry] Initialized with {sectionLookup.Count} sections and {stationLookupEntries.Count} stations");
    }
    
    /// <summary>
    /// Get the screen position for a section (center of section button)
    /// </summary>
    public Vector2 GetSectionPosition(string sectionId)
    {
        if (sectionLookup.TryGetValue(sectionId, out var rectTransform))
        {
            Debug.Log($"[CrewPositionRegistry] Section '{sectionId}' parent: {rectTransform.parent.name}, anchors: min={rectTransform.anchorMin}, max={rectTransform.anchorMax}, anchoredPos={rectTransform.anchoredPosition}");
            return rectTransform.anchoredPosition;
        }
        
        Debug.LogWarning($"[CrewPositionRegistry] Section '{sectionId}' not found in registry!");
        return Vector2.zero;
    }
    
    /// <summary>
    /// Get the home position for a station
    /// </summary>
    public Vector2 GetStationPosition(string stationId)
    {
        if (stationLookupEntries.TryGetValue(stationId, out var entry))
        {
            Vector2 pos = entry.GetPosition();
            Debug.Log($"[CrewPositionRegistry] GetStationPosition('{stationId}') returning {pos}");
            return pos;
        }
        
        Debug.LogWarning($"[CrewPositionRegistry] Station '{stationId}' not found in registry!");
        return Vector2.zero;
    }
    
    /// <summary>
    /// Snap a position to the main fuselage path (for movement)
    /// </summary>
    public Vector2 SnapToMainPath(Vector2 position)
    {
        if (usePathSnapping)
        {
            return new Vector2(position.x, mainPathY);
        }
        return position;
    }
    
    /// <summary>
    /// Register a section position at runtime (if not set in inspector)
    /// </summary>
    public void RegisterSection(string sectionId, RectTransform rectTransform)
    {
        sectionLookup[sectionId] = rectTransform;
    }
    
    /// <summary>
    /// Register a station position at runtime
    /// </summary>
    public void RegisterStation(string stationId, Vector2 position)
    {
        var entry = new StationPositionEntry { stationId = stationId, manualPosition = position };
        stationLookupEntries[stationId] = entry;
    }
    
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        // Draw section positions in green
        foreach (var entry in sectionPositions)
        {
            if (entry.positionTransform != null)
            {
                Gizmos.color = Color.green;
                Vector3 worldPos = entry.positionTransform.position;
                Gizmos.DrawWireSphere(worldPos, 15f);
                UnityEditor.Handles.Label(worldPos + Vector3.up * 20f, entry.sectionId);
            }
        }
        
        // Draw station positions in cyan
        foreach (var entry in stationPositions)
        {
            Vector2 pos = entry.GetPosition();
            if (entry.positionTransform != null)
            {
                Gizmos.color = Color.cyan;
                Vector3 worldPos = entry.positionTransform.position;
                
                // Offset to bottom center of sprite (assuming pivot is at center)
                float halfHeight = entry.positionTransform.rect.height * 0.5f;
                Vector3 bottomCenterPos = worldPos + Vector3.down * halfHeight;
                
                Gizmos.DrawWireSphere(bottomCenterPos, 10f);
                UnityEditor.Handles.Label(bottomCenterPos + Vector3.up * 20f, entry.stationId);
            }
        }
        
        // Draw main path line in blue
        if (usePathSnapping)
        {
            Gizmos.color = new Color(0.3f, 0.5f, 1f, 0.8f);
            // Draw a horizontal line at mainPathY (in world space this is approximate)
            Vector3 leftPoint = new Vector3(-500, mainPathY, 0);
            Vector3 rightPoint = new Vector3(500, mainPathY, 0);
            Gizmos.DrawLine(leftPoint, rightPoint);
        }
    }
}
