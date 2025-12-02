# Station Occupation System - Setup Guide

## Overview
The station occupation system allows crew members to be assigned to specific stations (guns, pilot seats, navigator, etc.) and enables crew succession when members die or become incapacitated. This guide covers complete setup from scratch.

---

## Part 1: Scene Setup

### 1.1 Add StationManager to Scene
1. In your main scene (likely in `Scenes/`), create a new empty GameObject
2. Name it: `StationManager`
3. Add Component → Scripts → Core → Managers → `StationManager`
4. The StationManager will auto-initialize 10 default stations if `AllStations` list is empty (B-17F configuration)

### 1.2 Configure Station List (Optional)
If you want manual control over station configuration:
1. Select the `StationManager` GameObject
2. In Inspector, expand `All Stations` list
3. Add 10 elements (one for each station type)
4. For each station, configure:
   - **Type**: Choose from dropdown (TopTurret, BallTurret, LeftWaistGun, RightWaistGun, TailGun, Pilot, CoPilot, Navigator, RadioOperator, Bombardier)
   - **Station Id**: Unique string matching CrewPositionRegistry (e.g., "top_turret", "navigator")
   - **Section Id**: Which section this station is in (e.g., "Nose", "Cockpit", "Fuselage", "Tail")
   - **Occupied By Crew Id**: Leave blank (managed at runtime)
   - **Is Operational**: Check true (will be set false when turret/station destroyed)

**Default Station Configuration (Auto-Created) - B-17F:**
```
1. TopTurret      → ID: "TopTurret"      Section: "Cockpit"
2. BallTurret     → ID: "BallTurret"     Section: "Fuselage"
3. LeftWaistGun   → ID: "LeftWaistGun"   Section: "Fuselage"
4. RightWaistGun  → ID: "RightWaistGun"  Section: "Fuselage"
5. TailGun        → ID: "TailGun"        Section: "Tail"
6. Pilot          → ID: "Pilot"          Section: "Cockpit"
7. CoPilot        → ID: "CoPilot"        Section: "Cockpit"
8. Navigator      → ID: "Navigator"      Section: "Nose" (operates left nose gun)
9. RadioOperator  → ID: "RadioOperator"  Section: "Fuselage"
10. Bombardier    → ID: "Bombardier"     Section: "Nose" (operates right nose gun)
```

**Note:** In the B-17F (Memphis Belle configuration), there is no powered nose turret. The Navigator and Bombardier each operate a single flexible .50 cal machine gun in the nose.

---

## Part 2: Crew Configuration

### 2.1 Assign Default Stations to Crew
1. In your crew setup (likely in a prefab or ScriptableObject defining crew roster)
2. For each `CrewMember`, set the `DefaultStation` field:
   - **Pilot crew member**: `DefaultStation = StationType.Pilot`
   - **Co-Pilot crew member**: `DefaultStation = StationType.CoPilot`
   - **Navigator**: `DefaultStation = StationType.Navigator` (operates left nose gun)
   - **Bombardier**: `DefaultStation = StationType.Bombardier` (operates right nose gun)
   - **Radio Operator**: `DefaultStation = StationType.RadioOperator`
   - **Gunners**: Assign to specific gun stations (TopTurret, BallTurret, LeftWaistGun, RightWaistGun, TailGun)

**Example Crew Setup (B-17F with 10 crew):**
```csharp
// In your crew initialization code:
crewList[0].DefaultStation = StationType.Pilot;
crewList[1].DefaultStation = StationType.CoPilot;
crewList[2].DefaultStation = StationType.Navigator;      // Nose gun (left)
crewList[3].DefaultStation = StationType.Bombardier;     // Nose gun (right)
crewList[4].DefaultStation = StationType.RadioOperator;
crewList[5].DefaultStation = StationType.TopTurret;
crewList[6].DefaultStation = StationType.BallTurret;
crewList[7].DefaultStation = StationType.LeftWaistGun;
crewList[8].DefaultStation = StationType.RightWaistGun;
crewList[9].DefaultStation = StationType.TailGun;
```

