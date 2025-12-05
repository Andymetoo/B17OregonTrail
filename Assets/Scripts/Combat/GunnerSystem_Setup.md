# First-Person Gunner System - Setup Guide

## Overview
This system enables transitioning between 2D plane management (UI Mode) and 3D first-person gunner view (Combat Mode) within a single scene. No scene loading required!

## Architecture Summary
- **Same Scene**: Both 2D and 3D worlds coexist in one scene
- **Camera Switching**: Two cameras toggle between orthographic (UI) and perspective (combat)
- **Input Abstraction**: Supports both mouse (desktop) and touch (mobile) controls
- **State Preservation**: All managers (PlaneManager, CrewManager, etc.) stay active

---

## Step 1: Scene Setup

### Create 3D Combat World
1. In your main scene, create a new empty GameObject: `CombatWorld`
2. Position it far from your 2D UI (e.g., at Y=1000 or use a different Layer)
3. Add child objects:
   - Sky (skybox or simple quad)
   - Ground plane (for visual reference)
   - Empty GameObjects for gunner station positions

### Station Positions
Create empty GameObjects as children of your B-17 model or `CombatWorld`:
- `Station_TopTurret`
- `Station_BallTurret`
- `Station_LeftWaist`
- `Station_RightWaist`
- `Station_TailGunner`
- `Station_Nose`

Position each at the appropriate location with correct rotation (forward = where gun aims).

---

## Step 2: Camera Setup

### Main UI Camera (Existing)
- Your current camera for 2D UI
- Should be Orthographic
- Tag: Leave as MainCamera or change if needed
- Add AudioListener (if not already present)

### Combat Camera (New)
1. Create new Camera: `CombatCamera`
2. Set Projection: **Perspective**
3. Position: Doesn't matter initially (will be controlled by script)
4. Add **AudioListener** (script will manage which listener is active)
5. **Disable** this camera by default (GameModeManager will enable it)
6. Optionally parent it to `CombatWorld`

---

## Step 3: Manager Setup

### GameModeManager
1. Create empty GameObject: `GameModeManager`
2. Add component: `GameModeManager.cs`
3. Assign in Inspector:
   - **Main UI Camera**: Your existing camera
   - **Combat Camera**: The new perspective camera you just created
   - **UI Mode Canvas**: Your main UI canvas (with Orders, Overview, etc.)
   - **Combat Mode Canvas**: (Create in Step 4)
   - **Auto Manage Audio Listeners**: Check this (prevents duplicate listener warnings)

### GunnerStationController
1. Create empty GameObject: `GunnerStationController` (or add to `CombatWorld`)
2. Add component: `GunnerStationController.cs`
3. Assign in Inspector:
   - **Station Transforms**: Drag your 6 station empty GameObjects
   - **Combat Camera**: Reference to your Combat Camera
   - **Transition Speed**: 5 (adjust to taste)
   - **Smooth Transitions**: Check this for animated camera movement
   - **Look Limits**: Horizontal=60, Vertical=45 (adjust per station if needed)

---

## Step 4: Combat UI Canvas

### Create Canvas
1. Create new Canvas: `CombatModeCanvas`
2. Set Render Mode: **Screen Space - Camera** (important for 3D mode)
3. Set **Render Camera**: CombatCamera
4. **Disable** this canvas by default

### Add UI Elements
Create these as children of `CombatModeCanvas`:

#### Station Info (Top-Left)
- TextMeshPro: `StationNameText` (e.g., "Top Turret")
- TextMeshPro: `CrewNameText` (e.g., "Gunner: Joe Smith")

#### Station Switcher Buttons (Left/Right Side)
- Create Panel: `StationButtonContainer`
- Create Button Prefab: `StationButtonPrefab` (with TextMeshPro child)
  - This will be duplicated for each station automatically

#### Return Button (Top-Right)
- Button: `ReturnToMainButton` → Text: "Return" or "Exit" or "◄ Main"

#### Optional: Combat HUD
- TextMeshPro: `AmmoText` (for future ammo display)
- TextMeshPro: `EnemyCountText` (for future enemy counter)
- TextMeshPro: `CombatLogText` (for combat messages)

### Wire Up CombatUIController
1. Add component to `CombatModeCanvas`: `CombatUIController.cs`
2. Assign references:
   - Station Name/Crew Text
   - Station Button Container
   - Station Button Prefab
   - Return Button
   - Optional HUD elements

---

## Step 5: Input Setup

### Desktop/Editor (Mouse)
1. Add `MouseGunnerInput.cs` component to your **Combat Camera**
2. Configure:
   - Mouse Sensitivity: 2.0
   - Invert Y: false (or true if you prefer inverted)
   - Fire Button: Mouse0 (left click)

### Mobile (Touch) - Optional Now
1. Add `TouchGunnerInput.cs` component to your **Combat Camera** (can coexist with MouseGunnerInput)
2. Configure:
   - Touch Sensitivity: 0.5
   - Fire Zone Width Percent: 0.3 (right 30% of screen = fire)

### Camera Controller
1. Add `GunnerCameraController.cs` component to your **Combat Camera**
2. Assign:
   - **Input Handler**: The `MouseGunnerInput` component (same GameObject)
   - Horizontal/Vertical Speed: 2.0
   - Smooth Rotation: true
   - Smooth Factor: 10

---

## Step 6: Hook Up Existing UI

