# Complete Setup Checklist - New Depth System

## ✅ All Scripts Created and Ready

### Core Scripts (Required)
- ✅ **DepthPointCloudGenerator.cs** - Main point cloud generator
- ✅ **PointCloudVisualizer.cs** - Updated for new system
- ✅ **DepthTestController.cs** - Updated for new system

### Optional Scripts
- ✅ **DepthTableScanner.cs** - Table filtering (Step 3)
- ✅ **DepthVisualizationDebugger.cs** - Enhanced visualization & debugging

### Removed (Old System)
- ❌ **DepthCaptureManager.cs** - Deleted (replaced by DepthPointCloudGenerator)

---

## Quick Setup Steps

### Step 1: Scene Setup

1. **Ensure you have:**
   - ✅ `OVRCameraRig` in scene
   - ✅ `EnvironmentDepthManager` component (auto-creates if missing)
   - ✅ Passthrough enabled in OVRManager

### Step 2: Add Core Components

1. **Create GameObject** (name it "DepthSystem"):
   - Create empty GameObject
   - Position at (0,0,0) or wherever needed

2. **Add Required Components:**
   - ✅ `EnvironmentDepthManager` (if not already in scene)
   - ✅ `DepthPointCloudGenerator`
   - ✅ `PointCloudVisualizer`

3. **Add Optional Components:**
   - ✅ `DepthTableScanner` (for table filtering)
   - ✅ `DepthVisualizationDebugger` (for debugging)
   - ✅ `DepthTestController` (for UI/controller input)

### Step 3: Configure DepthPointCloudGenerator

**Inspector Settings:**
- `Environment Depth Manager`: Auto-finds if null
- `Stride X`: 4 (default, adjust for performance)
- `Stride Y`: 4 (default, adjust for performance)
- `Max Depth`: 4.0 (Quest 3 max reliable depth)
- `Min Depth`: 0.1 (minimum valid depth)
- `Visualize`: ✅ true (for automatic visualization)
- `Point Material`: Leave empty (auto-creates) OR assign custom material
- `Point Size`: 0.008 (adjust if points too small/large)
- `Use Object Pooling`: ✅ true (better performance)
- `Max Points Per Frame`: 5000 (adjust for performance)

### Step 4: Configure PointCloudVisualizer

**Inspector Settings:**
- `Point Cloud Generator`: Drag `DepthPointCloudGenerator` component
- `Table Scanner`: (Optional) Drag `DepthTableScanner` if using
- `VR Camera`: Auto-finds if null
- `Point Material`: Leave empty OR assign custom material
- `Point Size`: 0.05 (adjust for visibility)
- `Near Color`: Blue (points close to camera)
- `Far Color`: Red (points far from camera)
- `Color Distance Range`: 2.0 (distance range for color gradient)
- `Max Points`: 20000 (limit for performance)
- `Use Mesh Rendering`: ✅ true (faster than spheres)

### Step 5: Configure DepthVisualizationDebugger (Optional but Recommended)

**Inspector Settings:**
- `Point Cloud Generator`: Drag `DepthPointCloudGenerator` component
- `VR Camera`: Auto-finds if null
- `Visualization Mode`: `Both` (for testing) or `StatisticsOnly` (for VR)
- `Show Depth Texture`: ✅ true (verify raw depth data)
- `Show Statistics`: ✅ true (real-time stats)
- `Show Gizmos`: ✅ true (see points in Scene view)
- `Gizmo Color`: Green (default)
- `Gizmo Size`: 0.02 (adjust if needed)
- `Max Gizmo Points`: 1000 (limit for performance)
- `Show On Screen Stats`: ✅ true
- `Font Size`: 20
- `Text Color`: White

### Step 6: Configure DepthTableScanner (Optional)

**Inspector Settings:**
- `Point Cloud Gen`: Drag `DepthPointCloudGenerator` component
- `Depth Manager`: Auto-finds if null
- `Min Height Above Table`: 0.01 (1cm)
- `Max Height Above Table`: 0.5 (50cm)
- `Table Bounds Margin`: 0.05 (5cm)
- `Visualize Filtered Points`: ✅ true
- `Filtered Point Material`: Leave empty OR assign
- `Filtered Point Size`: 0.01

### Step 7: Configure DepthTestController (Optional)

**Inspector Settings:**
- `Point Cloud Generator`: Drag `DepthPointCloudGenerator` component
- `Table Scanner`: (Optional) Drag `DepthTableScanner`
- `Visualizer`: Drag `PointCloudVisualizer` component
- `Use Controller Input`: ✅ true
- `Capture Button Input`: A Button (default)
- `Clear Button Input`: B Button (default)
- `Controller`: Right Touch (default)

---

## Testing Procedure

### 1. Initial Verification

1. **Enter Play Mode**
2. **Check Console:**
   - Should see: `"[DepthPointCloudGenerator] ✅ Depth system initialized"`
   - Should see: `"[PointCloudVisualizer] Using DepthPointCloudGenerator"`
3. **Check Statistics** (if DepthVisualizationDebugger enabled):
   - `Depth API: ✅ AVAILABLE`
   - `Point Count: > 0`
   - `Depth Texture: 320x240` (or similar)

### 2. Visual Verification

1. **Game View:**
   - Should see colored point cloud (blue = near, red = far)
   - Points should match real-world objects

2. **Scene View** (if Gizmos enabled):
   - Open Scene view (Window → General → Scene)
   - Should see colored spheres (gizmos)
   - Points should be in correct positions

