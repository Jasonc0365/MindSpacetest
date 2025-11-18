# How to See Debug Output on Quest

## Problem: Can't See Debug Logs After Build and Run

After building and running, you need to **connect Unity Console** to see debug logs from Quest.

---

## Quick Solution (3 Steps)

### Step 1: Enable Development Build

**Before building:**
1. **Edit → Project Settings → Player**
2. **Other Settings → Configuration**
3. ✅ **Enable "Development Build"**
4. ✅ **Enable "Script Debugging"** (optional, for breakpoints)

**Important:** You need to rebuild after enabling this!

### Step 2: Connect Quest to Unity Console

**After Build and Run:**
1. Keep Quest connected via USB (or ADB over WiFi)
2. In Unity: **Window → General → Console**
3. Console should automatically show logs from Quest

**If logs don't appear:**
- Check Quest is still connected: `adb devices` (if ADB installed)
- Try disconnecting and reconnecting Quest
- Check Console filter settings (should show all log levels)

### Step 3: Check Your Debug Settings

**In Unity Inspector (before building):**
1. Select your `DepthPointCloudGenerator` GameObject
2. Check **"Show Debug Logs"** is enabled ✅
3. Rebuild if you changed this

---

## Detailed Steps

### Method 1: Unity Console (Easiest)

**Setup:**
1. **Enable Development Build:**
   - Edit → Project Settings → Player
   - Other Settings → Configuration
   - ✅ Development Build
   - ✅ Script Debugging (optional)

2. **Build and Run:**
   - File → Build Settings
   - Click "Build and Run"
   - Keep Quest connected via USB

3. **Open Unity Console:**
   - Window → General → Console
   - Logs appear automatically from Quest

**What you'll see:**
```
[DepthPointCloudGenerator] ✅ Found EnvironmentDepthManager. IsSupported: True
[DepthPointCloudGenerator] ✅ Generated 2543 points
[DepthPointCloudGenerator] ⚠️ Depth not available yet...
```

### Method 2: ADB Logcat (If ADB Installed)

**View logs via command line:**
```bash
# Connect Quest via USB or WiFi
adb devices

# View Unity logs
adb logcat -s Unity

# View specific script logs
adb logcat -s DepthPointCloudGenerator:* EnvironmentDepthManager:*

# Clear and view fresh logs
adb logcat -c && adb logcat -s Unity
```

### Method 3: On-Screen Debug UI

**Use DepthVisualizationDebugger:**
1. Add `DepthVisualizationDebugger` component to scene
2. Assign `DepthPointCloudGenerator` reference
3. Enable **"Show On Screen Stats"**
4. Build and run
5. See debug info **in VR** (on Quest screen)

**What you'll see in VR:**
- Point count
- Depth API status
- Frame rate
- Other statistics

---

## Troubleshooting

### No Logs Appearing in Unity Console

**Check:**
1. ✅ Development Build enabled?
2. ✅ Quest connected via USB?
3. ✅ Console window open?
4. ✅ Console filter not hiding logs?

**Fix:**
- Rebuild with Development Build enabled
- Check Console filter: Should show Error, Warning, Info
- Try disconnecting/reconnecting Quest

### Logs Appear But No Debug Messages

**Check:**
1. ✅ `Show Debug Logs = true` in Inspector?
2. ✅ Scripts actually logging? (Check code)
3. ✅ App running on Quest?

**Fix:**
- Enable "Show Debug Logs" in Inspector
- Rebuild after changing settings
- Check if app is actually running on Quest

### Can't See On-Screen Debug

**Check:**
1. ✅ `DepthVisualizationDebugger` component added?
2. ✅ "Show On Screen Stats" enabled?
3. ✅ Reference to `DepthPointCloudGenerator` assigned?

**Fix:**
- Add `DepthVisualizationDebugger` to scene
- Enable "Show On Screen Stats"
- Assign `pointCloudGenerator` reference

---

## What Debug Output You Should See

### If Depth API is Working:

```
[DepthPointCloudGenerator] ✅ Found EnvironmentDepthManager. IsSupported: True
[DepthPointCloudGenerator] ✅ Generated 2543 points
[DepthPointCloudGenerator] Depth texture size: 640x480
```

### If Depth API is NOT Working:

```
[DepthPointCloudGenerator] ⚠️ Depth not available yet. Waiting for EnvironmentDepthManager...
[DepthPointCloudGenerator] EnvironmentDepthManager not found!
```

### If Build Succeeded:

```
Build completed with a result of 'Succeeded'
Application installed to device "Quest 3"
Launching application...
```

---

## Quick Checklist

**Before Building:**
- [ ] Development Build enabled
- [ ] Show Debug Logs = true (in Inspector)
- [ ] DepthVisualizationDebugger added (if using on-screen debug)

**After Building:**
- [ ] Quest connected via USB
- [ ] Unity Console window open
- [ ] Console filter shows all log levels
- [ ] App running on Quest

**To See Debug:**
- [ ] Check Unity Console for logs
- [ ] Or use ADB logcat
- [ ] Or enable on-screen stats in VR

---

## Recommended Setup

### For Development:
1. ✅ Enable Development Build
2. ✅ Enable Show Debug Logs
3. ✅ Use Unity Console (Method 1)
4. ✅ Keep Quest connected via USB

### For Testing in VR:
1. ✅ Add DepthVisualizationDebugger
2. ✅ Enable "Show On Screen Stats"
3. ✅ See debug info directly in VR

---

## Example: Full Debug Setup

### Step 1: Enable Development Build
```
Edit → Project Settings → Player
Other Settings → Configuration
✅ Development Build
✅ Script Debugging
```

### Step 2: Configure Debug Scripts
```
Select DepthPointCloudGenerator GameObject
Inspector → Show Debug Logs = ✅ true
```

### Step 3: Add On-Screen Debug (Optional)
```
Add DepthVisualizationDebugger component
Assign DepthPointCloudGenerator reference
Enable "Show On Screen Stats"
```

### Step 4: Build and Run
```
File → Build Settings
Build and Run
Keep Quest connected
```

### Step 5: View Debug
```
Window → General → Console
See logs in real-time!
```

---

## Next Steps

1. **Enable Development Build** (if not already)
2. **Rebuild** your app
3. **Open Unity Console** (Window → General → Console)
4. **Check logs** - you should see debug output!

If you still don't see logs, check:
- Development Build is enabled
- Quest is connected
- Console filter settings
- Show Debug Logs is enabled in Inspector

Good luck! 🚀

