using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages all crew stations on the B-17.
/// Tracks which stations are occupied, handles crew assignments, and determines station availability.
/// </summary>
public class StationManager : MonoBehaviour
{
    public static StationManager Instance { get; private set; }

    [Header("Station Definitions")]
    [Tooltip("All stations on the plane. Configure station IDs, sections, and initial state.")]
    public List<StationState> AllStations = new List<StationState>();

    // Events
    public event Action<StationState, string> OnStationOccupied;  // station, crewId
    public event Action<StationState> OnStationVacated;           // station

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        InitializeStations();
    }

    /// <summary>
    /// Initialize all stations with default configuration if not already set up.
    /// </summary>
    private void InitializeStations()
    {
        // If stations are already configured in Inspector, skip auto-setup
        if (AllStations != null && AllStations.Count > 0)
        {
            Debug.Log($"[StationManager] Using {AllStations.Count} stations from Inspector configuration.");
            return;
        }

        // Auto-create default station list for B-17F
        AllStations = new List<StationState>
        {
            // Combat Stations (turrets)
            new StationState { Type = StationType.TopTurret, StationId = "TopTurret", SectionId = "Cockpit", IsOperational = true },
            new StationState { Type = StationType.BallTurret, StationId = "BallTurret", SectionId = "Fuselage", IsOperational = true },
            new StationState { Type = StationType.LeftWaistGun, StationId = "LeftWaistGun", SectionId = "Fuselage", IsOperational = true },
            new StationState { Type = StationType.RightWaistGun, StationId = "RightWaistGun", SectionId = "Fuselage", IsOperational = true },
            new StationState { Type = StationType.TailGun, StationId = "TailGun", SectionId = "Tail", IsOperational = true },
            
            // Flight Deck
            new StationState { Type = StationType.Pilot, StationId = "Pilot", SectionId = "Cockpit", IsOperational = true },
            new StationState { Type = StationType.CoPilot, StationId = "CoPilot", SectionId = "Cockpit", IsOperational = true },
            
            // Specialized (Navigator & Bombardier each operate a nose gun in B-17F)
            new StationState { Type = StationType.Navigator, StationId = "Navigator", SectionId = "Nose", IsOperational = true },
            new StationState { Type = StationType.RadioOperator, StationId = "RadioOperator", SectionId = "Fuselage", IsOperational = true },
            new StationState { Type = StationType.Bombardier, StationId = "Bombardier", SectionId = "Nose", IsOperational = true }
        };

        Debug.Log($"[StationManager] Initialized {AllStations.Count} default stations.");
    }

    /// <summary>
    /// Assign crew to their default stations at mission start.
    /// </summary>
    public void AssignDefaultStations()
    {
        if (CrewManager.Instance == null) return;

        foreach (var crew in CrewManager.Instance.AllCrew)
        {
            if (crew.DefaultStation != StationType.None)
            {
                AssignCrewToStation(crew.Id, crew.DefaultStation);
            }
        }

        Debug.Log("[StationManager] Assigned crew to default stations.");
    }

    /// <summary>
    /// Assign a crew member to a specific station.
    /// </summary>
    public bool AssignCrewToStation(string crewId, StationType stationType)
    {
        var station = GetStation(stationType);
        if (station == null)
        {
            Debug.LogWarning($"[StationManager] Station type {stationType} not found.");
            return false;
        }

        return AssignCrewToStation(crewId, station);
    }

    /// <summary>
    /// Assign a crew member to a specific station state.
    /// </summary>
    public bool AssignCrewToStation(string crewId, StationState station)
    {
        if (station == null || !station.IsOperational)
        {
            Debug.LogWarning($"[StationManager] Cannot assign to null or inoperable station.");
            return false;
        }

        if (station.IsOccupied)
        {
            Debug.LogWarning($"[StationManager] Station {station.StationId} is already occupied by {station.OccupiedByCrewId}.");
            return false;
        }

        var crew = CrewManager.Instance?.GetCrewById(crewId);
        if (crew == null)
        {
            Debug.LogWarning($"[StationManager] Crew {crewId} not found.");
            return false;
        }

        // Vacate previous station if occupied
        if (crew.CurrentStation != StationType.None)
        {
            VacateStation(crew.CurrentStation, crewId);
        }

        // Assign to new station
        station.OccupiedByCrewId = crewId;
        crew.CurrentStation = station.Type;

        OnStationOccupied?.Invoke(station, crewId);
        Debug.Log($"[StationManager] {crew.Name} assigned to {station.StationId}.");

        return true;
    }

    /// <summary>
    /// Remove crew from a station (due to death, injury, or reassignment).
    /// </summary>
    public bool VacateStation(StationType stationType, string crewId = null)
    {
        var station = GetStation(stationType);
        if (station == null) return false;

        // If crewId specified, verify it matches
        if (!string.IsNullOrEmpty(crewId) && station.OccupiedByCrewId != crewId)
        {
            Debug.LogWarning($"[StationManager] Crew {crewId} is not occupying {station.StationId}.");
            return false;
        }

        var crew = CrewManager.Instance?.GetCrewById(station.OccupiedByCrewId);
        if (crew != null)
        {
            crew.CurrentStation = StationType.None;
        }

        station.OccupiedByCrewId = null;
        OnStationVacated?.Invoke(station);
        Debug.Log($"[StationManager] {station.StationId} vacated.");

        return true;
    }

    /// <summary>
    /// Get station by type.
    /// </summary>
    public StationState GetStation(StationType type)
    {
        return AllStations?.FirstOrDefault(s => s.Type == type);
    }

    /// <summary>
    /// Get station by ID string.
    /// </summary>
    public StationState GetStationById(string stationId)
    {
        return AllStations?.FirstOrDefault(s => s.StationId == stationId);
    }

    /// <summary>
    /// Get the crew member currently occupying a station.
    /// </summary>
    public CrewMember GetStationOccupant(StationType type)
    {
        var station = GetStation(type);
        if (station == null || !station.IsOccupied) return null;

        return CrewManager.Instance?.GetCrewById(station.OccupiedByCrewId);
    }

    /// <summary>
    /// Check if a station is available for occupation (operational and unmanned).
    /// </summary>
    public bool IsStationAvailable(StationType type)
    {
        var station = GetStation(type);
        return station != null && station.IsAvailable;
    }

    /// <summary>
    /// Get all unmanned stations.
    /// </summary>
    public List<StationState> GetUnmannedStations()
    {
        return AllStations?.Where(s => s.IsOperational && !s.IsOccupied).ToList() ?? new List<StationState>();
    }

    /// <summary>
    /// Get all manned combat stations (guns).
    /// </summary>
    public List<StationState> GetMannedGunStations()
    {
        return AllStations?.Where(s => s.IsOccupied && IsGunStation(s.Type)).ToList() ?? new List<StationState>();
    }

    /// <summary>
    /// Check if a station type is a gun position.
    /// In B-17F, Navigator and Bombardier each operate a nose gun.
    /// </summary>
    public bool IsGunStation(StationType type)
    {
        return type == StationType.TopTurret ||
               type == StationType.BallTurret ||
               type == StationType.LeftWaistGun ||
               type == StationType.RightWaistGun ||
               type == StationType.TailGun ||
               type == StationType.Navigator ||
               type == StationType.Bombardier;
    }

    /// <summary>
    /// Mark a station as inoperable (e.g., turret destroyed).
    /// </summary>
    public void SetStationOperational(StationType type, bool operational)
    {
        var station = GetStation(type);
        if (station == null) return;

        station.IsOperational = operational;

        // If destroyed while occupied, vacate it
        if (!operational && station.IsOccupied)
        {
            VacateStation(type);
        }

        Debug.Log($"[StationManager] {station.StationId} operational status: {operational}");
    }

    /// <summary>
    /// Handle crew death - vacate their station.
    /// </summary>
    public void OnCrewDied(CrewMember crew)
    {
        if (crew.CurrentStation != StationType.None)
        {
            VacateStation(crew.CurrentStation, crew.Id);
        }
    }

    /// <summary>
    /// Handle crew becoming incapacitated - vacate their station.
    /// </summary>
    public void OnCrewIncapacitated(CrewMember crew)
    {
        // Serious and Critical are incapacitated (Light can still work)
        if (crew.Status == CrewStatus.Serious || crew.Status == CrewStatus.Critical || crew.Status == CrewStatus.Unconscious)
        {
            if (crew.CurrentStation != StationType.None)
            {
                VacateStation(crew.CurrentStation, crew.Id);
            }
        }
    }

    [ContextMenu("Debug: List All Stations")]
    private void DebugListStations()
    {
        if (AllStations == null || AllStations.Count == 0)
        {
            Debug.Log("[StationManager] No stations configured.");
            return;
        }

        Debug.Log($"[StationManager] === All Stations ({AllStations.Count}) ===");
        foreach (var station in AllStations)
        {
            string occupant = station.IsOccupied ? station.OccupiedByCrewId : "EMPTY";
            string status = station.IsOperational ? "Operational" : "DESTROYED";
            Debug.Log($"  {station.StationId} ({station.Type}) | Section: {station.SectionId} | {status} | Occupant: {occupant}");
        }
    }

    [ContextMenu("Debug: List Unmanned Stations")]
    private void DebugListUnmannedStations()
    {
        var unmanned = GetUnmannedStations();
        Debug.Log($"[StationManager] === Unmanned Stations ({unmanned.Count}) ===");
        foreach (var station in unmanned)
        {
            Debug.Log($"  {station.StationId} ({station.Type}) in {station.SectionId}");
        }
    }
}
