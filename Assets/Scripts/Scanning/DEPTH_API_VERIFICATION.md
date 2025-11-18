# Depth API Verification Guide

## Quick Verification Checklist

After removing TableScanner, use this checklist to verify the Depth API is working:

### ✅ What to Check:

1. **Console Logs**
   - Look for: `[DepthPointCloudGenerator] ✅ Generated X points from depth`
   - Should see point count > 0
   - No errors about depth texture being null

2. **Point Cloud Visualization**
   - Points should appear in the scene (colored spheres or mesh points)
   - Points should match objects you're looking at
   - Points update in real-time as you move

3. **Statistics (if using DepthVisualizationDebugger)**
   - `Depth API: ✅ AVAILABLE` (green)
   - `Point Count: > 0` (e.g., 2500 points)
   - `Depth Texture: 320x240` (or similar, not NULL)

---

## Step-by-Step Verification

### Step 1: Check EnvironmentDepthManager

**In Unity Console, look for:**
```
[DepthPointCloudGenerator] Depth system initialized
```

**Or check manually:**
- Find `EnvironmentDepthManager` in scene
- Check `IsDepthAvailable` property (should be true)
- Component should be enabled

### Step 2: Check Point Generation

**What you should see:**
- Console log: `[DepthPointCloudGenerator] ✅ Generated X points from depth`
- Point count should be > 0 (typically 2000-5000 with stride 4x4)

**If point count is 0:**
- Depth API might not be available yet
- Wait a few seconds after scene starts
- Point camera at objects (not empty space)
- Check depth range settings (minDepth: 0.1m, maxDepth: 4.0m)

### Step 3: Check Visualization

**With PointCloudVisualizer:**
- Points should appear as colored dots/spheres
- Blue = near objects, Red = far objects
- Points should match real-world objects

**If no points visible:**
- Check `PointCloudVisualizer` component is enabled
- Check `DepthPointCloudGenerator.visualize = true`
- Check material is assigned (or auto-created)
- Try increasing `Point Size` in inspector

### Step 4: Use DepthVisualizationDebugger (Recommended)

**Add component for detailed info:**
1. Add `DepthVisualizationDebugger` to your GameObject
2. Assign `DepthPointCloudGenerator` reference
3. Enable `Show Statistics` and `Show Depth Texture Preview`
4. Enter Play Mode
5. Look for on-screen statistics

**What you should see:**
```
=== DEPTH API DEBUG ===
Point Count: 2,543
Center: (0.12, 1.45, -0.32)
Avg Distance: 1.85m
Depth API: ✅ AVAILABLE
Depth Texture: 320x240
```

---

## Common Issues & Solutions

### Issue: "Depth API: ❌ NOT AVAILABLE"

**Causes:**
- Space Setup not completed on Quest
- USE_SCENE permission not granted
- EnvironmentDepthManager not in scene
- Depth API not supported (wrong device)

**Solutions:**
1. Run Space Setup on Quest 3
2. Grant USE_SCENE permission when prompted
3. Ensure `EnvironmentDepthManager` component exists in scene
4. Check device is Quest 3 or Quest 3S

### Issue: "Point Count: 0"

**Causes:**
- Depth texture is null
- No objects in view
- Depth range too restrictive
- Reprojection matrices not available

**Solutions:**
1. Point camera at objects (not empty space)
2. Check depth range: `minDepth = 0.1f`, `maxDepth = 4.0f`
3. Wait a few seconds after scene starts
4. Check console for errors about reprojection matrices

### Issue: "Depth Texture: NULL"

**Causes:**
- EnvironmentDepthManager not initialized
- Depth API not available
- Shader global not set

**Solutions:**
1. Ensure `EnvironmentDepthManager` is enabled
2. Wait for `IsDepthAvailable` to become true
3. Check console for initialization errors

### Issue: "Points Scattered Randomly"

**Causes:**
- Wrong reprojection matrix
- Coordinate system mismatch
- Depth texture format issue

**Solutions:**
1. Verify using correct eye index (typically 0 for left eye)
2. Check reprojection matrix inverse is used
3. Ensure depth texture format is correct

---

## Quick Test Procedure

1. **Remove TableScanner** ✅ (You've done this)

2. **Check Components:**
   - `EnvironmentDepthManager` - Should be in scene
   - `DepthPointCloudGenerator` - Should be enabled
   - `PointCloudVisualizer` - Should be enabled

3. **Enter Play Mode:**
   - Wait 2-3 seconds for initialization
   - Point Quest at objects (table, walls, etc.)
   - Check console for logs

4. **Verify:**
   - Console shows point count > 0
   - Points appear in scene
   - Points match real-world objects

5. **Use Debugger (Optional):**
   - Add `DepthVisualizationDebugger`
   - Check on-screen statistics
   - Verify depth texture preview shows data

---

## Expected Console Output (Working)

```
[DepthPointCloudGenerator] ✅ Depth system initialized
[DepthPointCloudGenerator] ✅ Generated 2543 points from depth
[PointCloudVisualizer] Using DepthPointCloudGenerator: 2543 points
[PointCloudVisualizer] ✅ Visualized 2543 points
```

---

## Expected Console Output (Not Working)

```
[DepthPointCloudGenerator] Depth not available!
[DepthPointCloudGenerator] Depth texture is null!
[DepthPointCloudGenerator] No reprojection matrices available!
[PointCloudVisualizer] No points generated. Check depth system setup.
```

---

## Manual Verification Script

If you want to check programmatically, add this to a test script:

```csharp
void CheckDepthAPI()
{
    var depthManager = FindFirstObjectByType<EnvironmentDepthManager>();
    
    if (depthManager == null)
    {
        Debug.LogError("❌ EnvironmentDepthManager not found!");
        return;
    }
    
    Debug.Log($"✅ EnvironmentDepthManager found");
    Debug.Log($"   IsDepthAvailable: {depthManager.IsDepthAvailable}");
    Debug.Log($"   Enabled: {depthManager.enabled}");
    
    Texture depthTex = Shader.GetGlobalTexture("_EnvironmentDepthTexture");
    if (depthTex != null)
    {
        Debug.Log($"✅ Depth texture: {depthTex.width}x{depthTex.height}");
    }
    else
    {
        Debug.LogWarning("❌ Depth texture is NULL");
    }
    
    var generator = FindFirstObjectByType<DepthPointCloudGenerator>();
    if (generator != null)
    {
        int pointCount = generator.GetPointCount();
        Debug.Log($"✅ Point count: {pointCount}");
    }
}
```

---

## Summary

**Depth API is working if:**
- ✅ Console shows point count > 0
- ✅ Points appear in scene
- ✅ Points match real-world objects
- ✅ Statistics show "Depth API: AVAILABLE"

**Depth API is NOT working if:**
- ❌ Point count is 0
- ❌ No points visible
- ❌ "Depth not available" errors
- ❌ Depth texture is NULL

Use `DepthVisualizationDebugger` for the easiest verification - it shows all this info on-screen!

