# QUEST 3 DEPTH API - COMPLETE SETUP GUIDE

## NullReferenceException in PreprocessDepthTexture?
This means the depth manager can't access the depth texture. Follow ALL steps below.

---

## PART 1: UNITY PROJECT SETTINGS (Critical!)

### 1.1 Android Manifest - Permissions
The depth API requires specific Android permissions. You need to manually edit the manifest.

**Location:** `Assets/Plugins/Android/AndroidManifest.xml`

**If this file doesn't exist, create it!**

Add these permissions inside `<manifest>` tag:

```xml
<?xml version="1.0" encoding="utf-8"?>
<manifest xmlns:android="http://schemas.android.com/apk/res/android" 
          package="com.yourcompany.yourapp">
    
    <!-- REQUIRED for Depth API -->
    <uses-permission android:name="com.oculus.permission.USE_SCENE" />
    <uses-permission android:name="android.permission.CAMERA" />
    
    <!-- Quest 3 features -->
    <uses-feature android:name="android.hardware.vr.headtracking" android:required="true" />
    <uses-feature android:name="com.oculus.feature.PASSTHROUGH" android:required="true" />
    <uses-feature android:name="com.oculus.feature.SCENE" android:required="true" />
    <uses-feature android:name="com.oculus.software.environment_depth" android:required="true" />
    
    <application>
        <!-- Your app content -->
    </application>
</manifest>
```

### 1.2 XR Plug-in Management
**File > Build Settings > Player Settings > XR Plug-in Management**

- ✅ Install XR Plug-in Management (if not already)
- ✅ Switch to Android tab
- ✅ Check "Oculus" checkbox
- ✅ Click on "Oculus" to expand settings:
  - Stereo Rendering Mode: Multiview or Multipass
  - V2 Signing (Quest 3): Enabled

### 1.3 Meta XR Settings
**Edit > Project Settings > Meta XR**

- ✅ General:
  - Body Tracking Support: None or Optional
  - Face Tracking Support: None or Optional
  - Eye Tracking Support: None or Optional
  - **Passthrough Support: Required** ← CRITICAL!
  - **Scene Support: Required** ← CRITICAL for Depth!
  
- ✅ Experimental:
  - Environment Depth: Enabled

### 1.4 Build Settings
**File > Build Settings**

- Platform: Android
- Texture Compression: ASTC
- Build System: Gradle (new)
- Minimum API Level: Android 10.0 (API level 29) or higher

---

## PART 2: SCENE SETUP

### 2.1 OVRCameraRig Setup
1. **Delete any old cameras** (Main Camera, etc)
2. **Add OVRCameraRig** prefab to scene:
   - Assets > Oculus > VR > Prefabs > OVRCameraRig
   - Drag into Hierarchy

3. **Configure OVRCameraRig:**
   - Select OVRCameraRig in Hierarchy
   - Find **OVRManager** component
   - Set **Tracking Origin Type**: Floor Level
   - Set **Passthrough Support**: Required
   - Set **Scene Support**: Required (if available)

### 2.2 Add OVRPassthroughLayer
**On the OVRCameraRig GameObject:**
- Add Component > OVRPassthroughLayer
- Make sure it's **Enabled**
- Placement: Overlay
- Compositing: Additive or Blend (your choice)

### 2.3 Add SimpleDepthScanner
- Create empty GameObject: "DepthScanner"
- Add Component > SimpleDepthScanner script
- Settings:
  - Point Stride: 4 (default)
  - Min Depth: 0.1m
  - Max Depth: 4.0m
  - Scan Button: One (A button)
  - Controller: RTouch (right controller)

### 2.4 Add QuestDebugConsole (for testing)
- Create empty GameObject: "DebugConsole"
- Add Component > QuestDebugConsole script
- Console will auto-generate UI

---

## PART 3: QUEST 3 DEVICE SETUP

### 3.1 Enable Developer Mode
1. Install Meta Quest app on your phone
2. Connect Quest to your Meta account
3. Go to Menu > Devices > [Your Quest 3] > Developer Mode
4. Toggle Developer Mode ON
5. Put on Quest, approve USB debugging when prompted

### 3.2 Complete Space Setup (CRITICAL!)
**This is THE most important step!**

Put on Quest 3:
1. Go to **Quick Settings** (press Oculus button)
2. **Settings > Physical Space > Space Setup**
3. Click **"Set Up"** or **"Redo Space Setup"**
4. Follow the guardian setup completely:
   - Look at walls
   - Look at floor
   - Look at ceiling
   - Walk around the room
   - Let it scan your entire space
5. Complete the full setup!

**Why this matters:** The depth API uses the space map Quest creates. No space map = no depth data!

### 3.3 Grant Permissions
When you first run your app on Quest:
1. App will ask for **"Scene"** permission
2. **GRANT IT** - this allows access to depth/space data
3. App might ask for **"Camera"** permission - grant it too

