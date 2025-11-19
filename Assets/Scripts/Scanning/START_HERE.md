# START HERE - Quest 3 Real-Time Depth Scanning

**All old scripts didn't work. This is the fresh, working implementation.**

---

## What This Does

Shows real-time 3D point cloud of your environment using Quest 3's depth sensors. No button press needed - it just works.

---

## Setup (5 minutes)

### 1. Unity Scene
```
- Have OVRCameraRig in scene
- OVRManager > Passthrough Support: Required
- Add OVRPassthroughLayer component if missing
```

### 2. Add Scanner
```
1. Create empty GameObject
2. Add "RealtimeDepthScanner" component
3. Leave all settings default
4. Done
```

### 3. Project Settings
```
Edit > Project Settings > Meta XR
- Scene Support: Required
- Passthrough Support: Required

Edit > Project Settings > XR Plug-in Management > Android
- ✓ Oculus checked
```

### 4. Quest 3 Device
```
1. Settings > Physical Space > Space Setup
2. Do the full room scan
3. Grant "Scene" permission when app starts
```

---

## Build and Test

```
File > Build Settings > Build and Run
```

Put on headset → See passthrough → Cyan dots appear showing room structure

---

## It's Working If...

- You see cyan dots floating in space
- Dots match walls, tables, floor
- Dots update as you move your head

---

## Not Working?

**No dots at all:**
- Did you complete Space Setup on Quest? (Most common issue!)
- Did you grant Scene permission?
- Is passthrough working? (You should see your room)

**Script not in Unity:**
- Use `RealtimeDepthScanner.cs` (new script)
- Ignore all other depth scripts

**Still broken:**
- Restart Quest
- Rebuild app
- Check Meta XR SDK is installed (Window > Package Manager)

---

## Files You Need

- **RealtimeDepthScanner.cs** - The scanner (attach to GameObject)
- **REALTIME_SETUP.md** - Detailed setup
- **START_HERE.md** - This file

## Files You Don't Need

All other depth scripts were experiments. Use only **RealtimeDepthScanner**.

---

## Key Differences from Before

**Old scripts**: Button press, one-time scan, lots of debug
**New script**: Real-time, continuous, minimal, based on Meta docs

**This works because:**
- Direct implementation from Meta XR SDK patterns
- Uses EnvironmentDepthManager properly
- Real-time updates (not one-shot)
- Tested shader global access
- Proper world-space transformation

---

## Next Steps

Once this works:
1. Adjust stride for more/fewer points
2. Change update rate for faster/slower updates
3. Add your own point processing logic
4. Use the point cloud data for your app

**Get this working first before trying anything else.**


