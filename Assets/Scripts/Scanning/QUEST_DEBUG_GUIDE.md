# Quest 3 Debug Console Guide

Multiple ways to see Unity debug logs when running standalone on Quest 3.

---

## Method 1: In-App Debug Console (EASIEST) ⭐

Shows logs directly in VR as floating text panel.

### Setup (2 minutes):

1. **Add the script to your scene:**
   - Create empty GameObject called "DebugConsole"
   - Add `QuestDebugConsole.cs` component

2. **Done!** The script auto-creates UI for you.

3. **Usage:**
   - Press **B button** (right controller) to toggle console on/off
   - All `Debug.Log()`, `Debug.LogWarning()`, `Debug.LogError()` appear automatically

### Example:
```csharp
void Start()
{
    Debug.Log("✅ App started!");
    Debug.LogWarning("⚠️ This is a warning");
    Debug.LogError("❌ This is an error");
}
```

**Result:** See all logs floating in VR!

---

## Method 2: ADB Logcat (Most Detailed)

See ALL Unity logs on your PC via USB.

### Setup:

#### Windows:
```bash
# 1. Connect Quest via USB
# 2. Enable USB debugging on Quest (Settings > System > Developer)
# 3. Open Command Prompt/PowerShell

# View all Unity logs (real-time)
adb logcat -s Unity

# View logs with filter
adb logcat -s Unity:V ActivityManager:I

# Save logs to file
adb logcat -s Unity > quest_logs.txt
```

#### Mac/Linux:
```bash
# Same commands work on Mac/Linux
adb logcat -s Unity
```

### Useful ADB Commands:

```bash
# Clear previous logs
adb logcat -c

# View logs with color (if supported)
adb logcat -s Unity -v color

# View logs from your app only
adb logcat -s Unity | grep "SimpleDepthScanner"

# View with timestamps
adb logcat -s Unity -v time

# View last 100 lines
adb logcat -t 100 -s Unity
```

### Pro Tips:

**Filter by tag:**
```csharp
// In Unity:
Debug.Log("[DEPTH] Scanning started");
Debug.Log("[UI] Button pressed");

// In ADB:
adb logcat -s Unity | grep "\[DEPTH\]"
```

**Watch logs in real-time:**
```bash
# Windows PowerShell
adb logcat -s Unity | Select-String "SimpleDepthScanner"

# Mac/Linux
adb logcat -s Unity | grep "SimpleDepthScanner"
```

---

## Method 3: Meta Quest Developer Hub (EASIEST PC TOOL) ⭐

GUI tool from Meta with built-in log viewer.

### Setup:

1. **Download:** https://developer.oculus.com/meta-quest-developer-hub/
2. **Install** Meta Quest Developer Hub (MQDH)
3. **Connect Quest via USB**
4. **Click "Device Logs" tab**

**Benefits:**
- ✅ GUI interface (no command line)
- ✅ Filter logs by severity
- ✅ Search functionality
- ✅ Save logs to file
- ✅ Screenshots and recordings

---

## Method 4: Oculus Debug Tool (Advanced)

Real-time performance metrics + logs.

### Setup:

1. Connect Quest via USB
2. Run: `adb logcat | grep Unity`
3. Use Oculus Debug Tool for FPS/performance overlay

---

## Method 5: Network Logging (Wireless)

Send logs to PC over WiFi (no USB needed).

### Quick Setup:

```csharp
// Add this script: NetworkLogger.cs
using UnityEngine;
using System.Net.Sockets;
using System.Text;

public class NetworkLogger : MonoBehaviour
{
    private TcpClient client;
    private NetworkStream stream;
    
    void Start()
    {
        try
        {
            // Replace with your PC's IP address
            client = new TcpClient("192.168.1.100", 9999);
            stream = client.GetStream();
            Application.logMessageReceived += SendLog;
            Debug.Log("Connected to PC logger");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to connect: {e.Message}");
        }
    }
    
    void SendLog(string log, string trace, LogType type)
    {
        if (stream != null && stream.CanWrite)
        {
            string message = $"[{type}] {log}\n";
            byte[] data = Encoding.UTF8.GetBytes(message);
            stream.Write(data, 0, data.Length);
        }
    }
    
    void OnDestroy()
    {
        Application.logMessageReceived -= SendLog;
        stream?.Close();
        client?.Close();
    }
}
```

**PC Listener (Python):**
```python
import socket

sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
sock.bind(('0.0.0.0', 9999))
sock.listen(1)
print("Waiting for Quest to connect...")

conn, addr = sock.accept()
print(f"Connected: {addr}")

while True:
    data = conn.recv(1024)
    if not data:
        break
    print(data.decode('utf-8'), end='')
```

---

## Best Practice: Combined Setup

Use multiple methods together:

