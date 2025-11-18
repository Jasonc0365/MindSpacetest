# Depth Capture Test Setup Guide

## Quick Setup

### 1. Create Test GameObject
1. Create an empty GameObject in your scene (name it `DepthCaptureTest`)
2. Add the `DepthCaptureTest` component to it
3. Add the `DepthCapture` component to the same GameObject (or another GameObject)

### 2. Assign References
- **Depth Capture**: Drag the GameObject with `DepthCapture` component (or leave empty to auto-find)
- **Main Camera**: 
  - **For VR (Quest)**: Leave empty - script will auto-find camera from `OVRCameraRig.centerEyeAnchor`
  - **OR** manually drag the camera from `OVRCameraRig` → `CenterEyeAnchor` → Camera component
  - **OR** leave empty to use `Camera.main` (if tagged as MainCamera)

### 3. Optional: Point Visualization Material
- Create a material for point cloud visualization (or use default green)
- Assign to **Point Material** field

### 4. Optional: Status UI Display
- Create a Canvas with TextMeshPro text element
- Position it where you can see it in VR (e.g., top-left corner)
- Assign the TextMeshPro component to **Status Text** field
- This will show real-time depth status and point counts

## Controller Controls

### Right Controller:
- **A Button** - Capture depth frame and visualize point cloud
- **B Button** - Clear visualization

## What It Does

1. **Checks Depth Availability**: Verifies EnvironmentDepthManager is working
2. **Captures Depth Frame**: Gets depth texture and converts to float array
3. **Projects to 3D**: Converts depth pixels to 3D world coordinates
4. **Visualizes Points**: Shows point cloud as green spheres (or custom prefab)

## Testing Checklist

- [ ] DepthCapture component is in scene
- [ ] EnvironmentDepthManager is available (auto-creates if missing)
- [ ] Press Right Controller A button
- [ ] Check Console for: `"Captured X 3D points from depth frame"`
- [ ] See green spheres appear in scene (point cloud visualization)
- [ ] Press Right Controller B button to clear

## Troubleshooting

**"Depth not available yet":**
- Wait a few seconds for EnvironmentDepthManager to initialize
- Ensure you're on Quest 3/Pro (depth API requires Quest 3+)

**"Depth texture not available":**
- Check that EnvironmentDepthManager is enabled
- Try moving around to trigger depth updates

**No points visualized:**
- Check that you're pointing camera at objects/surfaces
- Adjust `minDepth` and `maxDepth` in DepthCapture settings
- Increase `maxPointsToShow` in test script

**Performance issues:**
- Reduce `maxPointsToShow` (default: 1000)
- Increase point visualization step size
- Disable `showPointCloud` if not needed

## Settings Explained

- **Show Point Cloud**: Enable/disable visualization
- **Point Size**: Size of visualization spheres (0.01-0.1m)
- **Max Points To Show**: Limit for performance (0-10000)
- **Auto Capture On Start**: Automatically capture when scene starts

