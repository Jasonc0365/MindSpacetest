# Quest Standalone Debugging Guide

## How to Debug Unity While Running on Quest (Standalone)

You can debug your Unity app running on Quest standalone (not connected via Link) using several methods.

---

## ⚠️ INSTALL ADB FIRST (Required for Method 1)

If you get `adb : The term 'adb' is not recognized`, you need to install ADB first.

### Quick ADB Installation (Windows)

**Option A: Download Platform Tools (Recommended)**
1. Download Android Platform Tools:
   - Visit: https://developer.android.com/studio/releases/platform-tools
   - Or direct download: https://dl.google.com/android/repository/platform-tools-latest-windows.zip
2. Extract the ZIP file to `C:\adb` (or any folder you prefer)
3. Add to PATH:
   - Press `Win + X` → **System**
   - Click **Advanced system settings**
   - Click **Environment Variables**
   - Under **System Variables**, find **Path** → Click **Edit**
   - Click **New** → Add `C:\adb` (or your folder path)
   - Click **OK** on all windows
4. **Restart PowerShell/Command Prompt** (important!)
5. Test: `adb version` (should show version number)

**Option B: Use Unity's ADB (Easier)**
Unity includes ADB with Android Build Support:
1. Open Unity Hub
2. **Installs** → Your Unity version → **Add modules**
3. Enable **Android Build Support** → **Android SDK & NDK Tools**
4. ADB location: `C:\Users\[YourName]\AppData\Local\Android\sdk\platform-tools\adb.exe`
5. Add this path to your PATH environment variable (see Option A, step 3)

**Option C: Use SideQuest (If you have it)**
If you have SideQuest installed, ADB is included:
- Location: `C:\Users\[YourName]\AppData\Local\SideQuest\platform-tools\adb.exe`
- Add this path to PATH (see Option A, step 3)

### Verify ADB Installation
```powershell
# Open new PowerShell window and test
adb version
# Should show: Android Debug Bridge version X.X.X
```

---

## Method 1: ADB Over WiFi (Recommended - Requires ADB)

### Step 1: Enable ADB Over WiFi

**On Quest:**
1. Put on Quest headset
2. Go to **Settings → Developer**
3. Enable **"USB Connection Dialog"** (if not already)
4. Connect Quest to PC via USB cable (temporarily)

**On PC (Command Prompt/Terminal):**
```bash
# Find your Quest's IP address
adb shell ip addr show wlan0

# Or check in Quest Settings → WiFi → Your network → IP address
# Example output: 192.168.1.100
```

### Step 2: Connect ADB Over WiFi

```bash
# Connect via USB first
adb devices

# Enable TCP/IP on port 5555
adb tcpip 5555

# Disconnect USB cable

# Connect via WiFi (replace IP with your Quest's IP)
adb connect 192.168.1.100:5555

# Verify connection
adb devices
# Should show: 192.168.1.100:5555    device
```

### Step 3: View Unity Logs

**Option A: Unity Console (Best)**
1. In Unity Editor: **Window → Analysis → Profiler**
2. Click **"Active Profiler"** dropdown
3. Select your Quest device (should appear as IP address)
4. Go to **Window → General → Console**
5. Console will show logs from Quest in real-time

**Option B: ADB Logcat**
```bash
# View all Unity logs
adb logcat -s Unity

# View specific tags
adb logcat -s Unity:* DepthPointCloudGenerator:* EnvironmentDepthManager:*

# Clear and view fresh logs
adb logcat -c && adb logcat -s Unity
```

**Option C: Android Studio Logcat**
1. Install Android Studio
2. Open **View → Tool Windows → Logcat**
3. Select your Quest device
4. Filter by "Unity" or your script names

---

## Method 2: Unity Console Without ADB (Easiest - No ADB Required!)

**This method works without installing ADB!**

### Step 1: Enable Development Build
1. **Edit → Project Settings → Player**
2. **Other Settings → Configuration**
3. ✅ Enable **"Development Build"**
4. ✅ Enable **"Script Debugging"** (optional, for breakpoints)
5. Build and deploy to Quest

### Step 2: Connect Quest via USB
1. Connect Quest to PC via USB cable
2. In Quest headset: Allow USB debugging (check "Always allow")
3. In Unity: **File → Build Settings → Run Device**
4. Select your Quest device
5. Click **Build and Run** (first time) or **Patch and Run** (faster for code changes)

**Note:** See `PATCH_VS_BUILD_RUN.md` for details on Patch and Run vs Build and Run.

### Step 3: View Logs in Unity Console
1. **Window → General → Console** (open Console window)
2. Console automatically shows logs from Quest
3. Logs appear in real-time as your app runs

**Note:** This requires USB connection, but you can disconnect after deployment and logs will still appear if you keep the connection.

---

## Method 3: Unity Profiler (Remote Profiling)

### Step 1: Enable Profiler in Build

**In Unity:**
1. **Edit → Project Settings → Player**
2. **Other Settings → Configuration**
3. Enable **"Development Build"**
4. Enable **"Autoconnect Profiler"** (optional)
5. Build and deploy to Quest

### Step 2: Connect Profiler

1. **Window → Analysis → Profiler**
2. Click **"Active Profiler"** dropdown
3. Select your Quest device (IP address or device name)
4. Profiler connects automatically

**What you can see:**
- CPU usage
- Memory usage
- Frame times
- Script execution times
- Rendering stats

---

## Method 4: On-Device Debugging (Quest Built-in)

### Enable Developer Mode

