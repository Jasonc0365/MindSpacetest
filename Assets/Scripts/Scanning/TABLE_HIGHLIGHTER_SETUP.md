# Table Highlighter Setup Guide

## Overview

The `TableHighlighter` component replaces the Oculus EffectMesh with a custom solution that:
- Generates world-locked highlighting for MRUK table surfaces
- Only highlights the top surface of tables
- Provides customizable appearance (color, opacity, glow)
- Supports animation (pulse effect)
- Ready for scanning workflow integration

## Quick Setup

### Option 1: Automatic Setup (Recommended)

1. Add `TableHighlighterManager` component to any GameObject in your scene
2. The manager will automatically create a `TableHighlighter` component if needed
3. Tables will be highlighted automatically when MRUK scene loads

### Option 2: Manual Setup

1. Create an empty GameObject named "TableHighlighter"
2. Add `TableHighlighter` component to it
3. Configure appearance settings in Inspector:
   - **Highlight Color**: Color of the highlight (default: green)
   - **Opacity**: Transparency level (0-1)
   - **Glow Intensity**: Intensity of glow effect (0-2)
   - **Height Offset**: Distance above table surface (default: 0.005m)
   - **Enable Pulse Animation**: Toggle pulse effect
   - **Pulse Speed**: Speed of pulse animation
   - **Pulse Amplitude**: Amount of opacity variation

### Option 3: Using Custom Shader

1. Create a Material using the `Custom/TableSurfaceHighlight` shader
2. Assign the material to the `Table Highlight Material` field in TableHighlighter
3. Customize shader properties:
   - Color
   - Glow Intensity
   - Pulse settings

## Testing

### Using TableHighlighterTest Component

1. Add `TableHighlighterTest` component to any GameObject
2. The test will automatically run when scene starts
3. Manual controls:
   - **Space**: Cycle through test colors
   - **C**: Manually create highlights
   - **X**: Clear all highlights

### Verification Checklist

- [ ] TableHighlighter component exists in scene
- [ ] MRUK scene is loaded (check console for "MRUK scene loaded")
- [ ] Tables are detected by MRUK (check console for table count)
- [ ] Green highlight appears on table surfaces
- [ ] Highlights are world-locked (don't move when you move)
- [ ] Highlights only show on top surface

## Replacing EffectMesh

### Step 1: Remove EffectMesh Dependency

If you're using `TableTopModifier`, you can now:
- Remove the EffectMesh reference
- Or keep it but use TableHighlighter instead

### Step 2: Disable EffectMesh

In your scene:
1. Find the GameObject with EffectMesh component
2. Disable it or remove it

### Step 3: Use TableHighlighter

Add TableHighlighterManager to your scene - it will handle everything automatically.

## Customization

### Changing Appearance

```csharp
// In code:
tableHighlighter.UpdateHighlightAppearance(new Color(0, 0, 1, 0.5f), 0.5f); // Blue, 50% opacity
```

### Showing Scanning Progress

```csharp
// Future feature - show coverage:
tableHighlighter.ShowScanningProgress(tableAnchor, 0.75f); // 75% coverage
```

### Accessing Highlights

```csharp
// Get highlight GameObject for a table:
GameObject highlight = tableHighlighter.GetTableHighlight(tableAnchor);

// Get all highlighted tables:
var highlightedTables = tableHighlighter.HighlightedTables;
```

## Troubleshooting

### No Highlights Appearing

1. **Check MRUK is loaded:**
   - Look for "MRUK scene loaded" in console
   - Verify MRUK.Instance is not null

2. **Check tables are detected:**
   - Look for "Found X table(s)" in console
   - Verify tables have TABLE label in MRUK

3. **Check TableHighlighter is enabled:**
   - Verify component is enabled
   - Check GameObject is active

4. **Check material/shader:**
   - Verify material is assigned or default shader works
   - Check shader compiles without errors

### Highlights Not World-Locked

- Highlights are parented to MRUK anchor transforms
- If anchors move, highlights move with them
- This is correct behavior for world-locking

### Highlights Too Bright/Dark

- Adjust **Opacity** in Inspector (0-1)
- Adjust **Glow Intensity** (0-2)
- Change **Highlight Color** alpha channel

### Performance Issues

- TableHighlighter uses minimal geometry (one quad per table)
- If performance is poor, check:
  - Number of tables in scene
  - Shader complexity
  - Other scene elements

## Integration with Scanning Workflow

The TableHighlighter is designed to integrate with the scanning workflow:

1. **Step 0**: TableHighlighter replaces EffectMesh ✅
2. **Step 1+**: Can be enhanced to show:
   - Scanning progress
   - Coverage visualization
   - Depth point visualization
   - Gaussian splat preview

## Files Created

- `Assets/Scripts/Scanning/TableHighlighter.cs` - Main component
- `Assets/Shaders/TableSurfaceHighlight.shader` - Custom shader
- `Assets/Scripts/Scanning/TableHighlighterManager.cs` - Manager component
- `Assets/Scripts/Scanning/TableHighlighterTest.cs` - Test component

## Next Steps

After verifying TableHighlighter works:
1. ✅ Remove EffectMesh dependency
2. ✅ Test on Quest device
3. ✅ Proceed to Step 1: Depth Capture

