# Simple Depth Capture Setup Guide

## Overview

Three simplified scripts for depth capture and point cloud visualization:
1. **DepthCaptureManager.cs** - Captures depth data on demand
2. **PointCloudVisualizer.cs** - Visualizes depth as 3D point cloud
3. **DepthTestController.cs** - Handles controller input (A = scan, B = clear)

## Setup Instructions

### Step 1: Scene Setup

1. Ensure you have `OVRCameraRig` in your scene at (0, 0, 0)
2. Enable passthrough in OVRManager settings
3. Ensure Scene Understanding is enabled

### Step 2: Add Components

1. Create an empty GameObject called "DepthSystem"
2. Add `DepthCaptureManager` component
3. Add `PointCloudVisualizer` component
4. Add `DepthTestController` component
5. In `PointCloudVisualizer`, assign the `DepthCaptureManager` reference

### Step 3: EnvironmentDepthManager

The `EnvironmentDepthManager` will be auto-created by `DepthCaptureManager` if not present.
Alternatively, you can manually add it to the scene.

### Step 4: Build Settings

1. Switch to Android platform
2. Player Settings:
   - Minimum API Level: Android 10.0 (API 29) or higher
   - XR Plugin Management: Oculus

### Step 5: Quest Setup

Before running on Quest:
```bash
# Enable experimental features (connect Quest via USB)
adb shell setprop debug.oculus.experimentalEnabled 1
adb reboot
```

Also ensure:
- Space Setup is completed on Quest
- Spatial data permission is granted

## Usage

### Controls

- **Right Controller A Button** - Capture depth and visualize point cloud
- **Right Controller B Button** - Clear visualization

### How It Works

1. Press **A button** on right controller
2. `DepthTestController` calls `DepthCaptureManager.CaptureDepthFrame()`
3. Depth data is captured from the depth texture
4. Points are converted to world space using reprojection matrices
5. `PointCloudVisualizer` displays the points as a colored point cloud
6. Press **B button** to clear the visualization

## Troubleshooting

### "Depth not available"

**Solutions:**
1. Run Space Setup on Quest (Settings → Physical Space → Space Setup)
2. Enable experimental features: `adb shell setprop debug.oculus.experimentalEnabled 1`
3. Check OVRManager settings:
   - Quest Features → Passthrough: Enabled
   - Quest Features → Scene Support: Required
4. Grant USE_SCENE permission

### No points generated

**Check:**
- Camera is not too close to walls (< 0.3m)
- Looking at surfaces with good lighting
- Depth range settings (minDepth = 0.1m, maxDepth = 4.0m)

### Points in wrong location

**Fix:**
- Ensure `OVRCameraRig` is at (0, 0, 0)
- Check that `centerEyeAnchor` is being used (not leftEyeAnchor)

### Performance issues

**Optimize:**
- Increase `pointStride` in DepthCaptureManager (try 8 instead of 4)
- Reduce `maxPoints` in PointCloudVisualizer (try 5000 instead of 10000)
- Ensure `useMeshRendering` is true (much faster than spheres)

## Script Details

### DepthCaptureManager

- Captures depth on demand (not continuous)
- Uses shader globals to access depth texture
- Converts depth to world space using reprojection matrices
- Filters points by depth range

### PointCloudVisualizer

- Visualizes points as mesh (fast) or spheres (slow)
- Colors points by distance (blue = near, red = far)
- Limits points for performance

### DepthTestController

- Simple input handler for controller buttons
- Right Controller A = Scan
- Right Controller B = Clear

## Expected Results

When working correctly:
- Blue points close to you
- Red points far away
- Points form recognizable shapes (walls, furniture, hands)
- Smooth performance (72+ FPS on Quest 3)

## Next Steps

Once this works:
- ✅ You have depth capture working
- ✅ You can generate point clouds
- ✅ Ready for Step 3: Filter points to table surface

