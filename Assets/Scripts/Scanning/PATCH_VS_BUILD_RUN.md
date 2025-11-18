# Patch and Run vs Build and Run

## Quick Summary

| Feature | Patch and Run | Build and Run |
|---------|---------------|---------------|
| **Speed** | ⚡ Fast (30 seconds - 2 minutes) | 🐢 Slow (5-15 minutes) |
| **What it does** | Patches only changed code/assets | Full rebuild + deployment |
| **When to use** | Code changes, quick testing | First build, major changes, issues |
| **Reliability** | ⚠️ Can fail with complex changes | ✅ More reliable |
| **Requires** | Previous build must exist | No previous build needed |

---

## Patch and Run

### What It Does
- **Only rebuilds changed code and assets**
- Patches the existing APK on your device
- Deploys the patch to Quest
- Restarts the app

### When to Use
✅ **Best for:**
- Making code changes to scripts
- Testing small fixes quickly
- Iterating on features
- Debugging (after initial build)

### Requirements
- ⚠️ **Must have a previous build** (Build and Run first)
- Development Build must be enabled
- Quest must be connected via USB

### Pros
- ⚡ **Very fast** - Usually 30 seconds to 2 minutes
- Only updates what changed
- Great for rapid iteration
- Saves time during development

### Cons
- ⚠️ Can fail with complex changes (new assets, major refactoring)
- Requires previous build to exist
- May miss some changes
- Less reliable than full build

### How to Use
1. **First time:** Use "Build and Run" (creates base APK)
2. **Make code changes** in Unity
3. **File → Build Settings**
4. Click **"Patch and Run"** (appears after first build)
5. Wait for patch to deploy
6. App restarts automatically

---

## Build and Run

### What It Does
- **Full rebuild** of entire project
- Compiles all scripts
- Processes all assets
- Builds complete APK
- Installs on Quest
- Launches app

### When to Use
✅ **Best for:**
- **First build** (no previous build exists)
- Major code changes
- Adding/removing assets
- Changing project settings
- When Patch and Run fails
- Before sharing/testing builds
- Production builds

### Requirements
- Quest connected via USB (or ADB over WiFi)
- Development Build enabled (for debugging)

### Pros
- ✅ **Most reliable** - Always works
- Complete rebuild ensures all changes included
- No dependency on previous build
- Use for production builds

### Cons
- 🐢 **Slow** - 5-15 minutes typically
- Rebuilds everything (even unchanged code)
- Takes longer during development

### How to Use
1. **File → Build Settings**
2. Select Android platform
3. Click **"Build and Run"**
4. Choose output folder (first time)
5. Wait for full build
6. App installs and launches automatically

---

## Comparison Table

| Scenario | Use This |
|----------|----------|
| First time building | **Build and Run** |
| Changed a script | **Patch and Run** |
| Added new asset | **Build and Run** |
| Changed project settings | **Build and Run** |
| Quick bug fix | **Patch and Run** |
| Testing feature | **Patch and Run** |
| Production build | **Build and Run** |
| Patch and Run failed | **Build and Run** |
| Major refactoring | **Build and Run** |

---

## Workflow Example

### Typical Development Cycle:

1. **Initial Setup:**
   ```
   Build and Run (first time) → 10 minutes
   ```

2. **Development Iteration:**
   ```
   Make code change
   Patch and Run → 1 minute
   Test
   Make another change
   Patch and Run → 1 minute
   Test
   ```

3. **Major Changes:**
   ```
   Add new assets
   Build and Run → 10 minutes (full rebuild)
   ```

4. **Back to Iteration:**
   ```
   Make code change
   Patch and Run → 1 minute
   ```

---

## Troubleshooting

### Patch and Run Fails

**Symptoms:**
- Error: "No previous build found"
- Error: "Patch failed"
- App crashes after patch

**Solutions:**
1. **Use Build and Run instead** (most reliable fix)
2. Check Development Build is enabled
3. Verify Quest is connected
4. Try cleaning build folder:
   - Delete `Temp/` folder
   - Delete `Library/Bee/` folder
   - Build and Run again

### Build and Run Takes Too Long

**Optimizations:**
1. **Use Patch and Run** for code changes
2. Only use Build and Run when necessary
3. Close other applications
4. Use SSD for faster builds
5. Reduce project size (remove unused assets)

### Patch and Run Not Available

**Why:**
- No previous build exists
- Build Settings window not showing option

**Solution:**
- Use **Build and Run** first
- After first build, "Patch and Run" will appear

---

## Best Practices

### Development Workflow

1. **Start of session:**
   - Build and Run (if no build exists)
   - Or use existing build

2. **During development:**
   - Make code changes
   - Use **Patch and Run** for quick testing
   - Repeat as needed

3. **When Patch fails:**
   - Use **Build and Run** to reset
   - Continue with Patch and Run

4. **End of session:**
   - Final **Build and Run** to ensure everything works
   - Test thoroughly

### Performance Tips

- ✅ Use Patch and Run for 90% of iterations
- ✅ Only Build and Run when necessary
- ✅ Keep Development Build enabled
- ✅ Keep Quest connected via USB
- ✅ Use Patch and Run for debugging

---

## Unity Build Settings Window

### What You'll See:

```
File → Build Settings

[Android Platform Selected]

Buttons:
┌─────────────────┐
│   Build         │  ← Creates APK only (no deploy)
├─────────────────┤
│ Build and Run   │  ← Full build + deploy + launch
├─────────────────┤
│ Patch and Run   │  ← Quick patch + deploy (if build exists)
└─────────────────┘
```

**Note:** "Patch and Run" only appears after you've done at least one "Build and Run".

---

## When to Use Each

### Use Patch and Run When:
- ✅ You've already built once
- ✅ Making script changes
- ✅ Testing quickly
- ✅ Iterating on features
- ✅ Debugging code

### Use Build and Run When:
- ✅ First time building
- ✅ Patch and Run fails
- ✅ Added/removed assets
- ✅ Changed project settings
- ✅ Major code refactoring
- ✅ Production build
- ✅ Sharing with others

---

## Example Timeline

### Scenario: Fixing a bug in DepthPointCloudGenerator

**With Patch and Run:**
```
1. Find bug in code
2. Fix code (30 seconds)
3. Patch and Run (1 minute)
4. Test on Quest (30 seconds)
5. Bug fixed! ✅
Total: ~2 minutes
```

**With Build and Run:**
```
1. Find bug in code
2. Fix code (30 seconds)
3. Build and Run (10 minutes)
4. Test on Quest (30 seconds)
5. Bug fixed! ✅
Total: ~11 minutes
```

**Time saved: 9 minutes per iteration!**

---

## Summary

- **Patch and Run** = Fast iteration, code changes only
- **Build and Run** = Full rebuild, use when needed

**Recommended workflow:**
1. Build and Run (first time)
2. Patch and Run (for all code changes)
3. Build and Run (when Patch fails or major changes)

This saves significant time during development! 🚀

