# Crew Movement System - Simplified

## What You Need

### 1. Position Transforms (All with CENTER anchors 0.5, 0.5)
- **Station positions**: Empty GameObjects where each crew idles at home
- **Section positions**: Empty GameObjects where crew stand when repairing/extinguishing
 - You may place these under ANY parent in the canvas.
   Set `CrewPositionRegistry.referenceParent` to the parent of your crew sprites and positions will be converted into that parent space automatically.

### 2. CrewPositionRegistry Component
Maps string IDs to the transforms above:
- `stationPositions`: List of {id, transform} for crew home bases
  - **ID must match `CrewMember.CurrentStationId`** (e.g., "Tail", "Cockpit", "TopTurret")
- `sectionPositions`: List of {id, transform} for action targets
  - **ID must match `PlaneSectionState.Id`** (e.g., "NoseBomb", "RadioRoom", "LeftWing")
 - `referenceParent` (RectTransform): Set this to the parent of your crew sprites (e.g., `Canvas/CrewSprites`).
   All station/section transforms are converted into this parent’s local space, so anchors/parents can differ.
 - Ordered sections: The order of `sectionPositions` defines adjacency. Waypoint pathing will make crew pass through each intermediate section’s transform between their current section and the target.

### 3. Crew Sprites
- Each has CrewVisualizer component
- Must have center anchors (0.5, 0.5). If you set `referenceParent`, crew sprites should be children of that parent. Position transforms can live anywhere.

## Setup in Unity

```
Canvas
├── Positions (anchors: 0.5, 0.5)
│   ├── Tail_Home
│   ├── Pilot_Home
│   ├── NoseBomb_Target
│   └── RadioRoom_Target
└── CrewSprites (anchors: 0.5, 0.5)
    ├── TailGunnerSprite (CrewVisualizer, crewId="TailGunner")
    └── PilotSprite (CrewVisualizer, crewId="Pilot")
```

In CrewPositionRegistry inspector:
- Station Positions: Add {"Tail", Tail_Home transform}
- Section Positions: Add {"NoseBomb", NoseBomb_Target transform}

## What Was Removed
- ❌ Manual Vector2 positions
- ❌ StationPositionEntry.GetPosition() complexity
- ❌ Path snapping
- ❌ Gizmos
- ❌ Custom editor
- ❌ Coroutine timing hacks
- ❌ Debug log spam
- ❌ Duplicate lookup dictionaries

## How It Works
1. Registry maps IDs → Transforms
2. CrewPositionRegistry converts the transform world position into `referenceParent` local space
3. CrewManager sets crew.CurrentPosition from registry lookups
4. CrewVisualizer lerps sprite to crew.CurrentPosition
5. That's it!

### Station Changes
When a crew successfully completes a Move or ManStation action their `CurrentStationId` updates and `CrewManager` calls an internal refresh to pull the new station's transform from `CrewPositionRegistry` and update both `HomePosition` and `CurrentPosition`. If you move station transforms during play and want crew to adopt the new location, ensure you trigger another Move/ManStation action or add a small dev helper that re-calls the refresh.

### Simulation Tick
Only one system should tick `CrewManager.Tick` per frame. If `SimulationTicker` exists it is the authoritative driver (fixed rate). `GameStateManager` now defers to it; without `SimulationTicker` present `GameStateManager` provides a fallback tick. Having both previously caused rapid phase transitions.

## Critical Rule
**Everything must use CENTER ANCHORS (0.5, 0.5)**.
If you do NOT set `referenceParent`, then sprites and position transforms must share the same parent.
If you DO set `referenceParent`, make sprites children of it; position transforms can be placed anywhere under the canvas.
