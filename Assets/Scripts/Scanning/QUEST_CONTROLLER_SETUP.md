# Quest Controller Setup - Depth Scanning Scripts

## ✅ All Scripts Updated for Quest Controller

All depth scanning scripts now support Quest controller input with keyboard fallback for editor testing.

---

## Controller Controls

### DepthVisualizationDebugger Controls

| Button | Action | Description |
|--------|--------|-------------|
| **X/Y Button** (Right) | Cycle Visualization Mode | Switch between MeshPoints, Gizmos, Both, StatisticsOnly |
| **Menu Button** (Right) | Toggle Gizmos | Show/hide gizmos in Scene view |
| **Left Thumbstick Click** | Toggle Statistics | Show/hide on-screen statistics |
| **Right Thumbstick Click** | Toggle Depth Texture | Show/hide depth texture preview |

**Keyboard Fallback (Editor):**
- `SPACE` = Cycle mode
- `G` = Toggle Gizmos
- `S` = Toggle Statistics
- `D` = Toggle Depth Texture

### DepthTestController Controls

| Button | Action | Description |
|--------|--------|-------------|
| **A Button** (Right) | Capture/Visualize | Trigger point cloud visualization |
| **B Button** (Right) | Clear | Clear current visualization |

---

## Script Versions (Working)

### ✅ Core Scripts

1. **DepthPointCloudGenerator.cs**
   - ✅ Matches depth-scanning-implementation.md guide exactly
   - ✅ Uses shader globals for depth texture access
   - ✅ Proper reprojection matrix pipeline
   - ✅ `visualize = false` by default (uses PointCloudVisualizer)

2. **PointCloudVisualizer.cs**
   - ✅ Auto-updates visualization
   - ✅ Auto-creates material if not assigned
   - ✅ Supports mesh and sphere rendering
   - ✅ Works with DepthPointCloudGenerator

3. **DepthVisualizationDebugger.cs**
   - ✅ Quest controller support added
   - ✅ Haptic feedback on button presses
   - ✅ Multiple visualization modes
   - ✅ On-screen statistics
   - ✅ Depth texture preview
   - ✅ Gizmos for Scene view debugging

4. **DepthTestController.cs**
   - ✅ Already had Quest controller support
   - ✅ A button = Capture/Visualize
   - ✅ B button = Clear

---

## Setup Instructions

### Step 1: Add Components

1. Create or select your "DepthSystem" GameObject
2. Add these components:
   - ✅ `EnvironmentDepthManager` (if not already present)
   - ✅ `DepthPointCloudGenerator`
   - ✅ `PointCloudVisualizer`
   - ✅ `DepthVisualizationDebugger` (optional, for debugging)
   - ✅ `DepthTestController` (optional, for manual control)

### Step 2: Configure DepthVisualizationDebugger

**Inspector Settings:**
- `Point Cloud Generator`: Auto-finds or assign manually
- `VR Camera`: Auto-finds from OVRCameraRig
- `Use Controller Input`: ✅ true (enables Quest controller)
- `Cycle Mode Button`: X/Y button (Button.Three)
- `Toggle Gizmos Button`: Menu button (Button.Four)
- `Toggle Stats Button`: Primary Thumbstick
- `Toggle Depth Texture Button`: Secondary Thumbstick
- `Controller`: Right Touch (RTouch)

### Step 3: Test in VR

1. Build and deploy to Quest 3
2. Use right controller:
   - Press **X/Y** to cycle visualization modes
   - Press **Menu** to toggle gizmos
   - Press **Left Thumbstick** to toggle statistics
   - Press **Right Thumbstick** to toggle depth texture preview
3. Use **A button** to manually trigger visualization (if using DepthTestController)

---

## Features

### Haptic Feedback
- All button presses provide haptic feedback
- Different intensities for different actions
- Helps confirm input registration

### Keyboard Fallback
- All controller inputs have keyboard equivalents
- Works in Unity Editor for testing
- Automatically disabled when `useControllerInput = false`

### Visualization Modes
1. **MeshPoints**: Standard mesh rendering (Game view)
2. **Gizmos**: Scene view visualization (Editor)
3. **Both**: Mesh + Gizmos together
4. **StatisticsOnly**: Just stats, no rendering

---

## Troubleshooting

### Controller Not Working
- ✅ Check `useControllerInput = true` in inspector
- ✅ Verify OVRInput is available (Quest build only)
- ✅ Check controller is connected and tracked
- ✅ Use keyboard fallback in editor

### No Haptic Feedback
- ✅ Haptics only work on Quest device
- ✅ Check controller is properly tracked
- ✅ Verify OVRInput is initialized

### Buttons Not Responding
- ✅ Check button assignments in inspector
- ✅ Verify controller is selected (RTouch/LTouch)
- ✅ Try keyboard fallback to test functionality

---

## Quick Reference

### DepthVisualizationDebugger
- **X/Y Button**: Cycle visualization modes
- **Menu Button**: Toggle gizmos
- **L Thumbstick**: Toggle statistics
- **R Thumbstick**: Toggle depth texture

### DepthTestController
- **A Button**: Capture/Visualize
- **B Button**: Clear visualization

---

## Notes

- All scripts are at the working version from when DepthVisualizationDebugger was created
- DepthPointCloudGenerator matches the guide exactly
- Controller input works on Quest device, keyboard fallback for editor
- Haptic feedback provides user confirmation

