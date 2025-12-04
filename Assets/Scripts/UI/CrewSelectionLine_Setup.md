# Crew Selection Line - Setup Guide

## Overview
The `CrewSelectionLine` component draws a visual line from a crew member's UI button to their sprite on the plane, making it easy to see which crew member is selected.

## Features
- Automatically tracks selected crew from `OrdersUIController`
- Draws line from button to sprite using Unity's `LineRenderer`
- Optional pulsing animation for visual feedback
- Configurable color, width, and appearance

## Unity Inspector Setup

### Step 1: Create GameObject
1. In your Canvas hierarchy, create a new empty GameObject
2. Name it `CrewSelectionLine`
3. Add the `CrewSelectionLine` component to it

### Step 2: Add LineRenderer
The component will automatically add a `LineRenderer` if one isn't present.

### Step 3: Configure References
In the Inspector, set these fields:

**References:**
- `Orders Controller`: Drag your `OrdersUIController` instance here
- `Crew Buttons Parent`: The RectTransform that contains all your crew button UI elements (likely the parent of all `CrewStatusIndicator` components)
- `Crew Sprites Parent`: The RectTransform that contains all your crew sprite views (the parent of all `CrewSpriteView` components)

**Line Settings:**
- `Line Color`: Default is cyan (0, 255, 255) with 80% opacity - adjust to match your UI theme
- `Line Width`: Default is 3 pixels - increase for thicker line
- `Line Material`: **IMPORTANT** - You need a UI-compatible material. See Material Setup below.
- `Sorting Order`: Default is 100 (renders above plane, below UI buttons)

**Animation (Optional):**
- `Animate Line`: Check to enable pulsing animation
- `Pulse Speed`: How fast the line pulses (default: 2)
- `Min Alpha` / `Max Alpha`: Range of transparency during pulse (0.4 to 1.0)

### Step 4: Material Setup

**Option A: Create a Simple UI Material**
1. Right-click in Project window → Create → Material
2. Name it `CrewSelectionLineMaterial`
3. Set Shader to `Sprites/Default` or `UI/Default`
4. Drag this material into the `Line Material` field

**Option B: Use Unity's Default Sprite Material**
1. Find a sprite material in your project (any UI sprite uses one)
2. Duplicate it and rename to `CrewSelectionLineMaterial`
3. Drag into the `Line Material` field

### Step 5: Sorting Layer Setup (If Needed)
The script uses sorting layer "UI" by default. If you don't have a "UI" sorting layer:

1. Go to Edit → Project Settings → Tags and Layers
2. Add "UI" to your Sorting Layers list
3. Or, modify line 60 in `CrewSelectionLine.cs` to use an existing layer:
   ```csharp
   lineRenderer.sortingLayerName = "Default"; // Change to your layer
   ```

## How It Works

### Detection System
1. Each frame, checks `OrdersUIController.SelectedCrewId`
2. Searches for a `CrewStatusIndicator` component with matching `crewId` (the button)
3. Searches for a `CrewSpriteView` component with matching `crewId` (the sprite)
4. Draws line between their world positions

### Requirements
- Crew buttons must have `CrewStatusIndicator` component with `crewId` set
- Crew sprites must have `CrewSpriteView` component with `crewId` set
- Both must be children of the respective parent transforms you configured

## Customization

### Change Line Appearance
Adjust these in Inspector:
- **Color**: Change `lineColor` for different theme
- **Width**: Increase `lineWidth` for thicker line
- **Material**: Use custom shader for special effects (glow, dashed, etc.)

### Disable Animation
Uncheck `Animate Line` for static line

### Different Rendering Mode
If your Canvas uses different rendering mode:
- **Screen Space - Overlay**: Should work automatically
- **Screen Space - Camera**: Should work automatically
- **World Space**: May need position adjustments (untested)

### Custom Shader Effects
For advanced effects (dashed line, glow, etc.):
1. Create a custom shader/material
2. Apply to `Line Material` field
3. Adjust `Sorting Order` if z-fighting occurs

## Troubleshooting

### Line Not Appearing
- **Check References**: Ensure `ordersController`, `crewButtonsParent`, and `crewSpritesParent` are assigned
- **Check Material**: Line needs a material to render - assign one in Inspector
- **Check Sorting**: Adjust `sortingOrder` if line is behind other UI elements
- **Check Camera**: If using ScreenSpace-Camera canvas, ensure canvas has a camera assigned

### Line in Wrong Position
- **Canvas Mode**: Verify canvas rendering mode matches your setup
- **Parent Transforms**: Ensure you've assigned the correct parent transforms
- **Z-Position**: LineRenderer uses world space - may need camera adjustment

### Line Not Updating
- **Component Active**: Ensure the `CrewSelectionLine` GameObject is active
- **Crew IDs Match**: Verify `CrewStatusIndicator.crewId` matches `CrewSpriteView.crewId`
- **Selection Working**: Test that `OrdersUIController.SelectedCrewId` is being set

### Performance Concerns
The script is very lightweight:
- Only runs when crew is selected
- Uses simple `GetComponentsInChildren` lookups (cached internally by Unity)
- No physics or raycasts involved
- Animation is simple sin wave calculation

## API Reference

### Public Methods

```csharp
// Manually enable/disable the line
public void SetLineEnabled(bool enabled)
```

### Example Usage
```csharp
// From another script, hide the line temporarily
var selectionLine = FindObjectOfType<CrewSelectionLine>();
selectionLine.SetLineEnabled(false);
```

## Advanced: Multiple Lines
To show lines for multiple crew (e.g., all selected crew in a squad):
1. Duplicate the `CrewSelectionLine` GameObject
2. Modify the script to track different crew IDs
3. Or create a manager script that spawns multiple line instances

## Tips
- **Color Contrast**: Choose a line color that stands out against your plane sprite
- **Thin Lines**: Start with thin lines (2-4 pixels) for cleaner look
- **Subtle Animation**: Lower pulse speed (1-1.5) for less distraction
- **Z-Fighting**: If line flickers, adjust `sortingOrder` or use different sorting layer
