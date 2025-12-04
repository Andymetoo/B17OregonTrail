# Plane Overview UI - Setup Instructions

## Overview
A full-screen informational overlay that displays comprehensive plane status in **separate columns**:
- **Crew Column**: Name (color-coded by health), status, current action
- **Sections Column**: Name (color-coded by integrity), integrity %, fire/destroyed indicators
- **Systems & Engines Column**: Systems and engines with status and integrity
- **General Info Column**: Fuel, speed, altitude, supplies

The overview **pauses the game** while open and closes on **any mouse click**.

---

## Unity Setup

### 1. Create the Overview Panel
1. In your Canvas, create a new **Panel** GameObject:
   - Right-click Canvas → UI → Panel
   - Rename it to `PlaneOverviewPanel`

2. Configure the panel to cover the full screen:
   - Anchor: Stretch both (full screen)
   - Color: Semi-transparent dark background (e.g., `rgba(0, 0, 0, 0.9)`)

### 2. Create the Column Layout
Inside `PlaneOverviewPanel`, create a layout structure:

1. Create an **Empty GameObject** named `ColumnsContainer`
   - Add **Horizontal Layout Group** component
   - Settings:
     - Child Alignment: Top
     - Control Child Size: ✓ Width, ✓ Height
     - Child Force Expand: ✓ Width, ✓ Height
     - Spacing: 20-40

2. Inside `ColumnsContainer`, create **4 TextMeshPro Text** objects:
   - **CrewText** (Column 1)
   - **SectionsText** (Column 2)
   - **SystemsText** (Column 3)
   - **GeneralText** (Column 4)

3. Configure each TextMeshPro:
   - Font Size: 14-16
   - Color: White
   - Alignment: Top-Left
   - **Rich Text: ENABLED** (critical!)
   - Wrapping: Enabled
   - Overflow: Truncate or Overflow
   - Auto Size: Optional

### 3. Attach the PlaneOverviewUI Script
1. Create a new **Empty GameObject** in the scene hierarchy
   - Rename it to `PlaneOverviewUI`
   - Attach the `PlaneOverviewUI.cs` script to it

2. In the Inspector, assign references:
   - **Overlay Panel**: Drag `PlaneOverviewPanel` here
   - **Crew Text**: Drag `CrewText` TextMeshPro
   - **Sections Text**: Drag `SectionsText` TextMeshPro
   - **Systems Text**: Drag `SystemsText` TextMeshPro
   - **General Text**: Drag `GeneralText` TextMeshPro

3. Configure colors (optional, defaults are set):
   - Excellent Color: Green (90-100% integrity)
   - Good Color: Yellow-Green (70-89%)
   - Fair Color: Yellow (50-69%)
   - Poor Color: Orange (25-49%)
   - Critical Color: Red (1-24%)
   - Destroyed Color: Gray (0%)

### 4. Create the Overview Button
1. In your main UI, create or find a **Button** to open the overview
   - Could be in a toolbar, menu, or HUD
   - Rename it to `OverviewButton`
   - Text: "OVERVIEW" or "STATUS" or similar

2. Attach the `OverviewButton.cs` script to this button
   - The script automatically hooks up the click event
   - **This is the button that opens the overview**

---

## How It Works

### Opening the Overview
- Click the button with `OverviewButton` script attached
- Calls `PlaneOverviewUI.Instance.OpenOverview()`
- Game pauses (`Time.timeScale = 0`)
- Panel becomes visible with generated status report

### Closing the Overview
- **Click anywhere** on screen (left, right, or middle mouse button)
- Press ESC key (optional)
- Game resumes (`Time.timeScale` restored)
- **No close button needed** - any click closes it

### Color Coding
**Crew & Section Names:**
- Names are colored by their health/integrity
- Easier to spot problems at a glance

**Health Status:**
- Healthy: Green
- Light: Yellow
- Serious: Orange
- Critical: Red
- Dead: Gray

**Integrity (Sections/Systems/Engines):**
- 90-100%: Green
- 70-89%: Yellow-Green
- 50-69%: Yellow
- 25-49%: Orange
- 1-24%: Red
- 0%: Gray

### Status Indicators
- `🔥` - Fire emoji for burning sections/engines
- `[X]` - Destroyed sections
- `[F]` - Feathered engines

---

## Layout Example

```
┌─────────────────────────────────────────────────────────────────┐
│                     PLANE OVERVIEW                              │
├──────────────┬──────────────┬──────────────┬────────────────────┤
│ CREW STATUS  │  SECTIONS    │ SYSTEMS &    │  GENERAL INFO      │
│              │              │ ENGINES      │                    │
│ Pilot        │ Nose 100%    │ Radio        │ Fuel: 2500 units   │
│   HEALTHY    │ Cockpit 85%  │   OPER (95%) │ Speed: 180 mph     │
│   Idle       │ Bombay 45%🔥 │ Navigator    │ Altitude: 25000 ft │
│              │ LeftWing 20% │   OPER (80%) │                    │
│ Bombardier   │ RightWing 5% │              │ SUPPLIES:          │
│   LIGHT      │              │ ENGINES:     │   Med Kits: 5      │
│   Performing │              │ Engine1      │   Repair Kits: 3   │
│              │              │   OPER (100%)│   Fire Ext: 2      │
└──────────────┴──────────────┴──────────────┴────────────────────┘
```

---

## Testing

1. **Start the game** and click the Overview button
2. **Verify pause**: Game should freeze (crew stops moving)
3. **Check columns**: All 4 columns should display data
4. **Test colors**: Damage sections/crew and verify color changes
5. **Click to close**: Any click anywhere should close and resume
6. **ESC key**: Should also close (optional feature)

---

## Customization

### Change Layout
- Edit `GenerateOverview()` method to reorder sections
- Comment out sections you don't want (e.g., hide General Info)
- Add new sections with custom data

### Change Colors
- Adjust color fields in Inspector
- Modify `GetCrewHealthColor()` or `GetIntegrityColor()` thresholds

### Change Text Formatting
- Edit the `StringBuilder` output in each `Generate*Section()` method
- Use Unity Rich Text tags: `<b>`, `<i>`, `<size=24>`, `<color=#FF0000>`

### Add More Data
- Extend `GenerateGeneralSection()` with mission progress, distance, etc.
- Add weather, altitude warnings, ammunition counts, etc.

---

## Notes

- **ScrollRect**: If the overview text is too long, add a ScrollRect component to the panel
- **Performance**: Overview generation is lightweight (only on open, not every frame)
- **Multiplayer**: Ensure Time.timeScale pause only affects local client if networked
- **Accessibility**: Consider adding keyboard navigation or text-to-speech support
