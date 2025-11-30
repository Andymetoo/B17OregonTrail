# Crew Movement System - Simplified

## What You Need

### 1. Position Transforms (All with CENTER anchors 0.5, 0.5)
- **Station positions**: Empty GameObjects where each crew idles at home
- **Section positions**: Empty GameObjects where crew stand when repairing/extinguishing

### 2. CrewPositionRegistry Component
Maps string IDs to the transforms above:
- `stationPositions`: List of {id, transform} for crew home bases
  - **ID must match `CrewMember.CurrentStationId`** (e.g., "Tail", "Cockpit", "TopTurret")
- `sectionPositions`: List of {id, transform} for action targets
  - **ID must match `PlaneSectionState.Id`** (e.g., "NoseBomb", "RadioRoom", "LeftWing")

### 3. Crew Sprites
- Each has CrewVisualizer component
- Must have same parent and anchors (0.5, 0.5) as position transforms

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
2. CrewManager sets crew.CurrentPosition from registry lookups
3. CrewVisualizer lerps sprite to crew.CurrentPosition
4. That's it!

## Critical Rule
**Everything must use CENTER ANCHORS (0.5, 0.5)** and share the same parent coordinate space.
