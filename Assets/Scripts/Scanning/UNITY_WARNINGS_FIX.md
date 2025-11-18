# Unity Console Warnings - Fix Guide

## Summary of Your Warnings

You have **3 types of warnings**:

1. ⚠️ **XR Simulation Duplicate Assets** (Should fix)
2. ℹ️ **Video Encoding Warnings** (Can ignore - harmless)
3. ℹ️ **URP Property Array Warnings** (Can ignore - requires Unity restart)

---

## 1. XR Simulation Duplicate Assets (Fix This)

### Problem:
```
Failed to move the asset (Assets\XR\Temp\XRSimulationPreferences.asset) 
to a temporary location: Cannot move asset from Assets/XR/Temp/... 
to Assets/XR/UserSimulationSettings/Resources/...: 
Destination path name does already exist
```

### Cause:
Unity XR Simulation created temporary files in `Assets/XR/Temp/` that conflict with existing files in permanent locations.

### Solution:

**Option A: Delete Temp Folder (Recommended)**
1. Close Unity Editor
2. Delete the folder: `Assets/XR/Temp/`
3. Reopen Unity
4. Unity will regenerate files if needed

**Option B: Delete Specific Files**
1. In Unity Project window, navigate to `Assets/XR/Temp/`
2. Delete:
   - `XRSimulationPreferences.asset`
   - `XRSimulationRuntimeSettings.asset`
3. Unity will regenerate them if needed

**Option C: Let Unity Clean It Up**
1. Close Unity
2. Delete `Library/` folder (Unity will regenerate)
3. Reopen Unity
4. Let Unity reimport everything

### Files to Check:
- ✅ `Assets/XR/Temp/XRSimulationPreferences.asset` (delete)
- ✅ `Assets/XR/Temp/XRSimulationRuntimeSettings.asset` (delete)
- ✅ Keep: `Assets/XR/UserSimulationSettings/Resources/XRSimulationPreferences.asset`
- ✅ Keep: `Assets/XR/Resources/XRSimulationRuntimeSettings.asset`

---

## 2. Video Encoding Warnings (Ignore - Harmless)

### Warnings:
```
Unexpected timestamp values detected. This can occur in H.264 videos 
not encoded with the baseline profile...

Color primaries 0 is unknown or unsupported by WindowsMediaFoundation...
```

### Cause:
These are from **Meta XR SDK sample videos** in the interaction package. They're tutorial videos that have non-standard encoding.

### Solution:
**✅ IGNORE THESE** - They don't affect your app:
- Videos are in `Library/PackageCache/` (package files)
- They're sample/tutorial videos, not used in your app
- Warnings are harmless - videos will still play
- No action needed

### If You Want to Suppress (Optional):
1. **Edit → Project Settings → Player → Other Settings**
2. Add to **Scripting Define Symbols**: `UNITY_VIDEO_WARNINGS_OFF`
3. (Not recommended - warnings are harmless)

---

## 3. URP Property Array Warnings (Ignore - Requires Restart)

### Warnings:
```
Property (urp_ReflProbes_BoxMin) exceeds previous array size (64 vs 32). 
Cap to previous size. Restart Unity to recreate the arrays.
```

### Cause:
Universal Render Pipeline (URP) shader property arrays need to be resized. This happens when:
- Scene has many reflection probes
- Scene has many lights
- URP settings changed

### Solution:
**✅ RESTART UNITY** - This is the only fix:
1. Save your work
2. Close Unity Editor completely
3. Reopen Unity
4. Warnings should be gone

**Note:** These warnings are **harmless** - Unity automatically caps the arrays. Restarting just recreates them at the correct size.

### Prevention:
- Limit reflection probes in scenes
- Use fewer lights
- These warnings are normal for complex scenes

---

## Quick Fix Checklist

### Immediate Actions:
- [ ] **Delete `Assets/XR/Temp/` folder** (fixes XR warnings)
- [ ] **Restart Unity** (fixes URP warnings)
- [ ] **Ignore video warnings** (harmless)

### After Fixing:
1. Close Unity
2. Delete `Assets/XR/Temp/` folder
3. Reopen Unity
4. Check Console - XR warnings should be gone
5. URP warnings will be gone after restart

---

## Detailed Fix Steps

### Fix XR Simulation Warnings:

**Step 1: Close Unity**
- Save your work
- Close Unity Editor completely

**Step 2: Delete Temp Folder**
- Navigate to: `Assets/XR/Temp/`
- Delete the entire `Temp` folder
- (Or just delete the `.asset` files inside)

**Step 3: Reopen Unity**
- Unity will regenerate files if needed
- Warnings should be gone

**Alternative: Use Unity to Delete**
1. In Unity Project window
2. Navigate to `Assets/XR/Temp/`
3. Right-click → **Delete**
4. Confirm deletion

---

## What Each Warning Means

### XR Simulation Warnings:
- **Severity:** ⚠️ Medium (can cause issues)
- **Impact:** May prevent XR Simulation from working correctly
- **Fix:** Delete Temp folder

### Video Encoding Warnings:
- **Severity:** ℹ️ Low (harmless)
- **Impact:** None - just noise in console
- **Fix:** None needed (ignore)

### URP Property Array Warnings:
- **Severity:** ℹ️ Low (harmless, auto-fixed)
- **Impact:** None - Unity caps arrays automatically
- **Fix:** Restart Unity (optional)

---

## Prevention

### To Avoid XR Warnings:
- Don't manually edit XR Simulation files
- Let Unity manage XR Simulation assets
- Don't duplicate XR Simulation settings

### To Avoid URP Warnings:
- Limit reflection probes (use fewer)
- Optimize lighting (fewer lights)
- These warnings are normal for complex scenes

### To Reduce Console Noise:
- Filter Console by log level (Error/Warning/Info)
- Use Console search to filter specific warnings
- Ignore package warnings (they're usually harmless)

---

## Console Filtering Tips

### Filter Out Package Warnings:
1. Open **Console** window
2. Click **Filter** dropdown
3. Uncheck **"Collapse"** to see all
4. Use search bar: Type `-PackageCache` to hide package warnings

### Focus on Your Scripts:
1. In Console search bar, type your script name
2. Example: `DepthPointCloudGenerator`
3. Only shows logs from your scripts

---

## Summary

**Action Required:**
1. ✅ Delete `Assets/XR/Temp/` folder
2. ✅ Restart Unity (for URP warnings)
3. ✅ Ignore video warnings (harmless)

**After Fixing:**
- XR warnings: ✅ Gone
- URP warnings: ✅ Gone (after restart)
- Video warnings: ℹ️ Still there (but harmless)

**Your app will work fine** - these are mostly cosmetic warnings! 🚀

