# Real-Time Depth Scanner Setup

Simple real-time depth point cloud visualization for Quest 3.

---

## Unity Setup

### 1. Scene Requirements
- **OVRCameraRig** prefab in scene (from Meta XR SDK)
- **OVRManager** > Passthrough Support: **Required**
- **OVRPassthroughLayer** component on OVRCameraRig

### 2. Add Scanner
1. Create empty GameObject: "DepthScanner"
2. Add **RealtimeDepthScanner** component
3. Settings:
   - Stride: 8 (lower = more points but slower)
   - Min Depth: 0.2m
   - Max Depth: 3.5m
   - Max Points: 3000
   - Visualize: ✓ checked
   - Update Rate: 10 (updates per second)

**Leave Point Prefab empty** - it will auto-create spheres

---

## Project Settings

### Meta XR
**Edit > Project Settings > Meta XR**
- Scene Support: **Required**
- Passthrough Support: **Required**
- Experimental Features: Enable environment depth

### XR Plug-in
**Edit > Project Settings > XR Plug-in Management > Android**
- ✓ Oculus

---

## Quest 3 Setup

### Space Setup (REQUIRED!)
1. Settings > Physical Space > Space Setup
2. Complete full room scan
3. Walk around, look at walls/floor/ceiling

### Permissions
- App will request "Scene" permission
- Grant it

---

## How It Works

- **Automatic**: No button press needed
- **Real-time**: Updates 10 times per second
- **What you'll see**: Cyan dots floating in space showing depth data

### Test It
1. Build and Run to Quest
2. Put on headset (see passthrough)
3. Look around - cyan dots appear automatically
4. Dots show the 3D structure of your room

### Adjust Settings
- **More points**: Lower stride (4 or 2) - slower but denser
- **Fewer points**: Higher stride (16 or 32) - faster but sparser
- **Closer surfaces**: Lower maxDepth (1.5m - 2.0m)
- **Faster updates**: Higher updateRate (20 or 30)

---

## Troubleshooting

**No dots appear:**
- Complete Space Setup on Quest
- Grant Scene permission
- Check Console for errors (use QuestDebugConsole)

**Too few dots:**
- Lower stride value (try 4)
- Increase maxPoints (try 5000)
- Look at solid surfaces (walls, tables)

**Performance issues:**
- Increase stride (try 16)
- Reduce maxPoints (try 1500)
- Lower updateRate (try 5)

---

## Based On

- [Meta Depth API Documentation](https://developers.meta.com/horizon/documentation/native/android/mobile-depth/)
- Meta XR SDK `EnvironmentDepthManager`
- Unity-DepthAPI official samples

**Key difference from previous scripts**: This one runs continuously in real-time, showing live depth data as you look around.