### Option A: Use Existing "Fire" Buttons
If you have "Fire" buttons in your Orders UI:
1. Add component: `EnterCombatButton.cs`
2. Set **Target Station** to appropriate station (e.g., LeftWaist)
3. Check **Require Healthy Crew**: true

### Option B: Create New "Enter Combat" Buttons
1. Add buttons to your main UI (near each gunner section)
2. Label them "Enter Turret", "Man Guns", etc.
3. Add `EnterCombatButton.cs` component
4. Assign appropriate `GunnerStation` enum value

---

## Step 7: Testing Checklist

### In Editor (Play Mode):
1. **Start in UI Mode**:
   - ✅ Main camera active, 2D UI visible
   - ✅ Combat camera disabled, combat UI hidden

2. **Click "Fire" or "Enter Combat" button**:
   - ✅ Camera switches to combat view
   - ✅ 3D world visible
   - ✅ Combat UI shows station name and crew
   - ✅ Station switcher buttons appear
   - ✅ Mouse moves camera (look around)

3. **Click Another Station Button**:
   - ✅ Camera smoothly transitions to new station
   - ✅ UI updates to show new station name

4. **Click "Return to Main"**:
   - ✅ Camera switches back to 2D UI
   - ✅ Main UI canvas reappears
   - ✅ Combat canvas hidden

5. **Test with Injured Crew**:
   - Use Debug Panel to injure a gunner
   - ✅ Their station button becomes disabled/grayed out
   - ✅ Can't enter that station in combat mode

6. **Test Audio**:
   - ✅ No "Multiple AudioListener" warnings in console

---

## Step 8: Next Steps (Future Phases)

### Phase 2: Enemy Fighters
- Create simple 3D fighter prefab
- Add `FighterSpawner.cs` system
- Implement attack runs and AI pathing

### Phase 3: Shooting Mechanics
- Implement firing in `GunnerCameraController.Fire()`
- Raycast from camera forward
- Hit detection on enemy fighters
- Apply damage to PlaneManager sections

### Phase 4: Combat Flow
- Hook into `MissionPhase.Fighters`
- Spawn fighters when phase begins
- Despawn when phase ends
- Transition back to UI mode automatically

### Phase 5: Polish
- Add muzzle flash effects
- Tracer rounds
- Hit indicators
- Ammo system
- Damage feedback

---

## Troubleshooting

### Camera doesn't switch
- Check GameModeManager references (both cameras assigned?)
- Ensure Combat Camera is a child of GunnerStationController or assigned correctly

### Can't look around
- Verify `MouseGunnerInput` is on Combat Camera
- Check `GunnerCameraController.inputHandler` is assigned
- Ensure you're in Combat Mode (check GameModeManager.CurrentMode)

### Station buttons don't work
- Check CombatUIController button prefab assignment
- Verify station button container exists
- Look for errors in console during button population

### Duplicate AudioListener warnings
- Enable `autoManageAudioListeners` on GameModeManager
- Or manually disable AudioListener on one camera

### Combat UI not visible
- Check Canvas Render Mode = Screen Space - Camera
- Verify Canvas Render Camera = CombatCamera
- Ensure Canvas is enabled when in combat mode

---

## Files Created

### Core Managers
- `GameModeManager.cs` - Mode switching and state management
- `GunnerStationController.cs` - Station positions and camera movement

### Input System
- `IGunnerInput.cs` - Input interface
- `MouseGunnerInput.cs` - Desktop mouse controls
- `TouchGunnerInput.cs` - Mobile touch controls
- `GunnerCameraController.cs` - Camera rotation and firing

### UI Components
- `CombatUIController.cs` - Combat mode UI management
- `EnterCombatButton.cs` - Button to enter combat mode from main UI

### Documentation
- `GunnerSystem_Setup.md` - This file

---

## Architecture Benefits

✅ **No Scene Loading** - Instant transitions, no DontDestroyOnLoad complexity  
✅ **State Persistence** - All managers stay active, events keep firing  
✅ **Mobile Ready** - Touch input abstracted and ready to use  
✅ **Modular** - Easy to add more stations, change layouts  
✅ **Testable** - Can enter/exit combat mode anytime during mission  

---

## Quick Reference: Inspector Assignments

```
GameModeManager:
├─ mainUICamera → Main Camera (your existing camera)
├─ combatCamera → CombatCamera (new perspective camera)
├─ uiModeCanvas → Your main Canvas
└─ combatModeCanvas → CombatModeCanvas

GunnerStationController:
├─ topTurretStation → Station_TopTurret GameObject
├─ ballTurretStation → Station_BallTurret GameObject
├─ leftWaistStation → Station_LeftWaist GameObject
├─ rightWaistStation → Station_RightWaist GameObject
├─ tailGunnerStation → Station_TailGunner GameObject
├─ noseStation → Station_Nose GameObject
└─ combatCamera → CombatCamera

CombatCamera:
├─ MouseGunnerInput component
├─ GunnerCameraController component
│  └─ inputHandler → MouseGunnerInput (same GameObject)
└─ AudioListener component

CombatUIController (on CombatModeCanvas):
├─ stationNameText → StationNameText TextMeshPro
├─ crewNameText → CrewNameText TextMeshPro
├─ stationButtonContainer → Panel for buttons
├─ stationButtonPrefab → Button prefab
└─ returnToMainButton → Return Button

EnterCombatButton (on each Fire/Combat button):
└─ targetStation → GunnerStation enum (TopTurret, LeftWaist, etc.)
```

---

Ready to test! Start with just getting the camera switching working, then add stations and polish from there.
