# Enhanced Point Cloud Visualization Guide

## Overview

There are **multiple ways** to visualize your point cloud to verify the Depth API is working correctly. This guide covers all visualization options.

---

## Visualization Options

### 1. **Standard Mesh Point Rendering** (Current)
- Uses `PointCloudVisualizer` component
- Renders points as mesh with vertex colors
- Good for runtime visualization
- **Pros:** Fast, good for VR
- **Cons:** Limited to Game view

### 2. **Gizmos Visualization** (New - Best for Debugging)
- Uses `DepthVisualizationDebugger` component
- Shows points in **Scene view** as colored spheres
- Visible in editor while playing
- **Pros:** See points in Scene view, color-coded by distance
- **Cons:** Only visible in Scene view, not Game view

### 3. **On-Screen Statistics** (New - Best for Verification)
- Real-time statistics overlay
- Shows point count, depth API status, bounds
- **Pros:** Immediate feedback, no visualization needed
- **Cons:** Text overlay only

### 4. **Depth Texture Preview** (New - Best for Raw Data)
- Shows the raw depth texture from Quest
- See actual depth data before conversion
- **Pros:** Verify depth data is coming through
- **Cons:** Shows raw texture, not 3D points

### 5. **Combined Visualization** (Recommended)
- Use multiple methods together
- Get complete picture of depth API status

---

## Setup: Enhanced Visualization Debugger

### Step 1: Add Component

1. Select your DepthSystem GameObject (or create new one)
2. Add `DepthVisualizationDebugger` component
3. Assign references:
   - `Point Cloud Generator`: Drag `DepthPointCloudGenerator`
   - `VR Camera`: Auto-finds, or drag manually

### Step 2: Configure Settings

**Visualization Modes:**
- `MeshPoints`: Standard mesh rendering (Game view)
- `Gizmos`: Scene view visualization (Editor)
- `Both`: Both mesh and gizmos
- `StatisticsOnly`: Just show stats

**Display Options:**
- ✅ `Show Depth Texture`: Display raw depth texture
- ✅ `Show Statistics`: Show on-screen stats
- ✅ `Show Gizmos`: Show points in Scene view

### Step 3: Test

1. **Enter Play Mode**
2. **Open Scene View** (Window → General → Scene)
3. **Look for:**
   - Green spheres in Scene view (Gizmos)
   - On-screen statistics
   - Depth texture preview (if enabled)

---

## Keyboard Controls

While in Play Mode:

| Key | Action |
|-----|--------|
| **SPACE** | Cycle visualization modes |
| **G** | Toggle Gizmos on/off |
| **S** | Toggle Statistics on/off |
| **D** | Toggle Depth texture preview |

---

## What to Look For (Verification Checklist)

### ✅ Depth API Working Correctly:

1. **Statistics Show:**
   - ✅ `Depth API: AVAILABLE` (green)
   - ✅ `Point Count: > 0` (e.g., 2500 points)
   - ✅ `Depth Texture: 320x240` (or similar)
   - ✅ `Avg Distance: 0.5-3.0m` (reasonable range)

2. **Gizmos Show:**
   - ✅ Colored spheres in Scene view
   - ✅ Points match real-world objects
   - ✅ Colors change by distance (blue = near, red = far)

3. **Depth Texture Shows:**
   - ✅ Grayscale image (white = near, black = far)
   - ✅ Updates in real-time
   - ✅ Shows objects in view

4. **Point Cloud:**
   - ✅ Points appear where objects are
   - ✅ Points match camera view
   - ✅ No random scattered points

### ❌ Depth API NOT Working:

1. **Statistics Show:**
   - ❌ `Depth API: NOT AVAILABLE` (red)
   - ❌ `Point Count: 0`
   - ❌ `Depth Texture: NULL`

2. **No Visualization:**
   - ❌ No gizmos in Scene view
   - ❌ No points in Game view
   - ❌ Empty depth texture

---

## Visualization Modes Explained

### Mode 1: MeshPoints (Default)
- Standard point cloud rendering
- Visible in Game view
- Uses `PointCloudVisualizer`
- Best for: Runtime visualization in VR

### Mode 2: Gizmos (Best for Debugging)
- Unity Gizmos in Scene view
- Color-coded by distance
- Visible while editing
- Best for: Verifying point positions

### Mode 3: Both
- Combines mesh and gizmos
- See points in both views
- Best for: Complete visualization

