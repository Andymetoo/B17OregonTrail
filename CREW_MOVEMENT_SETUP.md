# Crew Movement System - Setup Guide

## Overview
Crew members now physically move on screen to perform actions:
1. **Move to target** (section/crew/station)
2. **Perform action** (repair/medical/extinguish)
3. **Return to home station**

## New Components

### 1. CrewVisualizer
Displays crew sprites and handles movement animation.

**Location**: `Assets/Scripts/UI/CrewVisualizer.cs`

**Setup**:
1. Create a UI Image GameObject for each crew member
2. Add `CrewVisualizer` component
3. Assign crew ID (e.g., "Pilot", "Engineer")
4. Assign three sprites:
   - **Idle Sprite**: Standing at station
   - **Moving Sprite**: Walking animation frame
   - **Working Sprite**: Performing action (wrench icon, medical, etc.)

### 2. CrewPositionRegistry
Maps section IDs and station IDs to screen positions.

**Location**: `Assets/Scripts/Core/Managers/CrewPositionRegistry.cs`

**Setup**:
1. Add component to your Canvas or UI root GameObject
2. **Section Positions**: Drag your section buttons into the list and set their IDs
   - Example: `Cockpit` → drag CockpitButton RectTransform
3. **Station Positions**: Set home positions for each crew station
   - Example: `TailGun` → Vector2(-200, 0)
4. **Main Path Y**: Set the Y-coordinate of the fuselage centerline
   - Crew will "snap" to this Y when moving (for turret gunners above/below)

## Setup Steps

### Step 1: Initialize Crew Home Positions
In `CrewManager.Start()` or inspector, set each `CrewMember.HomePosition`:
```csharp
// Example positions (adjust to match your UI layout)
pilot.HomePosition = new Vector2(150, 50);      // Cockpit, slightly above
copilot.HomePosition = new Vector2(120, 50);
engineer.HomePosition = new Vector2(0, 0);      // Center fuselage
tailGunner.HomePosition = new Vector2(-180, -30); // Tail, slightly below
```

### Step 2: Create CrewVisualizer GameObjects
For each crew member:
1. Create UI > Image
2. Name it: `CrewSprite_Pilot`, etc.
3. Set anchors to middle-center
4. Add `CrewVisualizer` component
5. Assign crew ID: "Pilot"
6. Assign three sprite states (can be same sprite for now, animate later)

### Step 3: Set Up CrewPositionRegistry
1. Create empty GameObject: `CrewPositionRegistry`
2. Add `CrewPositionRegistry` component
3. Fill in section positions:
   - Size: 10 (or however many sections you have)
   - Entry 0: sectionId = "Cockpit", positionTransform = drag your Cockpit button
   - Entry 1: sectionId = "Nose", positionTransform = drag Nose button
   - etc.
4. Fill in station positions (crew home locations):
   - Entry 0: stationId = "PilotStation", position = (150, 50)
   - Entry 1: stationId = "TailGun", position = (-180, -30)
   - etc.
5. Set `mainPathY` to your fuselage centerline (e.g., 0)
6. Enable `usePathSnapping` if you want crew to move along the centerline

### Step 4: Test Movement
1. Start play mode
2. Select a crew member
3. Give them a repair/fire/medical order
4. Watch them:
   - Walk to the target section/crew
   - Perform the action (sprite changes to "working")
   - Walk back to their station

## Action Phases

Every action now has three phases:
```csharp
ActionPhase.MoveToTarget  // Crew walking to destination
ActionPhase.Performing    // Action in progress (timer-based)
ActionPhase.Returning     // Walking back to station
```

Cancel at any time returns crew immediately to their home position.

## Sprite States

Crew visual state determines which sprite is shown:
- `IdleAtStation`: Standing at their station (default)
- `Moving`: Walking to/from a target
- `Working`: Performing an action (repair, medical, extinguish)

## Movement Speed

Default: 50 pixels/second (set in `CrewMember.MoveSpeed`)

Adjust per crew member if you want pilots to move faster than gunners, etc.

## Path Snapping (for Ball Turret / Cockpit)

Crew above/below the main fuselage will:
1. Snap to the main path Y-coordinate when starting movement
2. Move along the centerline
3. Return to their offset home position when done

Enable/disable this in `CrewPositionRegistry.usePathSnapping`.

## Cancellation

When crew is cancelled (via UI or injury):
- They immediately teleport back to `HomePosition`
- Visual state returns to `IdleAtStation`
- No "walk back" animation on cancel (instant abort)

## Next Steps

1. **Placeholder sprites**: Use simple colored squares for now
   - Idle: Blue square
   - Moving: Green square
   - Working: Red square
2. **Test with chaos**: Watch crew get interrupted by injuries mid-movement
3. **Animate later**: Replace sprite swaps with actual walk cycles
4. **Polish**: Add movement sounds, dust effects, etc.

## Troubleshooting

**Crew not moving?**
- Check `CrewPositionRegistry.Instance` is not null
- Verify section/station IDs match exactly (case-sensitive)
- Check console for "[CrewPositionRegistry] not found" warnings

**Crew teleporting instead of walking?**
- Increase `CrewVisualizer.smoothing` value (default 8)
- Check `CrewMember.MoveSpeed` is > 0

**Crew stuck in "Moving" state?**
- Check that target positions are reachable (not Vector2.zero)
- Verify `ActionPhase` transitions are firing in console logs
