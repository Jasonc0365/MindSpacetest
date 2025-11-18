# Point Material Setup Guide

## Quick Answer

**You have 3 options for the Point Material field:**

### Option 1: Leave it Empty (Recommended - Easiest)
- **Leave the field as `None` (empty)**
- The script will automatically create a material for you
- It will use the best available shader for point rendering
- This is the simplest option and works well

### Option 2: Create Your Own Material (Best Control)
- Create a new Material in Unity
- Use one of these shaders:
  - `Particles/Standard Unlit` (best for points)
  - `Unlit/Color` (simple, good performance)
  - `Standard` (if you want lighting)
- Assign it to the Point Material field

### Option 3: Use Existing Material
- You can use any existing material (like `Green.mat` or `Yellow.mat`)
- But it may not render points correctly if it's not designed for point rendering

---

## Detailed Instructions

### Option 1: Auto-Create (Easiest) ✅

1. **In Unity Inspector:**
   - Find `PointCloudVisualizer` component
   - Find `Point Material` field
   - **Leave it as `None` (empty)**

2. **The script will automatically:**
   - Find a suitable shader (`Particles/Standard Unlit` or `Standard`)
   - Create a material with proper settings
   - Configure it for point rendering
   - Log a message: "Created default point material"

3. **That's it!** No further action needed.

---

### Option 2: Create Custom Material (Best for Customization)

#### Step 1: Create the Material

1. In Unity Project window, right-click in a folder (e.g., `Assets/Materials`)
2. Select **Create → Material**
3. Name it `PointCloudMaterial` (or any name you like)

#### Step 2: Configure the Material

1. Select the new material
2. In Inspector, click the **Shader** dropdown
3. Choose one of these shaders:

   **Best Option: `Particles/Standard Unlit`**
   - Good for point clouds
   - No lighting calculations (faster)
   - Works well with vertex colors

   **Alternative: `Unlit/Color`**
   - Very simple
   - Good performance
   - Basic color rendering

   **Alternative: `Standard`**
   - Supports lighting
   - More features but slower
   - Can look more realistic

#### Step 3: Configure Material Settings

For `Particles/Standard Unlit`:
- **Color**: White (or any base color - vertex colors will override)
- **Rendering Mode**: Transparent (if you want transparency)
- **Point Size**: Adjust if needed (script also sets this)

For `Unlit/Color`:
- **Color**: White (vertex colors will be used)

For `Standard`:
- **Rendering Mode**: Opaque or Transparent
- **Metallic**: 0
- **Smoothness**: 0.5 (or adjust as needed)

#### Step 4: Assign to PointCloudVisualizer

1. Select GameObject with `PointCloudVisualizer` component
2. Drag your `PointCloudMaterial` to the **Point Material** field
3. Done!

---

## Shader Recommendations

| Shader | Best For | Performance | Notes |
|--------|----------|-------------|-------|
| `Particles/Standard Unlit` | ✅ **Recommended** | Fast | Designed for particles/points |
| `Unlit/Color` | Simple cases | Very Fast | Basic, no lighting |
| `Standard` | Realistic look | Slower | Supports lighting, shadows |

---

## Material Settings Reference

### For Point Cloud Rendering:

**Key Properties:**
- **Color**: Usually white (vertex colors from script will be used)
- **Rendering Mode**: 
  - `Opaque` - Solid points (faster)
  - `Transparent` - See-through points (if you want transparency)
- **Point Size**: Controlled by script, but you can adjust in material too

**Note:** The script automatically sets point size via:
```csharp
pointMaterial.SetFloat("_PointSize", pointSize * 200f);
```

---

## Troubleshooting

### Points Not Visible
- Check if material shader supports point rendering
- Try `Particles/Standard Unlit` shader
- Increase `Point Size` in PointCloudVisualizer settings

### Points Too Small/Large
- Adjust `Point Size` in PointCloudVisualizer (range: 0.01 to 0.1)
- The script multiplies this by 200 for shader point size

### Colors Not Showing
- Ensure shader supports vertex colors
- `Particles/Standard Unlit` supports vertex colors
- Check that `useMeshRendering = true` (uses vertex colors)

### Material Not Working
- Try leaving it empty (Option 1) - let script auto-create
- Or create new material with `Particles/Standard Unlit` shader

---

## Quick Setup Checklist

- [ ] Option 1: Leave Point Material empty → Script auto-creates ✅
- [ ] OR Option 2: Create material with `Particles/Standard Unlit` shader
- [ ] Assign material to Point Material field (if using Option 2)
- [ ] Adjust Point Size (0.01-0.1) if needed
- [ ] Test in Play mode

---

## Example Material Creation

**Quick Steps:**
1. Right-click in Project → Create → Material
2. Name: `PointCloudMaterial`
3. Shader: `Particles/Standard Unlit`
4. Color: White
5. Drag to PointCloudVisualizer → Point Material field

**That's it!** Your point cloud will render with this material.

---

## Summary

**Simplest:** Leave Point Material empty (None) - script handles it automatically ✅

**Best Control:** Create material with `Particles/Standard Unlit` shader

**Current Default:** Script tries `Particles/Standard Unlit`, falls back to `Standard` if not found

