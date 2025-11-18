# UI Setup Tips for VR

## What's Automatic (No Manual Text Needed)

✅ **Text Content is Auto-Updated:**
- `instructionText` - Automatically updates with scanning instructions
- `progressText` - Shows percentage (e.g., "45%")
- `viewCountText` - Shows view count (e.g., "5/8 views")
- `coverageText` - Shows coverage (e.g., "Coverage: 62%")

**You don't need to set any text manually** - the scripts handle all text updates automatically.

---

## What You DO Need to Configure

### 1. Canvas Setup for VR

**Two Options:**

#### Option A: Screen Space - Overlay (RECOMMENDED - Easiest)
- **Render Mode**: `Screen Space - Overlay`
- **Positioning**: ✅ **Automatically in front of camera** - NO manual positioning needed!
- **Behavior**: UI always follows your view, stays at fixed distance
- **Setup**: Just create Canvas, set to Overlay mode, done!

**Recommended Canvas Settings:**
```
Render Mode: Screen Space - Overlay
Canvas Scaler: Scale With Screen Size
Reference Resolution: 1920 x 1080
Match: 0.5 (or adjust as needed)
```

**Advantages:**
- ✅ No positioning needed - always visible
- ✅ Easy to set up
- ✅ Works automatically in VR
- ✅ No scaling issues

#### Option B: World Space (Advanced - Only if you want 3D positioning)
- **Render Mode**: `World Space`
- **Positioning**: ❌ **Must position manually** in 3D space
- **Setup**: Position Canvas in front of camera (e.g., 2-3 meters away)
- **Scaling**: Must scale down Canvas (0.001-0.002 on all axes)

**When to use World Space:**
- You want UI at a specific 3D location
- You want UI to be part of the scene geometry
- You want UI to be occluded by objects

**For most cases, use Screen Space - Overlay!**

### 2. Font Size for VR Visibility

**TextMesh Pro Font Sizes (recommended for VR):**
- **Instruction Text**: 36-48 (main instructions)
- **Progress Text**: 24-32 (percentage display)
- **View Count Text**: 24-32 (view counter)
- **Coverage Text**: 24-32 (coverage percentage)

**How to adjust:**
1. Select the TextMesh Pro GameObject
2. In Inspector, find "Font Size" property
3. Set to recommended values above
4. Adjust based on your viewing distance

### 3. UI Element Positioning

**Layout Recommendations:**
- **Instruction Text**: Top center or center of screen
- **Progress Indicator**: Bottom center or top right
- **Progress Bar**: Below instruction text
- **Text Elements**: Stack vertically or arrange horizontally

**Use RectTransform Anchors:**
- Anchor to corners/edges for responsive positioning
- Example: Instruction text anchored to "Top Center"

### 4. Canvas Scale (Only If Using World Space)

**⚠️ Only needed if you chose World Space mode!**

If you want the UI in 3D world space:
1. Set Canvas Render Mode to `World Space`
2. Set Canvas RectTransform:
   - Width: 800-1200 pixels
   - Height: 600-800 pixels
3. Scale Canvas Transform:
   - X: 0.001-0.002
   - Y: 0.001-0.002
   - Z: 0.001-0.002
4. **Position Canvas in front of camera:**
   - Position: (0, 0, 2) to (0, 0, 3) meters in front of camera
   - Or parent Canvas to camera and offset forward
   - Or use a script to position it dynamically

**Note**: For Screen Space - Overlay, skip all of this - it's automatic!

### 5. Initial Text (Optional)

You can set placeholder text initially, but it will be overwritten:
- Instruction Text: "Ready to scan..."
- Progress Text: "0%"
- View Count: "0/8"
- Coverage: "Coverage: 0%"

---

## Quick Setup Checklist

### Canvas:
- [ ] Create Canvas (if not exists)
- [ ] Set Render Mode (Screen Space - Overlay recommended)
- [ ] Add Canvas Scaler component
- [ ] Set Reference Resolution (1920x1080)

### Text Elements:
- [ ] Create TextMesh Pro - Text (UI) GameObjects
- [ ] Set Font Size (36-48 for instructions, 24-32 for others)
- [ ] Position using RectTransform
- [ ] **Don't worry about text content** - scripts handle it!

### Layout:
- [ ] Arrange UI elements in logical positions
- [ ] Use anchors for responsive positioning
- [ ] Test in VR to ensure readability

---

## VR-Specific Tips

1. **Larger Fonts**: VR needs larger fonts than flat screens
2. **High Contrast**: Use white text on dark backgrounds or vice versa
3. **Distance**: If using World Space, position 2-3 meters from camera
4. **Size**: Text should be readable from typical VR viewing distance
5. **Testing**: Always test in VR headset, not just editor

---

## Example Font Size Guide

| Element | Font Size | Use Case |
|---------|-----------|----------|
| Instruction Text | 36-48 | Main instructions (most important) |
| Progress Text | 24-32 | Percentage display |
| View Count | 24-32 | Counter display |
| Coverage Text | 24-32 | Coverage percentage |

**Note**: Adjust based on your Canvas scale and viewing distance. Start with these values and increase if text is too small in VR.

---

## Troubleshooting

**"Text is too small in VR":**
- Increase font size in TextMesh Pro component
- If using World Space, scale Canvas down less (e.g., 0.002 instead of 0.001)
- Move Canvas closer to camera

**"Text is too large":**
- Decrease font size
- If using World Space, scale Canvas down more
- Move Canvas further from camera

**"Text not visible":**
- Check Canvas is active
- Verify TextMesh Pro component is enabled
- Check text color (should contrast with background)
- Ensure Canvas Render Mode is correct

