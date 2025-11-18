# Quest 3 Depth Scanner Setup

Simple setup guide based on [Meta's official Depth API documentation](https://developers.meta.com/horizon/documentation/native/android/mobile-depth/) and [Unity-DepthAPI samples](https://github.com/oculus-samples/Unity-DepthAPI).

---

## Unity Scene Setup

### 1. OVRCameraRig
- Add **OVRCameraRig** prefab to your scene (from Meta XR SDK)
- On the **OVRCameraRig** GameObject:
  - **OVRManager** component → Set **"Passthrough Support"** to **"Required"**
  - Add **OVRPassthroughLayer** component (if not already present)
  - Make sure OVRPassthroughLayer is **Enabled**

### 2. SimpleDepthScanner
- Create an empty GameObject: "DepthScanner"
- Add **SimpleDepthScanner** script
- Configure in Inspector:
  - Point Stride: 4 (lower = more points, slower)
  - Min/Max Depth: 0.1m - 4.0m (Quest 3 range)
  - Point Color: Cyan (or your preference)
  - Scan Button: One (A button)

### 3. QuestDebugConsole (optional)
- Create an empty GameObject: "DebugConsole"  
- Add **QuestDebugConsole** script
- It will auto-generate UI in VR

---

## Project Settings

### Meta XR Settings
**Edit > Project Settings > Meta XR**
- **Scene Support**: Required (critical for depth!)
- **Passthrough Support**: Required

### XR Plug-in Management  
**Edit > Project Settings > XR Plug-in Management > Android**
- ✅ Oculus checked

### Android Manifest
File already configured with required permissions:
- `com.oculus.permission.USE_SCENE`
- `horizonos.permission.HEADSET_CAMERA`
- `com.oculus.software.environment_depth`

---

## Quest 3 Device Setup

### Complete Space Setup
**This is required for depth to work!**

1. Put on Quest 3
2. Settings > Physical Space > Space Setup
3. Complete full room scan (don't skip)
4. Let Quest map walls, floor, ceiling

### Grant Permissions
When you first run your app:
- Grant **"Scene"** permission when prompted
- This allows depth data access

---

## Build and Test

1. **File > Build Settings > Build and Run**
2. Connect Quest 3 via USB
3. App launches with passthrough (you see your room)
4. Look at a **solid surface** (wall, table, floor)
5. Press **A button** to scan
6. Cyan dots appear on the scanned surface

---

## Troubleshooting

### No points generated
- Complete Space Setup on Quest (most common issue)
- Grant Scene permission to app
- Use solid surfaces (not curtains, glass, mirrors)
- Ensure good lighting
- Distance: 1-2 meters from surface

### Depth not initializing  
- Check OVRManager > Passthrough Support: Required
- Check Meta XR Settings > Scene Support: Required
- Complete Space Setup on Quest
- Restart Quest and rebuild app

### Reference Implementation
See existing working scripts for advanced usage:
- `DepthPointCloudGenerator.cs` - Continuous point cloud generation
- `DepthCapture.cs` - Official Oculus initialization pattern
- `PointCloudVisualizer.cs` - Advanced visualization options

---

## Key Differences from Debug Version

This simplified version:
- ✅ Minimal debug output (only essential logs)
- ✅ Based directly on official Unity-DepthAPI samples
- ✅ Follows Meta's documented patterns
- ✅ Clean, production-ready code
- ❌ No excessive diagnostic logging
- ❌ No step-by-step troubleshooting output

For detailed technical information, refer to:
- [Meta Depth API Docs](https://developers.meta.com/horizon/documentation/native/android/mobile-depth/)
- [Unity-DepthAPI GitHub](https://github.com/oculus-samples/Unity-DepthAPI)
