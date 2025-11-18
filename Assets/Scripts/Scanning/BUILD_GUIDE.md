# Build Guide - Android (Quest) & Standalone

## Build Configuration Checklist

### Pre-Build Checklist

**Before Building:**
- [ ] All component references assigned in Inspector
- [ ] UI Canvas set up and visible
- [ ] TextMesh Pro package imported
- [ ] MRUK prefab in scene
- [ ] PassthroughCameraAccess component in scene
- [ ] EnvironmentDepthManager (optional - auto-creates if missing)
- [ ] All scripts compile without errors

---

## Android Build (Quest) Settings

### 1. Build Settings

**File > Build Settings:**
- **Platform:** Android
- **Target Device:** Quest (or Quest 2/3/Pro)
- **Architecture:** ARM64
- **Build App Bundle:** Unchecked (for APK) or Checked (for AAB)

### 2. Player Settings

**Edit > Project Settings > Player:**

#### Android Tab:
- **Minimum API Level:** Android 7.0 (API level 24) or higher
- **Target API Level:** Latest (API level 33+)
- **Scripting Backend:** IL2CPP
- **Target Architectures:** 
  - ✅ ARM64 (required for Quest)

#### XR Plug-in Management:
- **Meta XR SDK:** ✅ Enabled
- **OpenXR:** ✅ Enabled (if using)

#### Other Settings:
- **Package Name:** `com.yourcompany.yourapp` (unique identifier)
- **Version:** Set your app version
- **Version Code:** Increment for each build

### 3. XR Settings

**Edit > Project Settings > XR Plug-in Management:**
- **Meta:** ✅ Enabled
- **OpenXR:** ✅ Enabled (if applicable)

**Meta XR Settings:**
- **Target Devices:** Quest, Quest 2, Quest Pro, Quest 3
- **Hand Tracking Support:** ✅ Enabled (if using)
- **Passthrough Support:** ✅ Enabled (required for scanning)

### 4. Quality Settings

**Edit > Project Settings > Quality:**
- **Quest 2:** Medium or High quality
- **Quest 3/Pro:** High or Very High quality
- **VR SDK Quality:** Set appropriately for target device

### 5. Graphics Settings

**Edit > Project Settings > Graphics:**
- **Scriptable Render Pipeline:** URP (Universal Render Pipeline)
- **Color Space:** Linear (recommended for VR)

---

## Standalone Build Settings

### 1. Build Settings

**File > Build Settings:**
- **Platform:** PC, Mac & Linux Standalone
- **Target Platform:** Windows/Mac/Linux
- **Architecture:** x86_64 (64-bit)

### 2. Player Settings

**Edit > Project Settings > Player:**

#### PC, Mac & Linux Standalone Tab:
- **Default Icon:** Set app icon
- **Resolution and Presentation:**
  - **Fullscreen Mode:** Fullscreen Window
  - **Run in Background:** ✅ Enabled
  - **Capture Single Screen:** ✅ Enabled

### 3. Standalone-Specific Considerations

**Important Notes:**
- ❌ **No PassthroughCameraAccess** - Will not work on standalone (Quest-only feature)
- ❌ **No EnvironmentDepthManager** - Quest-only feature
- ⚠️ **MRUK may not work** - Designed for Quest room scanning
- ⚠️ **Controller input** - May need keyboard fallback

**Adaptations Needed:**
1. **Disable Quest-Specific Features:**
   - Set `useControllerInput = false` in ScanningWorkflow
   - Use keyboard input instead
   - Or implement alternative input system

2. **Mock/Alternative Data:**
   - Create mock table detection for testing
   - Use simulated depth data
   - Or disable depth-based features

3. **Input Handling:**
   - Add keyboard shortcuts
   - Add mouse input support
   - Or disable scanning features entirely

---

## Platform-Specific Code Considerations

### Current Code Status

**Quest-Specific Features:**
- ✅ `PassthroughCameraAccess` - Quest only
- ✅ `EnvironmentDepthManager` - Quest only
- ✅ `OVRInput` - Quest only
- ✅ `MRUK` - Quest only

**Cross-Platform Features:**
- ✅ Basic Unity UI
- ✅ TextMesh Pro
- ✅ Standard Unity components

### Conditional Compilation

The code already uses conditional compilation for some features:

```csharp
#if UNITY_EDITOR || UNITY_ANDROID
// Quest-specific code
#endif
```

**For Standalone Build:**
- Depth features will be disabled automatically
- Some features may need additional platform checks

---

## Build Steps

### Android (Quest) Build:

1. **Prepare:**
   ```
   File > Build Settings
   Platform: Android
   Switch Platform (if needed)
   ```

2. **Configure:**
   ```
   Edit > Project Settings > Player
   Set Package Name
   Set Minimum/Target API Level
   Enable XR Plug-in Management
   ```

3. **Build:**
   ```
   File > Build Settings
   Click "Build" or "Build and Run"
   Choose output folder
   Wait for build to complete
   ```

4. **Deploy:**
   - Connect Quest via USB
   - Enable Developer Mode on Quest
   - Use ADB to install: `adb install YourApp.apk`
   - Or use Oculus Developer Hub