3. **Statistics Overlay:**
   - Should show real-time point count
   - Should show depth API status
   - Should show depth texture info

### 3. Controller Testing (if DepthTestController enabled)

1. **Press A Button** (Right Controller):
   - Should trigger visualization
   - Should see points appear/update

2. **Press B Button** (Right Controller):
   - Should clear visualization

---

## Verification Checklist

### ✅ Depth API Working:
- [ ] Console shows depth system initialized
- [ ] Statistics show `Depth API: ✅ AVAILABLE`
- [ ] Statistics show `Point Count > 0`
- [ ] Statistics show `Depth Texture: NOT NULL`
- [ ] Points visible in Game view
- [ ] Points match real-world objects
- [ ] Colors change by distance (blue = near, red = far)

### ✅ Visualization Working:
- [ ] PointCloudVisualizer shows points
- [ ] Points are colored correctly
- [ ] DepthVisualizationDebugger shows statistics
- [ ] Gizmos appear in Scene view (if enabled)
- [ ] Depth texture preview shows (if enabled)

### ✅ Performance Acceptable:
- [ ] Frame rate stable (72-90 FPS on Quest)
- [ ] Point count reasonable (2000-5000 points)
- [ ] No stuttering or lag

---

## Troubleshooting

### No Points Generated

**Check:**
1. Is `EnvironmentDepthManager` in scene?
2. Is `EnvironmentDepthManager.IsDepthAvailable = true`?
3. Check console for errors
4. Verify depth permissions granted
5. Check depth range settings (minDepth, maxDepth)

### Points Not Visible

**Check:**
1. Is `DepthPointCloudGenerator.visualize = true`?
2. Is `PointCloudVisualizer` component present?
3. Is point material assigned or auto-created?
4. Try increasing `Point Size` in PointCloudVisualizer
5. Check if points are outside camera view

### Statistics Not Showing

**Check:**
1. Is `DepthVisualizationDebugger` component present?
2. Is `showStatistics = true`?
3. Is `showOnScreenStats = true`?
4. Are you in Play Mode?

### Gizmos Not Showing

**Check:**
1. Is Scene view open?
2. Is `showGizmos = true`?
3. Are Gizmos enabled in Scene view toolbar?
4. Is `visualizationMode` set to `Gizmos` or `Both`?

---

## Recommended Configuration

### For Development/Testing:
```
DepthPointCloudGenerator:
  - Stride: 4x4 (~2,500 points)
  - Visualize: true
  - Max Points: 5000

PointCloudVisualizer:
  - Use Mesh Rendering: true
  - Max Points: 20000

DepthVisualizationDebugger:
  - Mode: Both
  - Show all: true
  - Max Gizmo Points: 1000
```

### For Production/VR:
```
DepthPointCloudGenerator:
  - Stride: 4x4 or 8x8 (adjust for performance)
  - Visualize: false (use PointCloudVisualizer instead)
  - Max Points: 5000

PointCloudVisualizer:
  - Use Mesh Rendering: true
  - Max Points: 10000-20000

DepthVisualizationDebugger:
  - Mode: StatisticsOnly
  - Show Statistics: true
  - Show Gizmos: false (not needed in VR)
```

---

## Code Usage Examples

### Get Point Cloud:
```csharp
DepthPointCloudGenerator generator = GetComponent<DepthPointCloudGenerator>();
Vector3[] points = generator.GetPointCloud();
int count = generator.GetPointCount();
```

### Visualize Points:
```csharp
PointCloudVisualizer visualizer = GetComponent<PointCloudVisualizer>();
visualizer.VisualizeLatestCapture();
```

### Table Filtering:
```csharp
DepthTableScanner scanner = GetComponent<DepthTableScanner>();
scanner.ScanTableObjects();
List<Vector3> tablePoints = scanner.GetFilteredPoints();
```

### Check Depth Status:
```csharp
// Via DepthVisualizationDebugger statistics
// Or directly:
EnvironmentDepthManager depthManager = FindFirstObjectByType<EnvironmentDepthManager>();
bool isAvailable = depthManager.IsDepthAvailable;
```

---

## File Structure

```
Assets/Scripts/Scanning/
├── DepthPointCloudGenerator.cs          ✅ Core - Point generation
├── PointCloudVisualizer.cs              ✅ Core - Visualization
├── DepthTableScanner.cs                 ✅ Optional - Table filtering
├── DepthVisualizationDebugger.cs        ✅ Optional - Debugging
├── DepthTestController.cs               ✅ Optional - UI/Controller
├── DEPTH_CAPTURE_SETUP_GUIDE.md        📖 Setup guide
├── MIGRATION_COMPLETE.md                📖 Migration info
├── VISUALIZATION_GUIDE.md               📖 Visualization guide
├── POINT_MATERIAL_SETUP.md              📖 Material guide
└── COMPLETE_SETUP_CHECKLIST.md         📖 This file
```

---

## Next Steps

1. ✅ **Setup Complete** - All scripts ready
2. → **Test on Quest 3** - Build and deploy
3. → **Verify Depth API** - Check statistics
4. → **Adjust Settings** - Optimize for your use case
5. → **Proceed to Step 3** - Table filtering (if needed)
6. → **Proceed to Step 4** - Object detection/clustering

---

## Summary

**All code from the conversation is ready to use!**

- ✅ New scripts created and integrated
- ✅ Old scripts removed
- ✅ All components updated
- ✅ Documentation complete

**Just follow the setup steps above and you're ready to test!**