### 2.2 Verify CrewPositionRegistry Matches Station IDs
1. Open `CrewPositionRegistry` in your scene (or wherever it's configured)
2. Ensure station position entries match the Station IDs from StationManager:
   - `GetStationPosition("TopTurret")` should return valid Vector2
   - `GetStationPosition("Pilot")` should return cockpit position
   - `GetStationPosition("Navigator")` should return nose position (left gun)
   - `GetStationPosition("Bombardier")` should return nose position (right gun)
   - etc. for all 10 station IDs

If positions are missing, add them to the registry with appropriate coordinates.

---

## Part 3: Testing the System

### 3.1 Verify Initialization
1. Start Play Mode
2. Check Console for: `"StationManager: Assigned crew [CrewName] to default station [StationType]"` messages
3. Each crew member should be assigned to their DefaultStation

### 3.2 Debug Station State
Use StationManager's context menu options (right-click component in Inspector during Play Mode):
- **Debug: List All Stations** - Shows all 10 stations with occupancy status
- **Debug: List Unmanned Stations** - Shows which stations are currently vacant

### 3.3 Test Station Occupation Action
1. Kill or incapacitate a crew member at a station (e.g., via debug command or combat)
2. Console should show: `"Station [Type] vacated by crew [Name]"`
3. Manually trigger an `OccupyStation` action for another crew member:
   ```csharp
   CrewManager.Instance.TryAssignAction(crewId, ActionType.OccupyStation, targetStation);
   ```
4. Crew should:
   - Walk through sections to reach the station
   - Occupy the station (console: `"Crew [Name] occupied station [Type]"`)
   - Walk back through sections to home position

### 3.4 Verify Event Hooks
1. When crew dies: Station should auto-vacate (check console)
2. When crew becomes unconscious: Station should auto-vacate
3. StationManager events fire: `OnStationOccupied`, `OnStationVacated`

---

## Part 4: UI Integration (Pending Implementation)

### 4.1 Station Indicators in CrewStatusIndicator
**TODO:** Extend `CrewStatusIndicator.cs` to show station assignment:
- Add small icon/sprite field for each station type (gun icon, pilot icon, navigator icon, etc.)
- In `UpdateDisplay()`, check `crew.CurrentStation` and display appropriate icon
- Position icon next to crew portrait or status area

**Suggested Implementation:**
```csharp
// In CrewStatusIndicator.cs
[SerializeField] private Image stationIconImage;
[SerializeField] private Sprite gunIcon;
[SerializeField] private Sprite pilotIcon;
[SerializeField] private Sprite navigatorIcon;
// ... etc for all station types

private void UpdateStationIcon(CrewMember crew)
{
    if (crew.CurrentStation == StationType.None)
    {
        stationIconImage.gameObject.SetActive(false);
        return;
    }
    
    stationIconImage.gameObject.SetActive(true);
    stationIconImage.sprite = GetIconForStation(crew.CurrentStation);
}

private Sprite GetIconForStation(StationType type)
{
    // Return appropriate sprite based on station type
    if (StationManager.Instance.IsGunStation(type)) return gunIcon;
    if (type == StationType.Pilot || type == StationType.CoPilot) return pilotIcon;
    if (type == StationType.Navigator) return navigatorIcon;
    // ... etc
}
```

### 4.2 Station Occupancy Panel
**TODO:** Create a new UI panel showing all stations and their occupants:
- Panel with 10 rows (one per station type)
- Each row: Station icon + Station name + Current occupant name (or "UNMANNED")
- Update in real-time via StationManager events (`OnStationOccupied`, `OnStationVacated`)

**Suggested Structure:**
```
StationOccupancyPanel (Canvas Group)
├── Header ("CREW STATIONS - B-17F")
├── StationRowList (Vertical Layout Group)
│   ├── StationRow_Navigator
│   │   ├── Icon (Image - gun icon)
│   │   ├── Label ("Navigator (Nose Gun L)")
│   │   └── OccupantName (Text - updates dynamically)
│   ├── StationRow_Bombardier
│   │   ├── Icon (Image - gun icon)
│   │   ├── Label ("Bombardier (Nose Gun R)")
│   │   └── OccupantName (Text)
│   ├── StationRow_TopTurret
│   │   └── ... (repeat for all 10 stations)
```

### 4.3 "Occupy Station" Button in OrdersUIController
**TODO:** Add button to orders panel when clicking unmanned stations:
1. In `OrdersUIController.cs`, detect when user clicks on an unmanned station
2. Show "Occupy Station" button alongside existing action buttons
3. When clicked, call:
   ```csharp
   CrewManager.Instance.TryAssignAction(selectedCrewId, ActionType.OccupyStation, targetStation);
   ```
4. Hide button if station is already occupied or crew is incapacitated

**Integration Points:**
- Hook into existing click detection system (likely in scene interaction manager)
- Query `StationManager.Instance.IsStationAvailable(stationType)` to check availability
- Use `StationManager.Instance.GetStation(stationType)` to get target station data

---

## Part 5: Advanced Features (Future)

### 5.1 Station-Based Combat Effects
- Manned gun stations increase hit chance against fighters
- Unmanned guns cannot fire (fighters have easier time)
- Dead pilot/co-pilot: plane gradually loses altitude or veers off course
- Unmanned navigator: slower progress to target, navigation errors

### 5.2 Station Damage
- When section takes damage, check if station is in that section
- Call `StationManager.Instance.SetStationOperational(stationType, false)`
- Prevent crew from occupying destroyed stations
- Require repair action to restore station functionality

### 5.3 Automatic Station Succession
- When station vacated due to death/injury, automatically suggest nearby crew member
- Show notification: "Nose Gun unmanned! Assign [CrewName]?"
- One-click button to trigger OccupyStation action

### 5.4 Station Priority System
- Critical stations (Pilot, CoPilot) get highest priority for succession
- Gun stations prioritized by threat level (fighters = all guns, flak = none)
- Navigator/Bombardier/Radio prioritized by mission phase

---

## Part 6: Troubleshooting

### Issue: Crew not assigned to stations at mission start
**Fix:** Ensure `StationManager.AssignDefaultStations()` is called during initialization. Check `CrewManager.DelayedInitPositions()` for the call.

### Issue: "Station not found" errors
**Fix:** Verify Station IDs in `StationManager.AllStations` match the IDs used in `CrewPositionRegistry`. Check for typos (e.g., "nose_gun" vs "nosegun").

### Issue: Crew walks through walls instead of sections
**Fix:** Ensure `OccupyStation` action uses section pathfinding. Verify `station.SectionId` is correctly set and matches actual section IDs in scene.

### Issue: Station occupation completes but crew doesn't show at station
**Fix:** Check `CrewPositionRegistry.GetStationPosition(stationId)` returns valid position. Ensure station positions are configured with correct coordinates.

### Issue: Crew doesn't vacate station when dying
**Fix:** Verify `StationManager` subscribes to `CrewManager.OnCrewDied` and `OnCrewInjuryStageChanged` events in `Awake()` or during initialization.

### Issue: Multiple crew occupy same station
**Fix:** Check `StationManager.AssignCrewToStation()` validates availability before assigning. Ensure `IsStationAvailable()` check runs before action assignment.

---

## Part 7: Key Code References

### Creating OccupyStation Action (From Code)
```csharp
// Get target station
var station = StationManager.Instance.GetStation(StationType.NoseGun);
if (station != null && station.IsAvailable)
{
    // Assign action to crew member
    bool success = CrewManager.Instance.TryAssignAction(
        crewId: "crew_gunner_2",
        actionType: ActionType.OccupyStation,
        targetPosition: Vector2.zero, // Will be resolved from station
        targetId: station.StationId,
        targetStation: station.Type
    );
}
```

### Querying Station State
```csharp
// Check if station is available
bool available = StationManager.Instance.IsStationAvailable(StationType.Pilot);

// Get all unmanned stations
var unmanned = StationManager.Instance.GetUnmannedStations();

// Get crew occupying specific station
string crewId = StationManager.Instance.GetStationOccupant(StationType.TopTurret);

// Get all manned gun stations (for combat calculations)
// In B-17F, this includes Navigator and Bombardier (nose guns) plus 5 turret positions
var mannedGuns = StationManager.Instance.GetMannedGunStations();
```

### Listening to Station Events
```csharp
void OnEnable()
{
    StationManager.OnStationOccupied += HandleStationOccupied;
    StationManager.OnStationVacated += HandleStationVacated;
}

void OnDisable()
{
    StationManager.OnStationOccupied -= HandleStationOccupied;
    StationManager.OnStationVacated -= HandleStationVacated;
}

void HandleStationOccupied(StationType type, string crewId)
{
    Debug.Log($"Station {type} now manned by {crewId}");
    // Update UI, recalculate combat stats, etc.
}

void HandleStationVacated(StationType type, string crewId)
{
    Debug.Log($"Station {type} vacated by {crewId}");
    // Show notification, suggest replacement crew, etc.
}
```

---

## Part 8: Tuning Parameters

### StationManager (Inspector)
- **All Stations**: List of all station configurations (auto-initializes to 10 if empty for B-17F)
  - Manually configure Section IDs if your plane sections differ from defaults
  - Set `IsOperational = false` for destroyed stations during testing
  - Note: Navigator and Bombardier are gun stations (nose guns) in addition to their primary roles

### CrewMember
- **Default Station**: Initial station assignment at mission start
  - Ensure all combat-critical stations (Pilot, CoPilot, guns) have assigned crew
  - Set to `None` for crew without specific station (e.g., engineer, medic)

### CrewActionConfig (Time costs)
- **OccupyStation**: Time to settle into station after reaching position (suggested: 2-3 seconds)
  - Currently uses default action time
  - Can be tuned separately if station occupation should be faster/slower than other actions

---

## Summary Checklist

Before deploying station system:
- [ ] StationManager GameObject added to scene with component attached
- [ ] All 10 stations configured for B-17F (auto-initialize or manual setup)
- [ ] Crew members have DefaultStation assigned (Pilot, CoPilot, Navigator, Bombardier, RadioOperator, 5 gunners)
- [ ] CrewPositionRegistry has positions for all 10 station IDs
- [ ] Navigator and Bombardier positions in nose section (left/right nose gun positions)
- [ ] Tested crew assignment at mission start (check console logs)
- [ ] Tested station vacation on crew death (check unmanned stations debug)
- [ ] Tested OccupyStation action (crew walks through sections, occupies station, returns)
- [ ] (Future) UI indicators showing crew station assignments
- [ ] (Future) Station occupancy panel with real-time updates
- [ ] (Future) "Occupy Station" button in OrdersUIController

---

## Notes
- **B-17F Configuration**: This system uses the B-17F model (Memphis Belle), which has 10 crew positions and NO powered nose turret
- **Navigator & Bombardier**: Both are gun stations in addition to their primary roles (each operates a flexible .50 cal nose gun)
- Station system is fully functional at backend level (actions, pathfinding, event handling)
- UI integration is pending (indicators, panel, button) but not required for system to work
- System is designed for extension: station damage, combat effects, automatic succession can be added
- All station-related code is centralized in `StationManager.cs` for easy maintenance
- Events (`OnStationOccupied`, `OnStationVacated`) provide hooks for future features without modifying core logic
- Total gun positions: 7 (TopTurret, BallTurret, LeftWaistGun, RightWaistGun, TailGun, Navigator, Bombardier)
