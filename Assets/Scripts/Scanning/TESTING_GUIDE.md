# Testing Guide - How to Verify Scanning System Works on Quest

## Quick Verification Checklist

### ✅ Step 1: Check System Initialization

**What to Look For:**
1. **MRUK Scene Loaded:**
   - Check Unity Console for: `"[TableScanHelper] MRUK scene loaded. Ready for table scanning."`
   - If you see this, MRUK is working ✅

2. **Components Found:**
   - Check Console for any errors about missing components
   - Should see components auto-finding each other

3. **Depth API Available:**
   - Check Console for: `"[DepthCapture] EnvironmentDepthManager not found"` (warning is OK, it auto-creates)
   - Or: `"[DepthCapture] Environment Depth API not available"` (error - depth won't work)

**How to Check:**
- Build and deploy to Quest
- Open Unity Console (Window > General > Console)
- Look for initialization messages

---

### ✅ Step 2: Test Table Detection

**What Should Happen:**
1. **Press Right Controller A Button** (or Space if keyboard mode)
2. **Check Console:**
   - Should see: `"[ScanningWorkflow] Found X table(s). Select one to scan."`
   - Or: `"[ScanningWorkflow] Started capture session for table: [TableName]"`

**If No Tables Found:**
- ❌ Error: `"No tables found. Waiting for MRUK to detect tables..."`
- **Solution:** 
  - Wait longer for MRUK to scan room
  - Move around the room to help MRUK detect surfaces
  - Check that you have a table in the room
  - Verify MRUK prefab is in scene

**Visual Indicators:**
- UI should show: "Ready to scan. Select a table to begin."
- If auto-start enabled, scanning should begin automatically

---

### ✅ Step 3: Test Scanning Process

**What Should Happen:**
1. **Visual Indicators Appear:**
   - 8 position indicators (spheres/cubes) appear around the table
   - They should be colored (blue/cyan = recommended positions)

2. **UI Updates:**
   - Instruction text changes: "Move to position 1 (XXcm away)"
   - Progress indicator appears
   - View count shows: "0/8 views"
   - Coverage shows: "Coverage: 0%"

3. **As You Move:**
   - Position indicators change color when you're near them
   - Instruction text updates with distance
   - Coverage percentage increases

**Console Messages to Watch:**
- `"[TableScanHelper] View 1/8 captured."`
- `"[MultiViewCapture] Captured view 1. Points: XXXX"`
- `"[TableScanHelper] View 2/8 captured."`

**If Nothing Happens:**
- ❌ Check Console for errors
- ❌ Verify PassthroughCameraAccess is assigned
- ❌ Check that DepthCapture found EnvironmentDepthManager
- ❌ Ensure you're moving close enough to recommended positions (< 20cm)

---

### ✅ Step 4: Test View Capture

**What to Look For:**
1. **When You Reach a Position:**
   - Position indicator turns yellow (current position)
   - Instruction says: "Position X - Good! Hold still to capture..."
   - After ~0.5 seconds, view is captured
   - Position indicator turns green (captured)
   - View count updates: "1/8 views", "2/8 views", etc.

2. **Console Messages:**
   - `"[MultiViewCapture] Captured view X. Points: [number]"`
   - Number of points should be > 0 (typically 1000-10000+)

**If Views Not Capturing:**
- ❌ Check distance - must be within 20cm of recommended position
- ❌ Check Console for: `"[MultiViewCapture] Camera access not available"`
- ❌ Check Console for: `"[MultiViewCapture] Depth capture not available"`
- ❌ Verify view quality validation isn't failing

---

### ✅ Step 5: Test Processing

**What Should Happen:**
1. **When 8 Views Captured:**
   - Instruction text: "Scan complete! Processing data..."
   - Console: `"[ScanningWorkflow] Processing 8 views..."`
   - Console: `"[ScanningWorkflow] Total points: XXXX"`
   - Console: `"[ScanningWorkflow] Found X object clusters."`

2. **During Processing:**
   - May see brief pause (1-5 seconds)
   - Console shows progress: `"[ScanningWorkflow] Processing..."`

3. **After Processing:**
   - Console: `"[ScanningWorkflow] Processing complete. Created X objects."`
   - 3D objects appear on/near the table
   - Objects should be grabbable

**If Processing Fails:**
- ❌ Check Console for errors during processing
- ❌ Verify point cloud has points (should be > 0)
- ❌ Check that segmentation found objects (clusters > 0)

---

### ✅ Step 6: Test Object Interaction

**What Should Happen:**
1. **Objects Appear:**
   - 3D mesh objects visible on table
   - Objects positioned where real objects are

2. **Grab Objects:**
   - Reach out with hand/controller
   - Grab object (should have OVRGrabbable)
   - Object becomes semi-transparent (ghost mode)
   - Can move object around

3. **Release Object:**
   - Release grab
   - Object returns to normal appearance
   - Object stays in new position

**If Objects Don't Appear:**
- ❌ Check Console for processing errors
- ❌ Verify objects were created (check Console log)
- ❌ Check that objects aren't hidden or too small
- ❌ Verify mesh generation succeeded

---

## Debug Console Messages Reference

### ✅ Good Messages (System Working):
```
[TableScanHelper] MRUK scene loaded. Ready for table scanning.
[ScanningWorkflow] Found 1 table(s). Select one to scan.
[TableScanHelper] Initialized scan for table: [TableName]
[TableScanHelper] View 1/8 captured.
[MultiViewCapture] Captured view 1. Points: 5234
[ScanningWorkflow] Processing 8 views...
[ScanningWorkflow] Total points: 45678
[ScanningWorkflow] Found 3 object clusters.
[ScanningWorkflow] Processing complete. Created 3 objects.
```

### ❌ Error Messages (System Not Working):

**MRUK Issues:**
```
[TableScanHelper] MRUK.Instance is null
→ Solution: Add MRUK prefab to scene
```

**No Tables:**
```
[ScanningWorkflow] No tables found. Waiting for MRUK...
→ Solution: Wait for MRUK to scan, move around room
```

**Camera Issues:**
```
[MultiViewCapture] Camera access not available
→ Solution: Assign PassthroughCameraAccess reference
```

**Depth Issues:**
```
[DepthCapture] Environment Depth API not available
→ Solution: Requires Quest 3/Pro, check device
[MultiViewCapture] Depth capture not available
→ Solution: Wait for depth to initialize, check permissions
```

**Processing Issues:**
```
[ScanningWorkflow] No scan data to process
→ Solution: Ensure views were captured successfully
[ScanningWorkflow] No points in point cloud
→ Solution: Check depth capture, verify table height filtering
```

---

## Step-by-Step Testing Procedure

### Test 1: Basic Initialization
1. Build and deploy to Quest
2. Open Unity Console (if using Link) or check logs
3. Look for initialization messages
4. **Expected:** No errors, components found

### Test 2: Table Detection
1. Ensure table is in room
2. Wait for MRUK to scan (10-30 seconds)
3. Press Right Controller A button
4. **Expected:** Console shows table found, or UI shows "Ready to scan"

### Test 3: Start Scanning
1. Press A button (or auto-start if enabled)
2. **Expected:** 
   - Position indicators appear around table
   - UI shows instructions
   - Progress indicator visible

### Test 4: Capture Views
1. Move to first position indicator
2. Wait for capture (should happen automatically)
3. **Expected:**
   - Position indicator turns green
   - View count updates
   - Console shows "Captured view 1"

### Test 5: Complete Scan
1. Move to all 8 positions
2. Capture all views
3. **Expected:**
   - Coverage reaches 70%+
   - "Scan complete! Processing..." message
   - Processing happens (1-5 seconds)

### Test 6: Verify Objects
1. After processing completes
2. **Expected:**
   - 3D objects appear on table
   - Objects are grabbable
   - Can interact with objects

---

## Visual Debugging Tips

### Enable Debug Logs
All scripts use `Debug.Log()` - check Unity Console for detailed information.

### Add Visual Debugging (Optional)
You can add temporary debug visuals:
- Draw lines to show recommended positions
- Show point cloud visualization
- Display coverage grid

### Check UI Visibility
- Ensure Canvas is active
- Check UI elements are visible
- Verify font sizes are readable in VR

---

## Common Issues & Solutions

### Issue: "Nothing happens when I press A button"
**Check:**
- Console for errors
- `useControllerInput` is true in ScanningWorkflow
- Controller is connected
- System is in Idle state

### Issue: "No tables detected"
**Check:**
- MRUK is in scene
- MRUK scene is loaded (wait 10-30 seconds)
- Table is in the room
- Move around to help MRUK scan

### Issue: "Views not capturing"
**Check:**
- You're within 20cm of recommended position
- PassthroughCameraAccess is assigned
- Depth API is available (Quest 3/Pro)
- Console for specific errors

### Issue: "No objects appear after scanning"
**Check:**
- Console for processing errors
- Point cloud has points (> 0)
- Objects were created (check Console)
- Objects might be too small or hidden

### Issue: "UI not visible"
**Check:**
- Canvas is active
- Canvas Render Mode is correct
- UI elements are active
- Font sizes are large enough for VR

---

## Performance Indicators

### Good Performance:
- Smooth frame rate (72 FPS on Quest 2, 90 FPS on Quest 3)
- Views capture within 0.5-1 second
- Processing completes in 2-5 seconds
- No stuttering or lag

### Performance Issues:
- Low frame rate during scanning
- Processing takes > 10 seconds
- Stuttering when capturing views
- **Solutions:** Reduce view count, increase capture interval, optimize mesh generation

---

## Quick Test Script (Optional)

Add this to a GameObject to test components:

```csharp
using UnityEngine;

public class ScanningSystemTester : MonoBehaviour
{
    void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.Two)) // B button
        {
            TestComponents();
        }
    }
    
    void TestComponents()
    {
        Debug.Log("=== Scanning System Test ===");
        
        var scanHelper = FindFirstObjectByType<TableScanHelper>();
        Debug.Log($"TableScanHelper: {(scanHelper != null ? "Found" : "MISSING")}");
        
        var multiView = FindFirstObjectByType<MultiViewCapture>();
        Debug.Log($"MultiViewCapture: {(multiView != null ? "Found" : "MISSING")}");
        
        var depth = FindFirstObjectByType<DepthCapture>();
        Debug.Log($"DepthCapture: {(depth != null ? "Found" : "MISSING")}");
        Debug.Log($"Depth Available: {depth?.IsDepthAvailable}");
        
        var workflow = FindFirstObjectByType<ScanningWorkflow>();
        Debug.Log($"ScanningWorkflow: {(workflow != null ? "Found" : "MISSING")}");
        Debug.Log($"Current State: {workflow?.CurrentState}");
        
        var tables = TableScanHelper.GetAvailableTables();
        Debug.Log($"Tables Found: {tables.Count}");
    }
}
```

---

## Summary

**To verify the system is working:**
1. ✅ Check Console for initialization messages
2. ✅ Press A button - should detect tables
3. ✅ Position indicators appear around table
4. ✅ Views capture as you move to positions
5. ✅ Processing completes and objects appear
6. ✅ Objects are grabbable and interactive

**If any step fails, check Console for specific error messages!**