### Standalone Build:

1. **Prepare:**
   ```
   File > Build Settings
   Platform: PC, Mac & Linux Standalone
   Switch Platform (if needed)
   ```

2. **Configure:**
   ```
   Edit > Project Settings > Player
   Set Company/Product Name
   Set Default Icon
   ```

3. **Disable Quest Features:**
   - In ScanningWorkflow: Set `useControllerInput = false`
   - Or create build-specific scene without Quest features

4. **Build:**
   ```
   File > Build Settings
   Click "Build"
   Choose output folder
   Wait for build to complete
   ```

---

## Build Size Optimization

### For Android (Quest):

**Reduce Build Size:**
- Remove unused assets
- Compress textures
- Use texture atlases
- Remove unused packages
- Enable compression in Player Settings

**Expected Size:**
- Quest build: 50-200 MB (depending on assets)
- With YOLOv11 model: +50-100 MB

### For Standalone:

**Reduce Build Size:**
- Same optimizations as Android
- Remove Quest-specific assets if possible
- Use platform-specific asset variants

---

## Testing After Build

### Android (Quest) Testing:

1. **Install on Quest:**
   - Deploy via ADB or Oculus Developer Hub
   - Launch from Quest home

2. **Test Features:**
   - ✅ Passthrough camera works
   - ✅ Depth API available
   - ✅ MRUK detects room
   - ✅ Controller input works
   - ✅ Scanning system functions

3. **Check Console:**
   - Use ADB logcat: `adb logcat -s Unity`
   - Or use Oculus Developer Hub logs

### Standalone Testing:

1. **Run Executable:**
   - Launch the .exe (Windows) or app (Mac)
   - Test in windowed/fullscreen mode

2. **Test Features:**
   - ⚠️ Passthrough won't work (expected)
   - ⚠️ Depth API won't work (expected)
   - ⚠️ MRUK may not work (expected)
   - ✅ UI should work
   - ✅ Keyboard input should work (if configured)

3. **Check Console:**
   - View Unity Player log
   - Check for platform-specific errors

---

## Common Build Issues

### Android Build Issues:

**"Gradle Build Failed":**
- Check Android SDK path
- Verify JDK version
- Check build.gradle for errors

**"Missing XR Plugin":**
- Enable Meta XR SDK in XR Plug-in Management
- Verify package is installed

**"APK too large":**
- Enable compression
- Remove unused assets
- Use texture compression

**"App crashes on Quest":**
- Check logcat for errors
- Verify all Quest-specific features are available
- Check permissions

### Standalone Build Issues:

**"Quest features not working":**
- Expected - these are Quest-only
- Disable or mock these features

**"Input not working":**
- Enable keyboard fallback
- Check input system settings

**"MRUK not working":**
- Expected on standalone
- Use mock data or disable

---

## Build Configuration Files

### Android Manifest (Auto-generated)

**Location:** `Assets/Plugins/Android/AndroidManifest.xml`

**Important Permissions:**
```xml
<uses-permission android:name="com.oculus.permission.HAND_TRACKING" />
<uses-permission android:name="com.oculus.permission.USE_ANCHOR_API" />
```

### Gradle Files (Auto-generated)

**Location:** `Temp/gradleOut/`

**Custom Gradle (if needed):**
- Create `mainTemplate.gradle` in `Assets/Plugins/Android/`
- Add custom dependencies if needed

---

## Recommended Build Settings Summary

### Android (Quest):
```
Platform: Android
Target Device: Quest
Architecture: ARM64
Scripting: IL2CPP
Minimum API: 24
Target API: 33+
XR: Meta XR SDK enabled
```

### Standalone:
```
Platform: PC, Mac & Linux Standalone
Architecture: x86_64
Scripting: Mono or IL2CPP
Graphics API: DirectX 11/12 (Windows)
```

---

## Quick Build Commands

### Android:
1. File > Build Settings
2. Select Android
3. Click "Build"
4. Choose folder
5. Install via ADB: `adb install YourApp.apk`

### Standalone:
1. File > Build Settings
2. Select Standalone
3. Click "Build"
4. Choose folder
5. Run executable

---

## Post-Build Checklist

### Android (Quest):
- [ ] App installs successfully
- [ ] App launches without crashes
- [ ] Passthrough camera works
- [ ] Depth API available
- [ ] MRUK detects room
- [ ] Controller input works
- [ ] Scanning system functions
- [ ] UI is visible and readable
- [ ] Performance is acceptable (72/90 FPS)

### Standalone:
- [ ] App launches successfully
- [ ] UI is visible
- [ ] Keyboard input works (if configured)
- [ ] No Quest-specific errors (expected)
- [ ] App runs smoothly

---

## Notes

**Important:** 
- Standalone build will NOT have Quest-specific features
- Consider creating separate scenes for standalone
- Or implement platform detection and disable features accordingly
- Most scanning features require Quest hardware

**For Production:**
- Test thoroughly on target device
- Optimize performance
- Test with different room setups
- Verify all features work as expected