**On Quest:**
1. Settings → System → Developer
2. Enable **"Developer Mode"**
3. Enable **"USB Debugging"**
4. Enable **"Wireless ADB"** (if available)

### View Logs on Quest

**Using SideQuest (if installed):**
1. Open SideQuest
2. Click **"Logs"** tab
3. View real-time logs

---

## Method 5: Unity Remote Debugging (Alternative)

### Setup

1. **Edit → Project Settings → Player**
2. Enable **"Development Build"**
3. Build and deploy to Quest
4. In Unity Editor: **Window → Analysis → Profiler**
5. Select Quest device from dropdown

### Debug Console

1. **Window → General → Console**
2. Console automatically shows logs from connected device
3. Filter by log level (Error, Warning, Info)

---

## Quick Setup Script

Create a batch file (`debug_quest.bat`) for Windows:

```batch
@echo off
echo Connecting to Quest...
adb connect 192.168.1.100:5555
echo.
echo Viewing Unity logs...
echo Press Ctrl+C to stop
adb logcat -s Unity:* DepthPointCloudGenerator:* EnvironmentDepthManager:*
```

Replace `192.168.1.100` with your Quest's IP address.

---

## Best Practices

### 1. Use Debug Logs in Code

```csharp
[Header("Debug")]
[SerializeField] private bool showDebugLogs = true;

void Update()
{
    if (showDebugLogs)
    {
        Debug.Log($"[MyScript] Status: {status}");
    }
}
```

### 2. Use Conditional Compilation

```csharp
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    Debug.Log("Debug info only in editor/dev builds");
#endif
```

### 3. Create Debug UI

```csharp
void OnGUI()
{
    if (showDebugInfo)
    {
        GUI.Label(new Rect(10, 10, 500, 30), 
            $"Points: {pointCount}");
    }
}
```

### 4. Use Assertions

```csharp
Debug.Assert(depthManager != null, "DepthManager is null!");
```

---

## Troubleshooting

### Problem: ADB Can't Connect

**Solution:**
1. Make sure Quest and PC are on same WiFi network
2. Check firewall isn't blocking port 5555
3. Try: `adb kill-server && adb start-server`
4. Reconnect: `adb connect IP:5555`

### Problem: No Logs Appearing

**Check:**
1. Quest is connected: `adb devices`
2. Unity Console is open
3. Log level filter is correct (not filtering out your logs)
4. Scripts are using `Debug.Log()` not `print()`

### Problem: Profiler Not Connecting

**Solution:**
1. Enable "Development Build" in build settings
2. Check Quest and PC are on same network
3. Try restarting Unity Editor
4. Check firewall settings

---

## Recommended Setup

### If ADB is Installed:
1. ✅ Enable ADB over WiFi (Method 1) - Best for wireless debugging
2. ✅ Enable Development Build
3. ✅ Use Unity Console for logs
4. ✅ Use Unity Profiler for performance

### If ADB is NOT Installed (Quick Start):
1. ✅ Use Method 2 (Unity Console via USB) - Easiest, no ADB needed
2. ✅ Enable Development Build
3. ✅ Connect Quest via USB
4. ✅ Use Unity Console for logs
5. ✅ Use Debug UI (OnGUI) for on-screen info

### For Testing:
1. ✅ Use Debug UI (OnGUI) for on-screen info
2. ✅ Enable debug logs in inspector
3. ✅ Use ADB logcat for detailed logs (if ADB installed)

---

## Quick Commands Reference

```bash
# Connect to Quest
adb connect 192.168.1.100:5555

# View Unity logs
adb logcat -s Unity

# View specific script logs
adb logcat -s DepthPointCloudGenerator

# Clear logs
adb logcat -c

# View all logs
adb logcat

# Disconnect
adb disconnect

# List connected devices
adb devices

# Install APK
adb install path/to/app.apk

# Uninstall app
adb uninstall com.yourcompany.yourapp
```

---

## Unity Console Filters

In Unity Console, you can filter logs:
- **Error** - Red logs
- **Warning** - Yellow logs  
- **Info** - White logs
- **Collapse** - Group duplicate logs
- **Clear on Play** - Clear when entering play mode

**Search bar:** Type script name or keyword to filter

---

## Example: Debug DepthPointCloudGenerator

### In Code:
```csharp
[Header("Debug")]
[SerializeField] private bool showDebugLogs = true;

void Update()
{
    if (showDebugLogs && Time.frameCount % 60 == 0) // Every second
    {
        Debug.Log($"[DepthPointCloudGenerator] Points: {GetPointCount()}");
        Debug.Log($"[DepthPointCloudGenerator] IsDepthAvailable: {depthManager.IsDepthAvailable}");
    }
}
```

### In Console:
You'll see:
```
[DepthPointCloudGenerator] Points: 2543
[DepthPointCloudGenerator] IsDepthAvailable: True
```

### Filter in Console:
Type `DepthPointCloudGenerator` in search bar to see only those logs.

---

## Tips

1. **Use consistent log prefixes:** `[ScriptName]` makes filtering easier
2. **Use log levels:** `Debug.Log()`, `Debug.LogWarning()`, `Debug.LogError()`
3. **Enable/Disable debug logs:** Use inspector bool to toggle
4. **Performance:** Don't log every frame, use frame counters
5. **Build types:** Use `#if DEVELOPMENT_BUILD` for debug-only code

---

## Next Steps

1. Set up ADB over WiFi
2. Enable Development Build
3. Deploy to Quest
4. Open Unity Console
5. Watch logs in real-time!

Good luck debugging! 🚀