To check permissions later:
- Quest Settings > Apps > [Your App Name] > Permissions
- Make sure "Scene" is enabled

---

## PART 4: BUILD AND TEST

### 4.1 Build and Deploy
```
File > Build Settings
- Make sure your scene is in "Scenes in Build"
- Click "Build and Run"
- Connect Quest 3 via USB
- Allow USB debugging on Quest
- Unity will build and install
```

### 4.2 Testing Procedure
1. **Put on Quest** - you should see passthrough (your room)
2. **Look at the debug console** in VR (floating text panel)
3. **Wait for initialization:**
   ```
   [SimpleDepthScanner] Found OVRManager
   [SimpleDepthScanner] Passthrough layer found and enabled
   [SimpleDepthScanner] EnvironmentDepthManager created on OVRCameraRig
   [SimpleDepthScanner] SUCCESS! Depth initialized and available!
   [SimpleDepthScanner] Press A button to scan
   ```

4. **If initialization fails:**
   - Check console errors
   - Most common: "No OVRPassthroughLayer" or "Depth not available"
   - Solution: Complete Space Setup on Quest!

5. **Test scan:**
   - Look at a **solid wall** (1-2 meters away)
   - Press **A button** on right controller
   - You should see:
     - Debug output: "Scan complete! Generated XXX points"
     - Cyan dots appearing on the wall

### 4.3 Troubleshooting

**If you see "NullReferenceException in PreprocessDepthTexture":**
- ❌ Depth manager can't access depth texture
- ✅ Fix:
  1. Check AndroidManifest.xml has all permissions (see Part 1.1)
  2. Meta XR Settings > Scene Support: Required
  3. Complete Space Setup on Quest
  4. Restart Quest and rebuild app

**If you see "All pixels have invalid depth":**
- ❌ Depth texture exists but contains no data
- ✅ Fix:
  1. Complete Space Setup on Quest (most common!)
  2. Grant Scene permission in Quest app settings
  3. Make sure passthrough is working (you see your room)
  4. Good lighting in the room

**If you see "No points generated":**
- ❌ Scan worked but found no valid points
- ✅ Fix:
  1. Look at solid surfaces (wall, table, floor)
  2. Avoid: curtains, glass, mirrors, black surfaces
  3. Be 1-2 meters away from surface
  4. Good lighting

**If initialization never completes:**
- ❌ Depth API not initializing
- ✅ Fix:
  1. Check OVRCameraRig has OVRPassthroughLayer component
  2. OVRManager > Passthrough Support: Required
  3. Project Settings > Meta XR > Scene Support: Required
  4. Complete Space Setup on Quest
  5. Restart Quest and rebuild

---

## PART 5: VERIFICATION CHECKLIST

Before deploying, verify:

### Unity:
- [ ] AndroidManifest.xml exists with all permissions
- [ ] XR Plug-in Management > Oculus enabled
- [ ] Meta XR > Passthrough Support: Required
- [ ] Meta XR > Scene Support: Required
- [ ] OVRCameraRig in scene with OVRPassthroughLayer
- [ ] SimpleDepthScanner script added to scene
- [ ] Build target: Android

### Quest Device:
- [ ] Developer Mode enabled
- [ ] USB debugging approved
- [ ] Space Setup completed (FULL setup, not skipped!)
- [ ] Passthrough working (can see room)
- [ ] Scene permission granted to app
- [ ] Good lighting in room

### Test:
- [ ] App launches and shows passthrough
- [ ] Debug console appears with initialization messages
- [ ] "SUCCESS! Depth initialized and available!" message
- [ ] Pressing A button triggers scan
- [ ] Cyan dots appear on scanned surfaces

---

## PART 6: COMMON ERRORS & SOLUTIONS

| Error | Cause | Solution |
|-------|-------|----------|
| "No OVRManager found" | Missing OVRCameraRig | Add OVRCameraRig prefab to scene |
| "No OVRPassthroughLayer found" | Missing component | Add OVRPassthroughLayer to OVRCameraRig |
| "NullReferenceException: PreprocessDepthTexture" | Missing permissions or Scene Support | Check AndroidManifest.xml, Meta XR settings, complete Space Setup |
| "Timeout: Depth did not become available" | Space Setup not done | Complete Space Setup on Quest |
| "All pixels have invalid depth" | No space data | Complete Space Setup, grant Scene permission |
| Black screen instead of passthrough | Passthrough not enabled | OVRManager > Passthrough Support: Required |

---

## QUICK START (if you've done this before)

1. AndroidManifest.xml has Scene permission
2. Meta XR > Scene Support: Required
3. OVRCameraRig + OVRPassthroughLayer in scene
4. Complete Space Setup on Quest
5. Build and Run
6. Grant Scene permission when prompted
7. Look at wall, press A button

---

If you follow ALL steps and it still doesn't work, check Unity console logs and send screenshots of:
1. Meta XR Project Settings
2. OVRCameraRig Inspector
3. In-VR debug console messages