```csharp
public class DebugManager : MonoBehaviour
{
    void Start()
    {
        // 1. In-VR console for quick checks
        gameObject.AddComponent<QuestDebugConsole>();
        
        // 2. Enable stack traces for errors only
        Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.ScriptOnly);
        Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
        
        // 3. Log system info
        Debug.Log($"Device: {SystemInfo.deviceModel}");
        Debug.Log($"OS: {SystemInfo.operatingSystem}");
        Debug.Log($"GPU: {SystemInfo.graphicsDeviceName}");
        Debug.Log($"Memory: {SystemInfo.systemMemorySize} MB");
    }
}
```

---

## Recommended: Use Tags for Better Filtering

```csharp
// In your scripts:
public class SimpleDepthScanner : MonoBehaviour
{
    private const string LOG_TAG = "[SimpleDepthScanner]";
    
    void Scan()
    {
        Debug.Log($"{LOG_TAG} Scanning...");
        Debug.Log($"{LOG_TAG} Scanned {points.Length} points");
    }
}

// Then filter in ADB:
// adb logcat -s Unity | grep "\[SimpleDepthScanner\]"
```

---

## Quick Comparison:

| Method | Difficulty | Setup Time | Wireless | Best For |
|--------|-----------|------------|----------|----------|
| **In-App Console** | ⭐ Easy | 2 min | ✅ Yes | Quick debugging in VR |
| **ADB Logcat** | ⭐⭐ Medium | 5 min | ❌ No (USB) | Detailed logs, crashes |
| **Meta Quest Hub** | ⭐ Easy | 10 min | ❌ No (USB) | GUI, screenshots |
| **Network Logging** | ⭐⭐⭐ Hard | 20 min | ✅ Yes | Wireless development |

---

## My Recommendation for Your Depth Scanner:

### Development Phase:
1. **Use In-App Console** (`QuestDebugConsole.cs`)
   - Quick visual feedback
   - Toggle on/off with B button
   - See logs without removing headset

2. **Keep ADB Logcat running** on PC
   ```bash
   adb logcat -s Unity | grep "SimpleDepthScanner"
   ```
   - Catch crashes and errors
   - See all system messages

### Testing Phase:
1. **Use Meta Quest Developer Hub**
   - Record sessions
   - Take screenshots
   - Monitor performance

---

## Setup for Your SimpleDepthScanner:

1. **Add debug console to scene:**
   ```csharp
   // In Unity Editor:
   // 1. Create empty GameObject "DebugConsole"
   // 2. Add QuestDebugConsole component
   // 3. Build and run
   ```

2. **Your logs will now appear in VR:**
   - "Scanning..." → Shows in VR
   - "Scanned 2500 points" → Shows in VR
   - All errors/warnings → Shows in VR

3. **Toggle console:**
   - Press **B button** to hide/show

---

## Troubleshooting:

### "Console not showing"
- Check GameObject is active
- Press B button to toggle
- Check canvas is in front of camera (position: 0, 1.5, 2)

### "ADB not found"
```bash
# Windows: Add to PATH
# Android SDK location: C:\Users\<user>\AppData\Local\Android\Sdk\platform-tools

# Or use full path:
C:\Users\<user>\AppData\Local\Android\Sdk\platform-tools\adb.exe logcat -s Unity
```

### "No logs appearing"
- Ensure USB debugging is enabled on Quest
- Accept "Allow USB debugging" prompt on Quest
- Try: `adb devices` to verify connection

---

## Example Output:

### In-App Console:
```
[17:45:23] ✅ SimpleDepthScanner initialized
[17:45:25] 📸 Scanning...
[17:45:26] ✅ Scanned 2547 points
[17:45:30] 📸 Scanning...
[17:45:31] ✅ Scanned 3012 points
```

### ADB Logcat:
```bash
11-17 17:45:23.451  2890  2920 I Unity   : [SimpleDepthScanner] ✅ Depth initialized
11-17 17:45:25.102  2890  2920 I Unity   : [SimpleDepthScanner] Scanning...
11-17 17:45:26.234  2890  2920 I Unity   : [SimpleDepthScanner] Scanned 2547 points
```

---

## Pro Tips:

1. **Color-code your logs:**
   ```csharp
   Debug.Log("✅ Success message");
   Debug.LogWarning("⚠️ Warning message");
   Debug.LogError("❌ Error message");
   ```

2. **Use conditional compilation:**
   ```csharp
   #if DEVELOPMENT_BUILD || UNITY_EDITOR
       Debug.Log("Development log");
   #endif
   ```

3. **Performance tracking:**
   ```csharp
   var startTime = Time.realtimeSinceStartup;
   // ... your code ...
   var elapsed = (Time.realtimeSinceStartup - startTime) * 1000f;
   Debug.Log($"Scan took {elapsed:F2}ms");
   ```

---

## Next Steps:

1. **Add QuestDebugConsole to your scene** (quickest)
2. **Build and run on Quest**
3. **Press B button to toggle console**
4. **Your debug logs will appear in VR!**

No more blind debugging! 🎉

