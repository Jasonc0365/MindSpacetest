# Quick Setup Checklist

## Your Current Setup ✅

All components on one GameObject - **Perfect!** This is the recommended setup.

```
Your GameObject (e.g., "DepthSystem")
├── EnvironmentDepthManager
├── DepthPointCloudGenerator
├── PointCloudVisualizer
├── DepthTestController
└── DepthTableScanner (optional)
```

---

## Critical Settings to Check

### 1. DepthPointCloudGenerator ⚠️ IMPORTANT
```
✅ Visualize: FALSE (must be false!)
✅ EnvironmentDepthManager: [Auto-finds or assign]
✅ StrideX: 4
✅ StrideY: 4
✅ MaxDepth: 4.0
✅ MinDepth: 0.1
```

**Why `Visualize = false`?**
- `PointCloudVisualizer` handles visualization
- If `true`, both will try to visualize = conflict!

### 2. PointCloudVisualizer ⚠️ IMPORTANT
```
✅ Auto Update: TRUE (must be true!)
✅ Update Interval: 0.1
✅ Point Cloud Generator: [Auto-finds or assign]
✅ Table Scanner: [Optional - can be null]
✅ Use Mesh Rendering: TRUE
✅ Point Material: [None - auto-creates]
```

**Why `AutoUpdate = true`?**
- Automatically visualizes points every frame
- No manual trigger needed

### 3. EnvironmentDepthManager
```
✅ Component Enabled: TRUE
✅ No other settings needed
```

### 4. DepthTestController
```
✅ Point Cloud Generator: [Auto-finds or assign]
✅ Table Scanner: [Optional]
✅ Visualizer: [Auto-finds or assign]
✅ Use Controller Input: TRUE
```

### 5. DepthTableScanner (Optional)
```
✅ Point Cloud Generator: [Auto-finds or assign]
✅ EnvironmentDepthManager: [Auto-finds or assign]
```

---

## Quick Test

1. **Enter Play Mode**
2. **Wait 2-3 seconds** for initialization
3. **Check Console:**
   ```
   [DepthPointCloudGenerator] ✅ Generated X points from depth
   [PointCloudVisualizer] ✅ Visualized X points
   ```
4. **Look for points** in scene (colored dots)
5. **Press A button** (right controller) to manually trigger

---

## Expected Behavior

### Automatic (Default):
- Points generate every frame
- Points visualize automatically every 0.1 seconds
- No button press needed

### Manual (Controller):
- Press **A button** → Triggers visualization update
- Press **B button** → Clears visualization

---

## Troubleshooting

### No Points Visible?

**Check:**
1. `DepthPointCloudGenerator.Visualize = false` ✅
2. `PointCloudVisualizer.AutoUpdate = true` ✅
3. `EnvironmentDepthManager.IsDepthAvailable = true`
4. Wait 2-3 seconds after scene starts
5. Point Quest at objects (not empty space)

### Points Not Updating?

**Check:**
1. `PointCloudVisualizer.AutoUpdate = true` ✅
2. `UpdateInterval = 0.1` (or lower for faster updates)
3. Console shows point generation logs

### Console Errors?

**Common errors:**
- "Depth not available" → Wait for initialization
- "No reprojection matrices" → Wait for depth API
- "Depth texture is null" → Check EnvironmentDepthManager

---

## Reference Assignment

**All references can be left as NULL** - scripts auto-find each other!

But you can also manually assign:
- Drag components from same GameObject
- Or drag from different GameObjects if needed

---

## Summary

Your setup is **correct**! Just verify:

✅ `DepthPointCloudGenerator.Visualize = false`  
✅ `PointCloudVisualizer.AutoUpdate = true`  
✅ All components enabled  
✅ Wait 2-3 seconds after Play Mode starts

Everything should work automatically! 🎉

