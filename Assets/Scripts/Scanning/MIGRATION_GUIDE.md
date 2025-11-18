# Migration Guide: Old vs New Depth Scripts

## Overview

You now have **two sets of depth scanning scripts** that can work together or independently:

### Old Scripts (Currently in Your Scene)
- **`DepthCaptureManager`** - Manual depth frame capture
- **`PointCloudVisualizer`** - Visualizes points from DepthCaptureManager
- **`DepthTestController`** - UI/Controller input

### New Scripts (Production-Ready)
- **`DepthPointCloudGenerator`** - Automatic point cloud generation using proper reprojection matrices
- **`DepthTableScanner`** - Combined point cloud + table filtering

## Key Differences

| Feature | Old Scripts | New Scripts |
|---------|------------|-------------|
| **Point Generation** | Manual (`CaptureDepthFrame()`) | Automatic (every frame in `Update()`) |
| **Transformation** | FOV-based calculation | Proper reprojection matrix pipeline |
| **Accuracy** | Good | **Better** (more accurate) |
| **Performance** | Manual control | Optimized with object pooling |
| **Table Filtering** | Separate (`TablePointFilter`) | Integrated (`DepthTableScanner`) |
| **Setup** | More steps | Simpler |

## Integration Options

### Option 1: Keep Both (Recommended for Testing)

Both systems can coexist. They use the same `EnvironmentDepthManager` but don't interfere with each other.

**Setup:**
- Keep your existing `DepthCaptureManager` + `PointCloudVisualizer` setup
- Add `DepthPointCloudGenerator` to a different GameObject
- Compare results to verify accuracy

### Option 2: Migrate to New Scripts (Recommended for Production)

Replace old scripts with new ones for better accuracy and simpler setup.

**Migration Steps:**

1. **Remove from Scene:**
   - Remove `DepthCaptureManager` component
   - Keep `PointCloudVisualizer` (we'll update it to work with new script)

2. **Add New Scripts:**
   - Add `DepthPointCloudGenerator` component
   - Add `DepthTableScanner` component (optional, for table filtering)

3. **Update PointCloudVisualizer:**
   - The updated version can work with both old and new scripts
   - Assign `DepthPointCloudGenerator` instead of `DepthCaptureManager`

4. **Update DepthTestController:**
   - Optionally update to work with new scripts (or create new controller)

### Option 3: Hybrid Approach

Use new scripts for automatic generation, keep old scripts for manual capture when needed.

## Code Comparison

### Old Approach (DepthCaptureManager)
```csharp
// Manual capture
depthCapture.CaptureDepthFrame();
Vector3[] points = depthCapture.ConvertDepthToPointCloud();
visualizer.VisualizeLatestCapture();
```

### New Approach (DepthPointCloudGenerator)
```csharp
// Automatic generation (runs every frame)
Vector3[] points = pointCloudGen.GetPointCloud(); // Already generated
// Or for table filtering:
tableScanner.ScanTableObjects();
List<Vector3> filteredPoints = tableScanner.GetFilteredPoints();
```

## Accuracy Comparison

**Old Script (`DepthCaptureManager`):**
- Uses FOV and aspect ratio to calculate view space
- Manual camera transform application
- Works but less accurate for edge cases

**New Script (`DepthPointCloudGenerator`):**
- Uses proper reprojection matrices from Quest SDK
- Handles all coordinate transformations correctly
- More accurate, especially at edges and different distances

## Performance Comparison

| Script | Points/Frame | Update Method |
|--------|-------------|----------------|
| `DepthCaptureManager` | Manual (on-demand) | Manual trigger |
| `DepthPointCloudGenerator` | Automatic (every frame) | Update() loop |

**Recommendation:** For real-time scanning, use `DepthPointCloudGenerator` with appropriate stride (4x4 = ~2,500 points).

## Migration Checklist

### If Migrating to New Scripts:

- [ ] Backup your current scene
- [ ] Add `DepthPointCloudGenerator` component
- [ ] Test point cloud generation
- [ ] Compare accuracy with old script
- [ ] Update `PointCloudVisualizer` to use new script (optional)
- [ ] Remove `DepthCaptureManager` if satisfied
- [ ] Add `DepthTableScanner` for table filtering (optional)

### If Keeping Both:

- [ ] Add `DepthPointCloudGenerator` to separate GameObject
- [ ] Test both systems side-by-side
- [ ] Use old for manual capture, new for automatic
- [ ] Gradually migrate features to new scripts

## Recommendation

**For new projects:** Use `DepthPointCloudGenerator` + `DepthTableScanner`

**For existing projects:** 
1. Test new scripts alongside old ones
2. Verify accuracy improvement
3. Migrate gradually or keep both for different use cases

## Example: Using Both Together

```csharp
public class HybridDepthSystem : MonoBehaviour
{
    [SerializeField] private DepthCaptureManager oldSystem; // Manual capture
    [SerializeField] private DepthPointCloudGenerator newSystem; // Automatic
    
    void Update()
    {
        // Use new system for real-time scanning
        Vector3[] autoPoints = newSystem.GetPointCloud();
        
        // Use old system for manual snapshots
        if (Input.GetKeyDown(KeyCode.Space))
        {
            oldSystem.CaptureDepthFrame();
            Vector3[] manualPoints = oldSystem.ConvertDepthToPointCloud();
        }
    }
}
```

## Next Steps

1. **Test both systems** to see which works better for your use case
2. **Compare accuracy** - new script should be more accurate
3. **Choose your approach** - migrate, keep both, or hybrid
4. **Update documentation** - note which system you're using

