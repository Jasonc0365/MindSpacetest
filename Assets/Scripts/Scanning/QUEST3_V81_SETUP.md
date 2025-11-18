# Quest 3 + Oculus SDK v81 - Depth API Setup

## ✅ Fixed Implementation for Quest 3

The `DepthPointCloudGenerator` has been updated to work specifically with Quest 3 and Oculus SDK v81.

---

## Key Changes for v81 Compatibility

### 1. **Better Error Handling**
- Added comprehensive debug logging
- Checks for null textures and matrices
- Validates depth data before processing

### 2. **Performance Optimizations**
- Throttled updates (default: 10 FPS instead of every frame)
- Frame skipping to reduce CPU load
- Better texture format handling

### 3. **Texture Format Support**
- Tries RFloat format first (standard for depth)
- Falls back to RGB24 if needed
- Handles different RenderTexture formats

### 4. **Validation**
- Checks for NaN/Infinity in world positions
- Validates pixel indices before access
- Filters invalid depth values

---

## Setup Checklist

### Step 1: Scene Setup
- [ ] `OVRCameraRig` at (0,0,0)
- [ ] `EnvironmentDepthManager` component in scene
- [ ] Passthrough enabled in OVRManager
- [ ] Scene Support permission requested

### Step 2: Add DepthPointCloudGenerator
1. Create empty GameObject "DepthSystem"
2. Add `DepthPointCloudGenerator` component
3. Configure settings:
   - `Stride X/Y`: 4 (default, adjust for performance)
   - `Max Depth`: 4.0 (Quest 3 max reliable)
   - `Min Depth`: 0.1
   - `Visualize`: false (use PointCloudVisualizer instead)
   - `Show Debug Logs`: true (for troubleshooting)

### Step 3: Add PointCloudVisualizer
1. Add `PointCloudVisualizer` component to same GameObject
2. Assign `DepthPointCloudGenerator` reference
3. Configure visualization settings

### Step 4: Enable Experimental Features
```bash
adb shell setprop debug.oculus.experimentalEnabled 1
adb reboot
```

### Step 5: Build Settings
- Android platform
- Minimum API Level: 29+
- Oculus XR Plugin enabled
- IL2CPP scripting backend

---

## Troubleshooting

### Problem: "Depth not available yet"

**Solution:**
1. Wait 2-3 seconds after scene starts
2. Check `EnvironmentDepthManager.IsDepthAvailable` in inspector
3. Verify Space Setup is complete on Quest
4. Check permissions in OVRManager

### Problem: "Depth texture is null"

**Check:**
1. `EnvironmentDepthManager` is enabled
2. `IsDepthAvailable = true`
3. Scene Support permission granted
4. Experimental features enabled

### Problem: "No reprojection matrices available"

**Solution:**
- This means depth texture hasn't been set up yet
- Wait for `IsDepthAvailable` to become true
- Check that `EnvironmentDepthManager` is running

### Problem: "Failed to read RenderTexture"

**Solution:**
- Texture format might be different
- Script now tries multiple formats automatically
- Check console for specific error

### Problem: No points generated

**Check:**
1. Enable `Show Debug Logs` in inspector
2. Look for console messages:
   - "✅ Generated X points" = working
   - "Depth texture: WxH" = texture found
   - Warnings = specific issue
3. Verify depth range (0.1m to 4.0m)
4. Check stride settings (lower = more points)

---

## Debug Information

### Enable Debug Logs
Set `Show Debug Logs = true` in `DepthPointCloudGenerator` inspector.

**You'll see:**
- ✅ Found EnvironmentDepthManager
- ✅ Generated X points
- Depth texture: WxH
- Warnings for any issues

### Check Console
Look for these messages:
```
[DepthPointCloudGenerator] ✅ Found EnvironmentDepthManager. IsSupported: True
[DepthPointCloudGenerator] ✅ Generated 2543 points
[DepthPointCloudGenerator] Depth texture: 320x240
```

### Common Warnings
- "Depth not available yet" → Wait for initialization
- "Depth texture is null" → Check EnvironmentDepthManager
- "No reprojection matrices" → Depth not ready yet
- "Failed to read pixels" → Texture format issue

---

## Performance Settings

### For Better Performance
- Increase `Stride X/Y` to 8 (fewer points)
- Increase `Update Interval` to 0.2 (5 FPS updates)
- Reduce `Max Points Per Frame` to 2500

### For Better Quality
- Decrease `Stride X/Y` to 2 (more points)
- Decrease `Update Interval` to 0.05 (20 FPS updates)
- Increase `Max Points Per Frame` to 10000

---

## Expected Behavior

### When Working Correctly:
1. Console shows: "✅ Found EnvironmentDepthManager"
2. After 2-3 seconds: "✅ Generated X points"
3. Points appear in scene (if visualize = true)
4. PointCloudVisualizer shows points

### When Not Working:
1. "Depth not available yet" (keep waiting)
2. "Depth texture is null" (check EnvironmentDepthManager)
3. No points generated (check depth range and stride)

---

## Quick Test

1. **Enable Debug Logs** in inspector
2. **Enter Play Mode**
3. **Wait 3-5 seconds**
4. **Check Console** for messages
5. **Look for points** in scene (if visualize = true)

---

## Notes

- Depth API takes 2-3 seconds to initialize
- Points generate automatically every 0.1 seconds (default)
- Use `PointCloudVisualizer` for better visualization
- Debug logs help identify issues quickly