### Mode 4: StatisticsOnly
- Just show stats, no rendering
- Minimal performance impact
- Best for: Performance testing

---

## Statistics Display

The on-screen statistics show:

```
=== DEPTH API DEBUG ===
Point Count: 2,543
Center: (0.12, 1.45, -0.32)
Avg Distance: 1.85m
Bounds: 2.1 x 1.8 x 1.9
Depth API: ✅ AVAILABLE
Depth Texture: 320x240
Mode: Both
Press SPACE to toggle visualization
```

**What Each Stat Means:**
- **Point Count**: Number of points generated (should be > 0)
- **Center**: Average position of all points
- **Avg Distance**: Average distance from camera (should be 0.5-4m)
- **Bounds**: Size of point cloud bounding box
- **Depth API**: Status (✅ = working, ❌ = not working)
- **Depth Texture**: Resolution of depth data

---

## Depth Texture Preview

Shows the **raw depth texture** from Quest:
- **White** = Near objects (close to camera)
- **Black** = Far objects (distant)
- **Gray** = Mid-range objects

**Use this to verify:**
- Depth data is being captured
- Objects are detected
- Texture updates in real-time

---

## Troubleshooting Visualization

### No Points Visible

**Check:**
1. Is `DepthPointCloudGenerator` generating points?
   - Check statistics: `Point Count > 0`
2. Is visualization enabled?
   - `DepthPointCloudGenerator.visualize = true`
   - `PointCloudVisualizer` component present
3. Are points in view?
   - Move camera to see objects
   - Check depth range (0.1m - 4m)

### Gizmos Not Showing

**Check:**
1. Is Scene view open?
   - Window → General → Scene
2. Are Gizmos enabled?
   - Scene view toolbar → Gizmos button
3. Is `showGizmos = true`?
   - In `DepthVisualizationDebugger` component

### Statistics Not Showing

**Check:**
1. Is `showStatistics = true`?
2. Is `showOnScreenStats = true`?
3. Are you in Play Mode?
   - Statistics only show during play

### Depth Texture Preview Not Showing

**Check:**
1. Is `showDepthTexturePreview = true`?
2. Is depth texture available?
   - Check statistics: `Depth Texture: NOT NULL`
3. Is `EnvironmentDepthManager` working?
   - Check: `Depth API: AVAILABLE`

---

## Recommended Setup for Testing

### For Initial Verification:

1. **Add `DepthVisualizationDebugger`**
2. **Set Visualization Mode**: `Both`
3. **Enable All Displays**:
   - ✅ Show Depth Texture
   - ✅ Show Statistics
   - ✅ Show Gizmos
4. **Enter Play Mode**
5. **Open Scene View** (Window → General → Scene)
6. **Look for:**
   - Statistics in Game view
   - Gizmos in Scene view
   - Depth texture preview

### For Runtime (VR):

1. **Use `PointCloudVisualizer`** (standard mesh rendering)
2. **Optional**: Add `DepthVisualizationDebugger` for stats only
3. **Set Mode**: `StatisticsOnly` (no gizmos in VR)
4. **Enable**: `Show Statistics` only

---

## Quick Test Procedure

1. **Setup:**
   - Add `DepthVisualizationDebugger` to scene
   - Assign `DepthPointCloudGenerator` reference

2. **Test:**
   - Enter Play Mode
   - Press **SPACE** to cycle modes
   - Check statistics display
   - Open Scene view, look for gizmos

3. **Verify:**
   - ✅ Statistics show point count > 0
   - ✅ Depth API status is AVAILABLE
   - ✅ Gizmos appear in Scene view
   - ✅ Points match real-world objects

---

## Performance Tips

- **Gizmos**: Limit `maxGizmoPoints` (default: 1000) for better performance
- **Statistics**: Always enabled, minimal impact
- **Depth Texture Preview**: Can be disabled if not needed
- **Visualization Mode**: Use `StatisticsOnly` for best performance

---

## Summary

**Best for Verification:**
1. ✅ `DepthVisualizationDebugger` with `Both` mode
2. ✅ Statistics overlay (immediate feedback)
3. ✅ Gizmos in Scene view (see actual points)
4. ✅ Depth texture preview (verify raw data)

**Best for Runtime:**
1. ✅ `PointCloudVisualizer` (standard mesh)
2. ✅ Optional statistics overlay
3. ✅ Disable gizmos (VR only)

Add `DepthVisualizationDebugger` to get comprehensive visualization and debugging tools!

