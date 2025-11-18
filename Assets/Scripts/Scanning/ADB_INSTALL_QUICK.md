# Quick ADB Installation Guide

## Problem: `adb : The term 'adb' is not recognized`

This means ADB (Android Debug Bridge) is not installed or not in your PATH.

---

## Solution: Install ADB (Choose One Method)

### Method 1: Download Platform Tools (5 minutes)

1. **Download:**
   - Go to: https://developer.android.com/studio/releases/platform-tools
   - Click **Download SDK Platform-Tools for Windows**
   - Or direct link: https://dl.google.com/android/repository/platform-tools-latest-windows.zip

2. **Extract:**
   - Extract ZIP to `C:\adb` (create folder if needed)
   - You should have: `C:\adb\adb.exe`

3. **Add to PATH:**
   - Press `Win + X` → **System**
   - Click **Advanced system settings** (right side)
   - Click **Environment Variables** button
   - Under **System Variables**, find **Path** → Click **Edit**
   - Click **New** → Type: `C:\adb`
   - Click **OK** on all windows

4. **Restart PowerShell:**
   - Close all PowerShell/Command Prompt windows
   - Open new PowerShell window
   - Test: `adb version`

---

### Method 2: Use Unity's ADB (If you have Android Build Support)

Unity includes ADB if you have Android Build Support installed.

1. **Check if installed:**
   - Open Unity Hub
   - **Installs** → Your Unity version
   - Check if **Android Build Support** is installed

2. **If installed, find ADB:**
   - Location: `C:\Users\[YourName]\AppData\Local\Android\sdk\platform-tools\adb.exe`
   - Replace `[YourName]` with your Windows username

3. **Add to PATH:**
   - Follow Method 1, Step 3
   - But use the path from Step 2 instead of `C:\adb`

---

### Method 3: Use SideQuest's ADB (If you have SideQuest)

If you have SideQuest installed, it includes ADB.

1. **Find ADB:**
   - Location: `C:\Users\[YourName]\AppData\Local\SideQuest\platform-tools\adb.exe`

2. **Add to PATH:**
   - Follow Method 1, Step 3
   - But use: `C:\Users\[YourName]\AppData\Local\SideQuest\platform-tools`

---

## Verify Installation

Open **new** PowerShell window:

```powershell
adb version
```

**Success:** Shows `Android Debug Bridge version X.X.X`

**Still not working:**
1. Make sure you restarted PowerShell
2. Check PATH: `$env:Path` (should include your ADB folder)
3. Try full path: `C:\adb\adb.exe version`

---

## Quick Test After Installation

```powershell
# Connect Quest via USB first
adb devices

# Should show your Quest device
```

---

## Alternative: Skip ADB Entirely

You don't need ADB for basic debugging! Use Unity Console:

1. **Edit → Project Settings → Player**
2. Enable **"Development Build"**
3. Connect Quest via USB
4. **File → Build and Run**
5. **Window → General → Console**
6. Logs appear automatically!

See `QUEST_STANDALONE_DEBUGGING.md` for Method 2 (no ADB required).

---

## Troubleshooting

### "adb is not recognized" after adding to PATH
- ✅ Restart PowerShell/Command Prompt
- ✅ Check PATH includes correct folder
- ✅ Verify `adb.exe` exists in that folder

### "adb: command not found" in PowerShell
- ✅ Use full path: `C:\adb\adb.exe`
- ✅ Or use Command Prompt instead of PowerShell

### Can't find Android SDK folder
- ✅ Install Android Build Support in Unity Hub
- ✅ Or download Platform Tools manually (Method 1)

---

## Next Steps

Once ADB is installed:
1. Connect Quest via USB
2. Run: `adb devices` (should show your device)
3. Follow `QUEST_STANDALONE_DEBUGGING.md` Method 1

Or skip ADB and use Unity Console directly (Method 2 in the main guide).

